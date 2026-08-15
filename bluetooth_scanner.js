// bluetooth_scanner.js — JavaScript версия

const noble = require('noble');
const fs = require('fs');
const readline = require('readline');

class BluetoothScanner {
    constructor() {
        this.devices = new Map();
        this.startTime = Date.now();
    }

    classifyRSSI(rssi) {
        if (rssi >= -50) return { status: 'Отлично', color: '\x1b[32m' };
        if (rssi >= -70) return { status: 'Средне', color: '\x1b[33m' };
        return { status: 'Плохо', color: '\x1b[31m' };
    }

    printTable() {
        if (this.devices.size === 0) {
            console.log('\x1b[33m❌ Устройства не найдены.\x1b[0m');
            return;
        }

        // Сортировка по RSSI
        const sorted = Array.from(this.devices.values()).sort((a, b) => b.rssi - a.rssi);

        console.log('\x1b[36m📊 Найденные устройства:\x1b[0m');
        console.log('┌' + '─'.repeat(75) + '┐');
        console.log(`│ ${'Устройство'.padEnd(25)} ${'MAC-адрес'.padEnd(20)} ${'RSSI'.padEnd(10)} ${'Статус'.padEnd(15)} │`);
        console.log('├' + '─'.repeat(75) + '┤');

        for (const d of sorted) {
            const name = d.name || 'Неизвестно';
            const shortName = name.length > 25 ? name.slice(0, 22) + '...' : name;
            const { status, color } = this.classifyRSSI(d.rssi);
            const rssiStr = `${d.rssi} dBm`;
            console.log(`│ ${shortName.padEnd(25)} ${d.address.padEnd(20)} ${color}${rssiStr.padEnd(10)}\x1b[0m ${color}${status.padEnd(15)}\x1b[0m │`);
        }

        console.log('└' + '─'.repeat(75) + '┘');
    }

    saveResults() {
        const data = {
            timestamp: new Date().toISOString(),
            devices: Array.from(this.devices.values()).map(d => ({
                name: d.name || 'Неизвестно',
                address: d.address,
                rssi: d.rssi
            }))
        };

        fs.writeFileSync('devices.json', JSON.stringify(data, null, 2));
        console.log('\x1b[32m💾 Сохранено JSON: devices.json\x1b[0m');

        let csv = 'Name,Address,RSSI\n';
        for (const d of data.devices) {
            csv += `${d.name},${d.address},${d.rssi}\n`;
        }
        fs.writeFileSync('devices.csv', csv);
        console.log('\x1b[32m💾 Сохранено CSV: devices.csv\x1b[0m');
    }

    start() {
        console.log('\x1b[36m📶 Bluetooth Scanner (RSSI) (JavaScript)\x1b[0m');
        console.log('🔍 Сканирование устройств... (нажмите Ctrl+C для остановки)');

        noble.on('stateChange', (state) => {
            if (state === 'poweredOn') {
                noble.startScanning([], true);
            } else {
                console.log(`⚠️ Bluetooth состояние: ${state}`);
            }
        });

        noble.on('discover', (peripheral) => {
            const address = peripheral.address;
            const name = peripheral.advertisement.localName || 'Неизвестно';
            const rssi = peripheral.rssi;

            // Обновляем информацию об устройстве
            this.devices.set(address, { name, address, rssi });

            // Очищаем экран и выводим таблицу
            console.clear();
            this.printTable();
        });

        // Обработка выхода
        process.on('SIGINT', () => {
            noble.stopScanning();
            console.log('\n⏹️ Сканирование остановлено.');
            this.saveResults();
            process.exit(0);
        });
    }
}

// Проверка зависимостей
try {
    require.resolve('noble');
} catch (e) {
    console.error('\x1b[31m❌ Установите noble: npm install noble\x1b[0m');
    process.exit(1);
}

const scanner = new BluetoothScanner();
scanner.start();
