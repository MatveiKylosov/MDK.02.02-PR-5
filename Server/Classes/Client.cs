using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class Client : Common.Client
    {
        [JsonIgnore]
        public Socket Socket;

        public Client(Socket socket)
        {
            Socket = socket;
        }
        public string GetClientIpAddress()
        {
            if (Socket?.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint.Address.ToString();
            }
            return string.Empty;
        }

    }
}
