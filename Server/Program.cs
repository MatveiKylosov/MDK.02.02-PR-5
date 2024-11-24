using System;
using System.Net;
using System.Threading.Tasks;
using Server.Classes;
using Common;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Tools.PrintLogo();
            while (true) 
            {
#if false
            var ip = Tools.GetInput("Укажите IP адрес сервера - ",
                               s => IPAddress.TryParse(s, out IPAddress address) ? address : IPAddress.Any,
                               s => true);

            var port = Tools.GetInput("Укажите порт сервера - ",
                                s => int.TryParse(s, out int p) ? p : -1,
                                p => p > 1025 && p < 65536);

            var maxClients = Tools.GetInput("Укажите максимальное количество клиентов - ",
                                            s => int.TryParse(s, out int q) ? q : -1,
                                            q => q > 0);

            var tokenLifetime = Tools.GetInput("Укажите сколько будет активен токен в секундах (60 или более) - ",
                                          s => ulong.TryParse(s, out ulong t) ? t : 60,
                                          t => t > 60);

            Console.WriteLine($"\nдля просмотра всех команд используйте команду /help");

            // Создание и запуск сервера
            var server = new TCPServer(ip, port, maxClients, tokenLifetime);
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
                        Console.WriteLine("  /config       - сменить настройки сервер");
                        Console.WriteLine("  /help         - список команд");
                        break;

                    case "/config":
                        server.DisconnectAll();
                        isRunning = false;
                        break;

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
