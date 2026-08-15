

### 1. `bluetooth_scanner.py` (Python)

```python
# bluetooth_scanner.py — Python версия

import asyncio
import json
import csv
import sys
import time
from datetime import datetime
from bleak import BleakScanner
from colorama import init, Fore, Style

init(autoreset=True)

class BluetoothScanner:
    def __init__(self, timeout=5, continuous=False):
        self.timeout = timeout
        self.continuous = continuous
        self.devices = []

    def classify_rssi(self, rssi):
        """Классифицирует качество сигнала по RSSI."""
        if rssi is None:
            return "N/A", Fore.YELLOW
        if rssi >= -50:
            return "Отлично", Fore.GREEN
        elif rssi >= -70:
            return "Средне", Fore.YELLOW
        else:
            return "Плохо", Fore.RED

    async def scan(self):
        """Выполняет сканирование Bluetooth-устройств."""
        print(f"{Fore.CYAN}📶 Bluetooth Scanner (RSSI) (Python)")
        print(f"🔍 Сканирование устройств... (таймаут: {self.timeout} сек)")

        if self.continuous:
            print("🔄 Непрерывный режим (нажмите Ctrl+C для остановки)")
            while True:
                self.devices = []
                devices = await BleakScanner.discover(timeout=self.timeout)
                self.devices = devices
                self.print_table()
                await asyncio.sleep(self.timeout)
        else:
            devices = await BleakScanner.discover(timeout=self.timeout)
            self.devices = devices
            self.print_table()
            self.save_json()
            self.save_csv()

    def print_table(self):
        """Выводит таблицу с устройствами."""
        if not self.devices:
            print(Fore.YELLOW + "❌ Устройства не найдены.")
            return

        # Сортировка по RSSI (от сильного к слабому)
        sorted_devices = sorted(self.devices, key=lambda d: d.rssi if d.rssi else -100, reverse=True)

        print(f"\n{Fore.CYAN}📊 Найденные устройства:{Style.RESET_ALL}")
        print("┌" + "─" * 75 + "┐")
        print(f"│ {'Устройство':<25} {'MAC-адрес':<20} {'RSSI':<10} {'Статус':<15} │")
        print("├" + "─" * 75 + "┤")

        for device in sorted_devices:
            name = device.name or "Неизвестно"
            if len(name) > 25:
                name = name[:22] + "..."
            rssi = device.rssi if device.rssi else 0
            status, color = self.classify_rssi(rssi)
            rssi_str = f"{rssi} dBm" if rssi else "N/A"
            print(f"│ {name:<25} {device.address:<20} {color}{rssi_str:<10}{Style.RESET_ALL} {color}{status:<15}{Style.RESET_ALL} │")

        print("└" + "─" * 75 + "┘")

    def save_json(self, filename="devices.json"):
        """Сохраняет результаты в JSON."""
        data = {
            "timestamp": datetime.now().isoformat(),
            "devices": [
                {
                    "name": d.name or "Неизвестно",
                    "address": d.address,
                    "rssi": d.rssi,
                    "metadata": d.metadata if hasattr(d, 'metadata') else {}
                }
                for d in self.devices
            ]
        }
        with open(filename, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print(f"{Fore.GREEN}💾 Сохранено JSON: {filename}")

    def save_csv(self, filename="devices.csv"):
        """Сохраняет результаты в CSV."""
        if not self.devices:
            return
        with open(filename, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(["Name", "Address", "RSSI"])
            for d in self.devices:
                writer.writerow([d.name or "Неизвестно", d.address, d.rssi])
        print(f"{Fore.GREEN}💾 Сохранено CSV: {filename}")

async def main():
    timeout = 5
    continuous = False

    if len(sys.argv) > 1:
        if sys.argv[1] == "--continuous" or sys.argv[1] == "-c":
            continuous = True
        if len(sys.argv) > 2:
            try:
                timeout = int(sys.argv[2])
            except:
                pass

    scanner = BluetoothScanner(timeout, continuous)
    try:
        await scanner.scan()
    except KeyboardInterrupt:
        print(f"\n{Fore.YELLOW}⏹️ Сканирование остановлено.")
    except Exception as e:
        print(f"{Fore.RED}❌ Ошибка: {e}")

if __name__ == "__main__":
    asyncio.run(main())
