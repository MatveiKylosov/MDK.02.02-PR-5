using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class Tools
    {
        static public void PrintLogo()
        {
            Console.WriteLine(@"           /$$                 /$$                                 ");
            Console.WriteLine(@"          | $$                | $$                                 ");
            Console.WriteLine(@"  /$$$$$$ | $$$$$$$   /$$$$$$ | $$$$$$$   /$$$$$$                  ");
            Console.WriteLine(@" |____  $$| $$__  $$ /$$__  $$| $$__  $$ |____  $$                 ");
            Console.WriteLine(@"  /$$$$$$$| $$  \ $$| $$  \ $$| $$  \ $$  /$$$$$$$                 ");
            Console.WriteLine(@" /$$__  $$| $$  | $$| $$  | $$| $$  | $$ /$$__  $$                 ");
            Console.WriteLine(@"|  $$$$$$$| $$$$$$$/|  $$$$$$/| $$$$$$$/|  $$$$$$$                 ");
            Console.WriteLine(@" \_______/|_______/  \______/ |_______/  \_______/                 ");
            Console.WriteLine(@"                                                                   ");
            Console.WriteLine(@"                                                                   ");
            Console.WriteLine(@"                                                                   ");
            Console.WriteLine(@"                                           /$$                     ");
            Console.WriteLine(@"                                          | $$                     ");
            Console.WriteLine(@"                                      /$$$$$$$  /$$$$$$  /$$    /$$");
            Console.WriteLine(@"                                     /$$__  $$ /$$__  $$|  $$  /$$/");
            Console.WriteLine(@"                                    | $$  | $$| $$$$$$$$ \  $$/$$/ ");
            Console.WriteLine(@"                                    | $$  | $$| $$_____/  \  $$$/  ");
            Console.WriteLine(@"                                    |  $$$$$$$|  $$$$$$$   \  $/   ");
            Console.WriteLine(@"                                     \_______/ \_______/    \_/    ");
        }

        public static T GetInput<T>(string prompt, Func<string, T> parse, Func<T, bool> validate)
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine();
                var parsedValue = parse(input);
                if (validate(parsedValue))
                {
                    return parsedValue;
                }
            }
        }
    }
}
