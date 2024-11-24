using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Classes
{
    public class TCPClient
    {
        private Client _client;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private const ulong _sizeBuffer = 2048;

        public TCPClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var clientSocket = new TcpClient(_serverIp, _serverPort);

                var receivedMessage = await ReceiveMessageAsync(clientSocket);

                if(receivedMessage == null || receivedMessage.Command == "Disconnect")
                {
                    Console.WriteLine($"Не удалось подключиться к серверу. Ответ от сервера - {(receivedMessage != null ? receivedMessage.Data : "")}");
                    return;
                }

                var client = JsonConvert.DeserializeObject<Common.Client>(receivedMessage.Data);
                _client = new Client(clientSocket, client);

                // Закрытие соединения
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task SendMessageAsync(TcpClient client, CommandMessasge commandMessasge)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    var message = JsonConvert.SerializeObject(commandMessasge);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        private async Task<CommandMessasge> ReceiveMessageAsync(TcpClient client)
        {
            var buffer = new byte[_sizeBuffer];
            int bytesRead;
            try
            {
                using (var stream = client.GetStream())
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        return null;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return JsonConvert.DeserializeObject<CommandMessasge>(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения сообщения: {ex.Message}");
                return null;
            }
        }
    }
}
