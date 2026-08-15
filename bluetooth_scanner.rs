// bluetooth_scanner.rs — Rust версия

use bluer::{
    adapter::{Adapter, DiscoveryFilter},
    Address,
};
use colored::*;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fs;
use std::time::Duration;
use tokio::time;

#[derive(Debug, Serialize, Deserialize, Clone)]
struct DeviceInfo {
    name: String,
    address: String,
    rssi: i16,
}

fn classify_rssi(rssi: i16) -> (String, String) {
    if rssi >= -50 {
        ("Отлично".to_string(), "green".to_string())
    } else if rssi >= -70 {
        ("Средне".to_string(), "yellow".to_string())
    } else {
        ("Плохо".to_string(), "red".to_string())
    }
}

fn print_table(devices: &HashMap<String, DeviceInfo>) {
    if devices.is_empty() {
        println!("{}", "❌ Устройства не найдены.".yellow());
        return;
    }

    let mut sorted: Vec<&DeviceInfo> = devices.values().collect();
    sorted.sort_by(|a, b| b.rssi.cmp(&a.rssi));

    println!("\n{}", "📊 Найденные устройства:".cyan());
    println!("┌{}┐", "─".repeat(75));
    println!("│ {:<25} {:<20} {:<10} {:<15} │", "Устройство", "MAC-адрес", "RSSI", "Статус");
    println!("├{}┤", "─".repeat(75));

    for d in sorted {
        let name = if d.name.len() > 25 {
            format!("{}...", &d.name[..22])
        } else {
            d.name.clone()
        };
        let (status, color) = classify_rssi(d.rssi);
        let rssi_str = format!("{} dBm", d.rssi);
        let colored_rssi = match color.as_str() {
            "green" => rssi_str.green(),
            "yellow" => rssi_str.yellow(),
            "red" => rssi_str.red(),
            _ => rssi_str.normal(),
        };
        let colored_status = match color.as_str() {
            "green" => status.green(),
            "yellow" => status.yellow(),
            "red" => status.red(),
            _ => status.normal(),
        };
        println!("│ {:<25} {:<20} {:<10} {:<15} │",
            name, d.address, colored_rssi, colored_status);
    }

    println!("└{}┘", "─".repeat(75));
}

fn save_results(devices: &HashMap<String, DeviceInfo>) {
    let list: Vec<DeviceInfo> = devices.values().cloned().collect();

    // JSON
    if let Ok(json) = serde_json::to_string_pretty(&list) {
        fs::write("devices.json", json).unwrap();
        println!("{}", "💾 Сохранено JSON: devices.json".green());
    }

    // CSV
    let mut csv = String::from("Name,Address,RSSI\n");
    for d in list {
        csv.push_str(&format!("{},{},{}\n", d.name, d.address, d.rssi));
    }
    fs::write("devices.csv", csv).unwrap();
    println!("{}", "💾 Сохранено CSV: devices.csv".green());
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("{}", "📶 Bluetooth Scanner (RSSI) (Rust)".cyan());
    println!("{}", "🔍 Сканирование устройств... (нажмите Ctrl+C для остановки)".cyan());

    let session = bluer::Session::new().await?;
    let adapter = session.default_adapter().await?;
    adapter.set_powered(true).await?;

    let mut devices: HashMap<String, DeviceInfo> = HashMap::new();
    let mut interval = time::interval(Duration::from_secs(2));

    let filter = DiscoveryFilter {
        duplicate: Some(true),
        ..Default::default()
    };

    let mut device_events = adapter.discover_devices_with_filter(&filter).await?;

    tokio::spawn(async move {
        while let Some(device) = device_events.next().await {
            if let Ok(addr) = device.address() {
                let address = addr.to_string();
                let name = device.name().await.unwrap_or_else(|_| "Неизвестно".to_string());
                let rssi = device.rssi().await.unwrap_or(-100);

                devices.insert(address.clone(), DeviceInfo {
                    name,
                    address,
                    rssi,
                });
            }
        }
    });

    // Ждём завершения по Ctrl+C
    tokio::signal::ctrl_c().await?;
    println!("\n{}", "⏹️ Сканирование остановлено.".yellow());

    print_table(&devices);
    save_results(&devices);

    Ok(())
}
