using Client.Classes;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static string _ip;
        static int _port;
        static string _username;
        static string _password;

        static void DisplaySettings()
        {
            Console.Clear();
            Tools.PrintLogo();
            Console.WriteLine("Текущие настройки сервера:");
            Console.WriteLine($"  IP-адрес: {_ip}");
            Console.WriteLine($"  Порт: {_port}");
            Console.WriteLine($"\nдля просмотра всех команд используйте команду /help");
        }
        static void GetSettings()
        {
            var ip = Tools.GetInput("Укажите IP адрес сервера - ",
                s => IPAddress.TryParse(s, out IPAddress address) ? s : "127.0.0.1",
                s => true);

            var port = Tools.GetInput("Укажите порт сервера - ",
                s => int.TryParse(s, out int p) ? p : -1,
                p => p > 1025 && p < 65536);

            var username = Tools.GetInput("Укажите логин - ",
                s => !string.IsNullOrWhiteSpace(s) ? s : "",
                s => !string.IsNullOrWhiteSpace(s));

            var password = Tools.GetInput("Укажите пароль - ",
                s => !string.IsNullOrWhiteSpace(s) ? s : "",
                s => !string.IsNullOrWhiteSpace(s));

            _ip = ip;
            _port = port;
            _username = username;
            _password = password;

            string configFile = "client.cfg";

            try
            {
                var settings = new string[]
                {
            _ip,
            _port.ToString(),
            _username,
            _password
                };

                // Записываем в файл
                File.WriteAllLines(configFile, settings);
                Console.WriteLine("Настройки успешно сохранены в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка при сохранении конфигурации в файл: {ex.Message}");
            }
        }

        static bool GetSettingsFromFile()
        {
            string configFile = "client.cfg";

            if (!System.IO.File.Exists(configFile))
            {
                Console.WriteLine("Файл конфигурации не найден.");
                return false;
            }

            try
            {
                string[] lines = System.IO.File.ReadAllLines(configFile);

                if (lines.Length < 4)
                {
                    Console.WriteLine("Файл конфигурации поврежден или неполный.");
                    return false;
                }

                _ip = lines[0].Trim();
                if (!IPAddress.TryParse(_ip, out _))
                {
                    Console.WriteLine("Ошибка чтения IP-адреса из конфигурационного файла.");
                    return false;
                }

                if (!int.TryParse(lines[1].Trim(), out _port) || _port <= 1025 || _port >= 65536)
                {
                    Console.WriteLine("Ошибка чтения порта из конфигурационного файла.");
                    return false;
                }

                _username = lines[2].Trim();
                if (string.IsNullOrWhiteSpace(_username))
                {
                    Console.WriteLine("Ошибка чтения логина из конфигурационного файла.");
                    return false;
                }

                _password = lines[3].Trim();
                if (string.IsNullOrWhiteSpace(_password))
                {
                    Console.WriteLine("Ошибка чтения пароля из конфигурационного файла.");
                    return false;
                }

                Console.WriteLine("Настройки успешно загружены из файла.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка при чтении конфигурационного файла: {ex.Message}");
                return false;
            }
        }


        static async Task Main(string[] args)
        {
            Tools.PrintLogo();
            if (!GetSettingsFromFile())
            {
                GetSettings();
            }

            var client = new TCPClient(_ip, _port, _username, _password);
            DisplaySettings();
            
            bool isRunning = true;
            while (isRunning)
            {
                var command = Console.ReadLine().ToLower().Split(' ');

                switch (command[0])
                {
                    case "/connect":
                        {
                            await client.ConnectAsync();
                            break;
                        }
                    case "/status":
                        {
                            client.PrintStatus();
                            break;
                        }
                    case "/config":
                        {
                            client.CloseConnection();
                            GetSettings();
                            DisplaySettings();

                            client = new TCPClient(_ip, _port, _username, _password);
                            break;
                        }

                    case "/help":
                        Console.WriteLine("Доступные команды:");
                        Console.WriteLine("  /connect       - подключиться к серверу");
                        Console.WriteLine("  /status        - показать данные текущего подключения");
                        Console.WriteLine("  /config        - сменить настройки клиента");
                        Console.WriteLine("  /help          - список команд");
                        break;

                    default:
                        Console.WriteLine("Неизвестная команда. Используйте /help для справки.");
                        break;
                }
            }
        }
    }
}
