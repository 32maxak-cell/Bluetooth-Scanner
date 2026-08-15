# 📶 Bluetooth Scanner (RSSI) — найди все устройства вокруг и узнай силу сигнала

> «Сигнал есть — устройство рядом»

**Bluetooth Scanner (RSSI)** — это набор консольных утилит для сканирования Bluetooth-устройств (Classic и BLE) с отображением уровня сигнала (RSSI) в реальном времени.  
Программа показывает все доступные устройства, их MAC-адреса, имена и силу сигнала в dBm, позволяя оценить расстояние до устройства.

## 🚀 Особенности
- 🔍 Сканирование Bluetooth-устройств (Classic и BLE).
- 📊 Отображение RSSI (уровень сигнала) в dBm.
- 🎨 Цветовая индикация качества сигнала (зелёный — отлично, жёлтый — средне, красный — плохо).
- 📋 Вывод в удобной таблице с сортировкой по RSSI.
- 💾 Сохранение результатов в JSON и CSV.
- ⏱️ Непрерывное сканирование с обновлением каждые N секунд.
- 🖥️ Кроссплатформенная поддержка: Linux, Windows, macOS.
- 🔧 Фильтрация по имени устройства или MAC-адресу.

## 🛠️ Установка и запуск

Для каждого языка — минимальные зависимости.

| Язык       | Библиотека/пакет                     | Команда запуска                         |
|------------|--------------------------------------|-----------------------------------------|
| Python     | `bleak`                              | `python bluetooth_scanner.py`           |
| Go         | `tinygo-org/bluetooth`               | `go run bluetooth_scanner.go`           |
| JavaScript | `noble` (Node.js)                    | `node bluetooth_scanner.js`             |
| Java       | `bluecove` или `javax.bluetooth`     | `javac bluetooth_scanner.java && java bluetooth_scanner` |
| C#         | `InTheHand.BluetoothLE`              | `dotnet run`                            |
| Rust       | `bluer` (Linux) или `bluest`         | `cargo run`                             |
| Ruby       | `rble` или `scan_beacon`             | `ruby bluetooth_scanner.rb`             |
| PHP        | `exec` + `bluetoothctl`              | `php bluetooth_scanner.php`             |

> Для большинства языков требуется установленный Bluetooth-стек (BlueZ на Linux, CoreBluetooth на macOS, Bluetooth LE на Windows 10+).

## 📖 Пример использования

```bash
$ python bluetooth_scanner.py
Вывод:

text
📶 Bluetooth Scanner (RSSI) (Python)
🔍 Сканирование устройств...

📊 Найденные устройства:
┌─────────────────────────────────────────────────────────────┐
│ Устройство              MAC-адрес           RSSI   Статус   │
├─────────────────────────────────────────────────────────────┤
│ iPhone 15 Pro           AA:BB:CC:DD:EE:FF  -45 dBm ✅ Отлично
│ Sony WH-1000XM5         11:22:33:44:55:66  -62 dBm ⚠️ Средне
│ Bluetooth Mouse         77:88:99:AA:BB:CC  -85 dBm ❌ Плохо
└─────────────────────────────────────────────────────────────┘

💾 Сохранено: devices.json
💾 Сохранено: devices.csv
🤝 Вклад
Принимаются улучшения, новые языки, фичи.

📜 Лицензия
MIT — используйте свободно.

Автор: Ваш покорный слуга
