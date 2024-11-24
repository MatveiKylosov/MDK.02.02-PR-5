using Client.Classes;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static TCPClient CreateClient()
        {
#if false
                var ip = Tools.GetInput("Укажите IP адрес сервера - ",
                   s => IPAddress.TryParse(s, out IPAddress address) ? address : IPAddress.Any,
                   s => true);

                var port = Tools.GetInput("Укажите порт сервера - ",
                                    s => int.TryParse(s, out int p) ? p : -1,
                                    p => p > 1025 && p < 65536);
                var client = new TCPClient(ip.ToString(), port);
#else
            var ip = "127.0.0.1";
            var port = 1337;
#endif
            var client = new TCPClient(ip, port);
            return client;
        }
        static async Task Main(string[] args)
        {
            Tools.PrintLogo();
            Console.WriteLine("Используйте /help для справки.");
            var client = CreateClient();

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
                            client = CreateClient();
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
