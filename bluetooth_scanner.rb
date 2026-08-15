# bluetooth_scanner.rb — Ruby версия

require 'rble'
require 'json'
require 'csv'
require 'time'

class BluetoothScanner
  def initialize
    @devices = {}
    @running = true
  end

  def classify_rssi(rssi)
    if rssi >= -50
      ["Отлично", "\e[32m"]
    elsif rssi >= -70
      ["Средне", "\e[33m"]
    else
      ["Плохо", "\e[31m"]
    end
  end

  def print_table
    if @devices.empty?
      puts "\e[33m❌ Устройства не найдены.\e[0m"
      return
    end

    sorted = @devices.values.sort_by { |d| -d[:rssi] }

    puts "\n\e[36m📊 Найденные устройства:\e[0m"
    puts "┌" + "─" * 75 + "┐"
    puts "│ #{'Устройство'.ljust(25)} #{'MAC-адрес'.ljust(20)} #{'RSSI'.ljust(10)} #{'Статус'.ljust(15)} │"
    puts "├" + "─" * 75 + "┤"

    sorted.each do |d|
      name = d[:name] || "Неизвестно"
      name = name[0...22] + "..." if name.length > 25
      status, color = classify_rssi(d[:rssi])
      rssi_str = "#{d[:rssi]} dBm"
      puts "│ #{name.ljust(25)} #{d[:address].ljust(20)} #{color}#{rssi_str.ljust(10)}\e[0m #{color}#{status.ljust(15)}\e[0m │"
    end

    puts "└" + "─" * 75 + "┘"
  end

  def save_results
    data = {
      timestamp: Time.now.iso8601,
      devices: @devices.values
    }

    File.write("devices.json", JSON.pretty_generate(data))
    puts "\e[32m💾 Сохранено JSON: devices.json\e[0m"

    CSV.open("devices.csv", "w") do |csv|
      csv << ["Name", "Address", "RSSI"]
      @devices.values.each do |d|
        csv << [d[:name] || "Неизвестно", d[:address], d[:rssi]]
      end
    end
    puts "\e[32m💾 Сохранено CSV: devices.csv\e[0m"
  end

  def start
    puts "\e[36m📶 Bluetooth Scanner (RSSI) (Ruby)\e[0m"
    puts "🔍 Сканирование устройств... (нажмите Ctrl+C для остановки)"

    trap("INT") do
      @running = false
      puts "\n⏹️ Сканирование остановлено."
      print_table
      save_results
      exit
    end

    # Используем RBLE для сканирования
    begin
      RBLE.scan(timeout: 0) do |device|
        address = device.address
        name = device.name || "Неизвестно"
        rssi = device.rssi

        @devices[address] = {
          name: name,
          address: address,
          rssi: rssi
        }

        # Очищаем экран и выводим таблицу
        system("clear") || system("cls")
        print_table
      end
    rescue => e
      puts "\e[31m❌ Ошибка: #{e.message}\e[0m"
      puts "⚠️ Убедитесь, что Bluetooth включён и gem 'rble' установлен."
    end
  end
end

# Проверка зависимостей
begin
  require 'rble'
rescue LoadError
  puts "\e[31m❌ Установите rble: gem install rble\e[0m"
  exit 1
end

scanner = BluetoothScanner.new
scanner.start
