// bluetooth_scanner.go — Go версия

package main

import (
	"encoding/csv"
	"encoding/json"
	"fmt"
	"os"
	"os/signal"
	"syscall"
	"time"

	"tinygo.org/x/bluetooth"
)

type DeviceInfo struct {
	Name    string `json:"name"`
	Address string `json:"address"`
	RSSI    int    `json:"rssi"`
}

func classifyRSSI(rssi int) (string, string) {
	if rssi >= -50 {
		return "Отлично", "\x1b[32m"
	} else if rssi >= -70 {
		return "Средне", "\x1b[33m"
	} else {
		return "Плохо", "\x1b[31m"
	}
}

func main() {
	adapter := bluetooth.DefaultAdapter
	if err := adapter.Enable(); err != nil {
		fmt.Printf("\x1b[31m❌ Ошибка включения Bluetooth: %v\x1b[0m\n", err)
		os.Exit(1)
	}

	fmt.Println("\x1b[36m📶 Bluetooth Scanner (RSSI) (Go)\x1b[0m")
	fmt.Println("🔍 Сканирование устройств... (нажмите Ctrl+C для остановки)")

	devices := make(map[string]DeviceInfo)
	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		<-sigChan
		fmt.Println("\n⏹️ Сканирование остановлено.")
		saveResults(devices)
		os.Exit(0)
	}()

	err := adapter.Scan(func(adapter *bluetooth.Adapter, device bluetooth.ScanResult) {
		name := device.LocalName()
		if name == "" {
			name = "Неизвестно"
		}
		rssi := int(device.RSSI)

		// Обновляем информацию об устройстве
		devices[device.Address.String()] = DeviceInfo{
			Name:    name,
			Address: device.Address.String(),
			RSSI:    rssi,
		}

		// Очищаем экран и выводим таблицу
		fmt.Print("\033[H\033[2J")
		printTable(devices)
	})

	if err != nil {
		fmt.Printf("\x1b[31m❌ Ошибка сканирования: %v\x1b[0m\n", err)
		os.Exit(1)
	}

	// Ждём завершения
	select {}
}

func printTable(devices map[string]DeviceInfo) {
	if len(devices) == 0 {
		fmt.Println("\x1b[33m❌ Устройства не найдены.\x1b[0m")
		return
	}

	fmt.Println("\x1b[36m📊 Найденные устройства:\x1b[0m")
	fmt.Println("┌" + "─"*75 + "┐")
	fmt.Printf("│ %-25s %-20s %-10s %-15s │\n", "Устройство", "MAC-адрес", "RSSI", "Статус")
	fmt.Println("├" + "─"*75 + "┤")

	for _, d := range devices {
		name := d.Name
		if len(name) > 25 {
			name = name[:22] + "..."
		}
		status, color := classifyRSSI(d.RSSI)
		rssiStr := fmt.Sprintf("%d dBm", d.RSSI)
		fmt.Printf("│ %-25s %-20s %s%-10s\x1b[0m %s%-15s\x1b[0m │\n",
			name, d.Address, color, rssiStr, color, status)
	}

	fmt.Println("└" + "─"*75 + "┘")
}

func saveResults(devices map[string]DeviceInfo) {
	var list []DeviceInfo
	for _, d := range devices {
		list = append(list, d)
	}

	// JSON
	jsonData, _ := json.MarshalIndent(list, "", "  ")
	os.WriteFile("devices.json", jsonData, 0644)
	fmt.Println("\x1b[32m💾 Сохранено JSON: devices.json\x1b[0m")

	// CSV
	if len(list) > 0 {
		file, _ := os.Create("devices.csv")
		defer file.Close()
		writer := csv.NewWriter(file)
		defer writer.Flush()
		writer.Write([]string{"Name", "Address", "RSSI"})
		for _, d := range list {
			writer.Write([]string{d.Name, d.Address, fmt.Sprintf("%d", d.RSSI)})
		}
		fmt.Println("\x1b[32m💾 Сохранено CSV: devices.csv\x1b[0m")
	}
}
