using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)      
        {
            var ip = Tools.GetInput("Укажите IP адрес сервера - ",
                   s => IPAddress.TryParse(s, out IPAddress address) ? address : IPAddress.Any,
                   s => true);

            var port = Tools.GetInput("Укажите порт сервера - ",
                                s => int.TryParse(s, out int p) ? p : -1,
                                p => p > 1025 && p < 65536);
        }
    }
}
