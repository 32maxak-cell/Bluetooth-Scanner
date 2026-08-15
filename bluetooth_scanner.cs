// bluetooth_scanner.cs — C# версия

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using InTheHand.Bluetooth;
using InTheHand.BluetoothLE;

class DeviceInfo
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int RSSI { get; set; }
}

class BluetoothScanner
{
    private static List<DeviceInfo> devices = new List<DeviceInfo>();
    private static bool running = true;

    static void Main(string[] args)
    {
        Console.WriteLine("\u001B[36m📶 Bluetooth Scanner (RSSI) (C#)\u001B[0m");
        Console.WriteLine("🔍 Сканирование устройств... (нажмите Ctrl+C для остановки)");

        // Обработка Ctrl+C
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            running = false;
            Console.WriteLine("\n⏹️ Сканирование остановлено.");
            PrintTable();
            SaveResults();
        };

        ScanDevices();
    }

    static async void ScanDevices()
    {
        try
        {
            var adapter = BluetoothAdapter.Default;
            if (adapter == null)
            {
                Console.WriteLine("\u001B[31m❌ Bluetooth не найден.\u001B[0m");
                return;
            }

            if (!adapter.IsDiscovering)
            {
                adapter.DeviceDiscovered += OnDeviceDiscovered;
                adapter.ScanMode = ScanMode.LowLatency;
                adapter.StartDiscovery();
            }

            while (running)
            {
                Thread.Sleep(1000);
                Console.Clear();
                PrintTable();
            }

            adapter.StopDiscovery();
        }
        catch (Exception e)
        {
            Console.WriteLine($"\u001B[31m❌ Ошибка: {e.Message}\u001B[0m");
            Console.WriteLine("⚠️ Убедитесь, что Bluetooth включён и библиотека InTheHand.BluetoothLE установлена.");
        }
    }

    static void OnDeviceDiscovered(object sender, DeviceDiscoveredEventArgs e)
    {
        var device = e.Device;
        var name = device.Name ?? "Неизвестно";
        var address = device.Address?.ToString() ?? "N/A";
        var rssi = device.RSSI ?? -100;

        // Обновляем или добавляем устройство
        var existing = devices.FirstOrDefault(d => d.Address == address);
        if (existing != null)
        {
            existing.Name = name;
            existing.RSSI = rssi;
        }
        else
        {
            devices.Add(new DeviceInfo { Name = name, Address = address, RSSI = rssi });
        }
    }

    static void PrintTable()
    {
        if (devices.Count == 0)
        {
            Console.WriteLine("\u001B[33m❌ Устройства не найдены.\u001B[0m");
            return;
        }

        var sorted = devices.OrderByDescending(d => d.RSSI).ToList();

        Console.WriteLine("\n\u001B[36m📊 Найденные устройства:\u001B[0m");
        Console.WriteLine("┌" + "─".Repeat(75) + "┐");
        Console.WriteLine($"│ {"Устройство",-25} {"MAC-адрес",-20} {"RSSI",-10} {"Статус",-15} │");
        Console.WriteLine("├" + "─".Repeat(75) + "┤");

        foreach (var d in sorted)
        {
            string name = d.Name.Length > 25 ? d.Name.Substring(0, 22) + "..." : d.Name;
            string status = d.RSSI >= -50 ? "Отлично" : d.RSSI >= -70 ? "Средне" : "Плохо";
            string color = d.RSSI >= -50 ? "\u001B[32m" : d.RSSI >= -70 ? "\u001B[33m" : "\u001B[31m";
            Console.WriteLine($"│ {name,-25} {d.Address,-20} {color}{d.RSSI,-10} dBm\u001B[0m {color}{status,-15}\u001B[0m │");
        }

        Console.WriteLine("└" + "─".Repeat(75) + "┘");
    }

    static void SaveResults()
    {
        try
        {
            // JSON
            var json = JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("devices.json", json);
            Console.WriteLine("\u001B[32m💾 Сохранено JSON: devices.json\u001B[0m");

            // CSV
            using var writer = new StreamWriter("devices.csv");
            writer.WriteLine("Name,Address,RSSI");
            foreach (var d in devices)
            {
                writer.WriteLine($"{d.Name},{d.Address},{d.RSSI}");
            }
            Console.WriteLine("\u001B[32m💾 Сохранено CSV: devices.csv\u001B[0m");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ошибка сохранения: {e.Message}");
        }
    }
}

public static class StringExtensions
{
    public static string Repeat(this string str, int count)
    {
        return new string(str[0], count);
    }
}
