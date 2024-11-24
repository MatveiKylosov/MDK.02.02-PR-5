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
                var client = new TCPClient(ip.ToString(), port);
#else
                var client = new TCPClient("127.0.0.1", 1337);
#endif

                await client.ConnectAsync();
                Console.ReadLine();
            }
        }
    }
}
