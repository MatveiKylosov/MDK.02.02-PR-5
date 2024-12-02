using System;
using System.Net;
using System.Threading.Tasks;
using Server.Classes;
using Common;
using System.IO;
using System.Diagnostics;

namespace Server
{
    internal class Program
    {
        static private IPAddress _address;
        static private int _port;
        static private int _maxClients;
        static private ulong _tokenLifetime;

        static void DisplaySettings()
        {
            Console.Clear();
            Tools.PrintLogo();
            Console.WriteLine("Текущие настройки сервера:");
            Console.WriteLine($"  IP-адрес: {_address}");
            Console.WriteLine($"  Порт: {_port}");
            Console.WriteLine($"  Максимальное количество клиентов: {_maxClients}");
            Console.WriteLine($"  Время жизни токена (сек.): {_tokenLifetime}");
            Console.WriteLine($"\nдля просмотра всех команд используйте команду /help");
        }


        static void GetSettings()
        {
            var ip = Tools.GetInput("Укажите IP адрес сервера - ",
                   s => IPAddress.TryParse(s, out IPAddress address) ? address : IPAddress.Any,
                   s => true);

            var port = Tools.GetInput("Укажите порт сервера (от 1025 до 65536) - ",
                                s => int.TryParse(s, out int p) ? p : -1,
                                p => p > 1025 && p < 65536);

            var maxClients = Tools.GetInput("Укажите максимальное количество клиентов - ",
                                            s => int.TryParse(s, out int q) ? q : -1,
                                            q => q > 0);

            var tokenLifetime = Tools.GetInput("Укажите сколько будет активен токен в секундах (60 или более) - ",
                                          s => ulong.TryParse(s, out ulong t) ? t : 60,
                                          t => t >= 60);

            _address = ip;
            _port = port;
            _maxClients = maxClients;
            _tokenLifetime = tokenLifetime;

            using (StreamWriter writer = new StreamWriter("server.cfg"))
            {
                writer.WriteLine(_address.ToString());
                writer.WriteLine(_port.ToString());
                writer.WriteLine(_maxClients.ToString());
                writer.WriteLine(_tokenLifetime.ToString());
            }
        }

        static bool GetSettingsFromFile()
        {
            string configFile = "server.cfg";

            if (!File.Exists(configFile))
            {
                Console.WriteLine("Файл конфигурации не найден.");
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(configFile);

                if (lines.Length < 4)
                {
                    Console.WriteLine("Файл конфигурации поврежден или неполный.");
                    return false;
                }

                if (!IPAddress.TryParse(lines[0], out _address))
                {
                    Console.WriteLine("Ошибка чтения IP-адреса из конфигурационного файла.");
                    return false;
                }

                if (!int.TryParse(lines[1], out _port) || _port <= 1025 || _port >= 65536)
                {
                    Console.WriteLine("Ошибка чтения порта из конфигурационного файла.");
                    return false;
                }

                if (!int.TryParse(lines[2], out _maxClients) || _maxClients <= 0)
                {
                    Console.WriteLine("Ошибка чтения максимального количества клиентов.");
                    return false;
                }

                if (!ulong.TryParse(lines[3], out _tokenLifetime) || _tokenLifetime < 60)
                {
                    Console.WriteLine("Ошибка чтения времени жизни токена.");
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
            while (true) 
            {
#if true
                if (!GetSettingsFromFile())
                {
                    GetSettings();
                }

                DisplaySettings();

                // Создание и запуск сервера
                var server = new TCPServer(_address, _port, _maxClients, _tokenLifetime);
#else
                var server = new TCPServer(IPAddress.Parse("127.0.0.1"), 1337, 5, 160);
#endif

                Console.WriteLine("Используйте /help для справки.");
                // Запускаем сервер в отдельном потоке
                var serverTask = server.StartAsync();

                // Обрабатываем консольный ввод
                HandleConsoleInput(server);

                // Ожидаем завершения работы сервера
                await serverTask;
            }
        }

        private static void HandleConsoleInput(TCPServer server)
        {
            bool isRunning = true;

            while (isRunning)
            {
                var command = Console.ReadLine().ToLower().Split(' ');

                switch (command[0])
                {
                    case "/disconnect":
                        {
                            if (command.Length == 1)
                            {
                                Console.Write($"Для отключение укажите токен клиента.");
                            }
                            else
                            {
                                server.Disconnect(command[1]);
                            }
                            break;
                        }

                    case "/status":
                        server.ListConnectedClients();
                        break;

                    case "/help":
                        Console.WriteLine("Доступные команды:");
                        Console.WriteLine("  /status       - показать список подключенных клиентов");
                        Console.WriteLine("  /disconnect   - отключить клиента");
                        Console.WriteLine("  /block        - добавить в чёрный список клиента");
                        Console.WriteLine("  /config       - сменить настройки сервер");
                        Console.WriteLine("  /help         - список команд");
                        break;

                    case "/config":
                        {
                            server.DisconnectAll();
                            isRunning = false;
                            GetSettings();
                            DisplaySettings();
                            break;
                        }

                    case "/block":
                        {
                            if (command.Length == 1)
                            {
                                Console.Write($"Чтобы заблокировать пользователя, необходимо указать токен клиента.");
                            }
                            else
                            {
                                server.BlockClient(command[1]);
                            }
                            break;
                        }

                    default:
                        Console.WriteLine("Неизвестная команда. Используйте /help для справки.");
                        break;
                }
            }
        }
    }
}
