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
        public NetworkStream NetworkStream;

        public Client(TcpClient tcpClient, Common.Client baseClient)
        {
            TCPClient = tcpClient;
            Token = baseClient.Token;
            ConnectionTime = baseClient.ConnectionTime;
            Work = baseClient.Work;
            NetworkStream = tcpClient.GetStream();
        }
    }
}
