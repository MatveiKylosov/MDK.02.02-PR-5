using Client.Classes;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static TCPClient CreateClient()
        {
#if true
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

#else
            var ip = "127.0.0.1";
            var port = 1337;
            var username = "kylosov";
            var password = "Asdfg123";
#endif
            var client = new TCPClient(ip, port, username, password);
            return client;
        }

        static async Task Main(string[] args)
        {
            Tools.PrintLogo();
            var client = CreateClient();

            Console.WriteLine("Используйте /help для справки.");

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
