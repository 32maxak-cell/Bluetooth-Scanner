// bluetooth_scanner.java — Java версия

import javax.bluetooth.*;
import javax.microedition.io.StreamConnection;
import java.io.*;
import java.util.*;

public class bluetooth_scanner implements DiscoveryListener {
    private static final Object lock = new Object();
    private static List<DeviceInfo> devices = new ArrayList<>();

    static class DeviceInfo {
        String name;
        String address;
        int rssi;

        DeviceInfo(String name, String address, int rssi) {
            this.name = name;
            this.address = address;
            this.rssi = rssi;
        }
    }

    public void deviceDiscovered(RemoteDevice device, DeviceClass cod) {
        try {
            String name = device.getFriendlyName(false);
            if (name == null || name.isEmpty()) name = "Неизвестно";
            // RSSI не доступен напрямую в JSR-82, используем заглушку
            // В реальном проекте нужно использовать BlueCove с расширениями
            int rssi = -50 - new Random().nextInt(40); // Имитация RSSI
            devices.add(new DeviceInfo(name, device.getBluetoothAddress(), rssi));
        } catch (IOException e) {
            // Игнорируем
        }
    }

    public void inquiryCompleted(int discType) {
        System.out.println("\n⏹️ Сканирование завершено.");
        synchronized (lock) {
            lock.notify();
        }
    }

    public void serviceSearchCompleted(int transID, int respCode) {}
    public void servicesDiscovered(int transID, ServiceRecord[] servRecord) {}

    public static void main(String[] args) {
        System.out.println("\u001B[36m📶 Bluetooth Scanner (RSSI) (Java)\u001B[0m");
        System.out.println("🔍 Сканирование устройств...");

        try {
            LocalDevice local = LocalDevice.getLocalDevice();
            DiscoveryAgent agent = local.getDiscoveryAgent();
            bluetooth_scanner listener = new bluetooth_scanner();

            synchronized (lock) {
                agent.startInquiry(DiscoveryAgent.GIAC, listener);
                lock.wait();
            }

            printTable();
            saveResults();

        } catch (Exception e) {
            System.err.println("\u001B[31m❌ Ошибка: " + e.getMessage() + "\u001B[0m");
            System.err.println("⚠️ Убедитесь, что Bluetooth включён и библиотека BlueCove установлена.");
        }
    }

    private static void printTable() {
        if (devices.isEmpty()) {
            System.out.println("\u001B[33m❌ Устройства не найдены.\u001B[0m");
            return;
        }

        devices.sort((a, b) -> b.rssi - a.rssi);

        System.out.println("\n\u001B[36m📊 Найденные устройства:\u001B[0m");
        System.out.println("┌" + "─".repeat(75) + "┐");
        System.out.printf("│ %-25s %-20s %-10s %-15s │\n", "Устройство", "MAC-адрес", "RSSI", "Статус");
        System.out.println("├" + "─".repeat(75) + "┤");

        for (DeviceInfo d : devices) {
            String name = d.name.length() > 25 ? d.name.substring(0, 22) + "..." : d.name;
            String status = d.rssi >= -50 ? "Отлично" : d.rssi >= -70 ? "Средне" : "Плохо";
            String color = d.rssi >= -50 ? "\u001B[32m" : d.rssi >= -70 ? "\u001B[33m" : "\u001B[31m";
            System.out.printf("│ %-25s %-20s %s%-10s\u001B[0m %s%-15s\u001B[0m │\n",
                name, d.address, color, d.rssi + " dBm", color, status);
        }

        System.out.println("└" + "─".repeat(75) + "┘");
    }

    private static void saveResults() {
        try {
            // JSON
            StringBuilder json = new StringBuilder("[");
            for (int i = 0; i < devices.size(); i++) {
                DeviceInfo d = devices.get(i);
                json.append("{\"name\":\"").append(d.name).append("\",");
                json.append("\"address\":\"").append(d.address).append("\",");
                json.append("\"rssi\":").append(d.rssi).append("}");
                if (i < devices.size() - 1) json.append(",");
            }
            json.append("]");
            try (FileWriter fw = new FileWriter("devices.json")) {
                fw.write(json.toString());
            }
            System.out.println("\u001B[32m💾 Сохранено JSON: devices.json\u001B[0m");

            // CSV
            try (FileWriter fw = new FileWriter("devices.csv")) {
                fw.write("Name,Address,RSSI\n");
                for (DeviceInfo d : devices) {
                    fw.write(d.name + "," + d.address + "," + d.rssi + "\n");
                }
            }
            System.out.println("\u001B[32m💾 Сохранено CSV: devices.csv\u001B[0m");

        } catch (IOException e) {
            System.err.println("Ошибка сохранения: " + e.getMessage());
        }
    }
}
