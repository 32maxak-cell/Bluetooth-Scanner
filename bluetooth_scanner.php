<?php
// bluetooth_scanner.php — PHP версия

class BluetoothScanner {
    private $devices = [];
    private $running = true;

    public function __construct() {
        // Проверяем наличие bluetoothctl
        exec('which bluetoothctl', $output, $return);
        if ($return !== 0) {
            echo "\033[31m❌ bluetoothctl не найден. Установите bluez-utils.\033[0m\n";
            exit(1);
        }
    }

    private function classifyRSSI($rssi) {
        if ($rssi >= -50) {
            return ["Отлично", "\033[32m"];
        } elseif ($rssi >= -70) {
            return ["Средне", "\033[33m"];
        } else {
            return ["Плохо", "\033[31m"];
        }
    }

    private function scanDevices() {
        // Запускаем сканирование
        exec('bluetoothctl scan on 2>&1', $output, $return);
        
        // Читаем вывод в течение нескольких секунд
        $timeout = 10;
        $start = time();
        $devices = [];

        while (time() - $start < $timeout) {
            exec('bluetoothctl devices', $deviceOutput);
            foreach ($deviceOutput as $line) {
                if (preg_match('/Device\s+([0-9A-F:]+)\s+(.+)/', $line, $matches)) {
                    $address = $matches[1];
                    $name = $matches[2];
                    
                    // Получаем RSSI для устройства
                    exec("bluetoothctl info $address 2>&1", $infoOutput);
                    $rssi = -100;
                    foreach ($infoOutput as $infoLine) {
                        if (preg_match('/RSSI:\s+(-?\d+)/', $infoLine, $rssiMatch)) {
                            $rssi = (int)$rssiMatch[1];
                            break;
                        }
                    }
                    
                    $devices[$address] = [
                        'name' => $name,
                        'address' => $address,
                        'rssi' => $rssi
                    ];
                }
            }
            usleep(500000); // 0.5 сек
        }

        // Останавливаем сканирование
        exec('bluetoothctl scan off 2>&1');

        return $devices;
    }

    private function printTable($devices) {
        if (empty($devices)) {
            echo "\033[33m❌ Устройства не найдены.\033[0m\n";
            return;
        }

        // Сортировка по RSSI
        usort($devices, function($a, $b) {
            return $b['rssi'] - $a['rssi'];
        });

        echo "\n\033[36m📊 Найденные устройства:\033[0m\n";
        echo "┌" . str_repeat("─", 75) . "┐\n";
        echo "│ " . str_pad("Устройство", 25) . " " . str_pad("MAC-адрес", 20) . " " . str_pad("RSSI", 10) . " " . str_pad("Статус", 15) . " │\n";
        echo "├" . str_repeat("─", 75) . "┤\n";

        foreach ($devices as $d) {
            $name = $d['name'] ?: "Неизвестно";
            if (strlen($name) > 25) {
                $name = substr($name, 0, 22) . "...";
            }
            list($status, $color) = $this->classifyRSSI($d['rssi']);
            $rssiStr = $d['rssi'] . " dBm";
            echo "│ " . str_pad($name, 25) . " " . str_pad($d['address'], 20) . " " . $color . str_pad($rssiStr, 10) . "\033[0m " . $color . str_pad($status, 15) . "\033[0m │\n";
        }

        echo "└" . str_repeat("─", 75) . "┘\n";
    }

    private function saveResults($devices) {
        $data = [
            'timestamp' => date('c'),
            'devices' => array_values($devices)
        ];

        file_put_contents('devices.json', json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
        echo "\033[32m💾 Сохранено JSON: devices.json\033[0m\n";

        $csv = "Name,Address,RSSI\n";
        foreach ($devices as $d) {
            $csv .= ($d['name'] ?: "Неизвестно") . "," . $d['address'] . "," . $d['rssi'] . "\n";
        }
        file_put_contents('devices.csv', $csv);
        echo "\033[32m💾 Сохранено CSV: devices.csv\033[0m\n";
    }

    public function start() {
        echo "\033[36m📶 Bluetooth Scanner (RSSI) (PHP)\033[0m\n";
        echo "🔍 Сканирование устройств... (это займёт ~10 секунд)\n";

        $devices = $this->scanDevices();
        $this->printTable($devices);
        
        if (!empty($devices)) {
            $this->saveResults($devices);
        }
    }
}

// Обработка сигналов для корректного завершения
pcntl_signal(SIGINT, function() {
    echo "\n⏹️ Сканирование остановлено.\n";
    exit(0);
});

$scanner = new BluetoothScanner();
$scanner->start();
?>
