using Common;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Classes
{
    public class TCPClient
    {
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

                // Проверяем, подключен ли сокет
                if (!clientSocket.Connected)
                {
                    Console.WriteLine("Не удалось подключиться к серверу.");
                    return;
                }

                var networkStream = clientSocket.GetStream();

                // Получаем начальное сообщение от сервера
                var receivedMessage = await ReceiveMessageAsync(networkStream);

                if (receivedMessage == null || receivedMessage.Command == "Disconnect")
                {
                    Console.WriteLine($"Не удалось подключиться к серверу. Ответ от сервера - {(receivedMessage != null ? receivedMessage.Data : "")}");
                    return;
                }

                var client = JsonConvert.DeserializeObject<Common.Client>(receivedMessage.Data);
                Client _client = new Client(clientSocket, client);

                // Обработка ответа от сервера
                await HandleServerResponseAsync(_client);

                // Закрытие сокета после завершения всех операций
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task HandleServerResponseAsync(Client _client)
        {
            try
            {
                bool work = true;

                while (work)
                {
                    // Отправка Ping
                    CommandMessasge commandMessasge = new CommandMessasge() { Command = "Ping", Data = "" };
                    await SendMessageAsync(_client.NetworkStream, commandMessasge);
                    Debug.WriteLine($"Отправил ping");

                    // Получаем ответ от сервера
                    var receivedMessage = await ReceiveMessageAsync(_client.NetworkStream);

                    if (receivedMessage == null)
                    {
                        Console.WriteLine($"Ошибка получения ответа от сервера");
                        work = false;
                    }
                    else if (receivedMessage.Command == "Pong")
                    {
                        Debug.WriteLine($"Прилетел pong");
                        await Task.Delay(100); // Задержка перед следующим ping
                    }
                    else if (receivedMessage.Command == "Disconnect")
                    {
                        Console.WriteLine("Получена команда Disconnect. Закрытие соединения...");
                        work = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки ответа от сервера: {ex.Message}");
            }
        }

        private async Task SendMessageAsync(NetworkStream networkStream, CommandMessasge commandMessasge)
        {
            try
            {
                // Проверяем, что сокет открыт перед отправкой данных
                if (networkStream.CanWrite)
                {
                    var message = JsonConvert.SerializeObject(commandMessasge);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(message);
                    await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    Debug.WriteLine($"Отправлено: {message}");
                }
                else
                {
                    Console.WriteLine("Сокет закрыт. Невозможно отправить сообщение.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        private async Task<CommandMessasge> ReceiveMessageAsync(NetworkStream networkStream)
        {
            var buffer = new byte[_sizeBuffer];
            int bytesRead;
            try
            {
                // Проверяем, что сокет открыт перед получением данных
                if (networkStream.CanRead)
                {
                    bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        return null;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return JsonConvert.DeserializeObject<CommandMessasge>(receivedMessage);
                }
                else
                {
                    Console.WriteLine("Сокет закрыт. Невозможно получить сообщение.");
                    return null;
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
