using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Classes
{
    public class Client : Common.Client
    {
        [JsonIgnore]
        public TcpClient TCPClient;
        [JsonIgnore]
        public NetworkStream NetworkStream;

        public Client(TcpClient tcpClient)
        {
            TCPClient = tcpClient;
            NetworkStream = tcpClient.GetStream();
        }
    }
}
