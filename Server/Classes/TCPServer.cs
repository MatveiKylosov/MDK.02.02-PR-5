using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class TCPServer
    {
        private readonly IPAddress _ip;
        private readonly int _port;
        private readonly int _maxClients;
        private readonly ulong _tokenLifetime;
        private const ulong _sizeBuffer = 2048;

        private readonly List<Client> _connectedClients = new List<Client>();


        public TCPServer(IPAddress ip, int port, int maxClients, ulong tokenLifetime)
        {
            _ip = ip;
            _port = port;
            _maxClients = maxClients;
            _tokenLifetime = tokenLifetime;
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(_ip, _port);
            listener.Start();
            Console.WriteLine($"Сервер запущен на {_ip}:{_port}...");

            try
            {
                while (true)
                {
                    if (listener.Pending())
                    {
                        var tcpClient = await listener.AcceptTcpClientAsync();
                        CommandMessasge commandMessasge;
                        if (_connectedClients.Count == _maxClients)
                        {
                            commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "No available seats" };
                            await SendMessageAsync(tcpClient.Client, commandMessasge);
                            continue;
                        }

                        var sendClient = new Common.Client
                        {

                            Token = Guid.NewGuid().ToString(),
                            ConnectionTime = DateTime.UtcNow,
                            Work = true
                        };
                        
                        
                        commandMessasge = new CommandMessasge() { Command = "Client", Data = JsonConvert.SerializeObject((sendClient)) };
                        await SendMessageAsync(tcpClient.Client, commandMessasge);

                        Client client = new Client(tcpClient.Client, sendClient);
                        client.Socket = tcpClient.Client;

                        lock (_connectedClients)
                        {
                            _connectedClients.Add(client); // Добавляем клиента в список
                        }

                        _ = HandleClientAsync(client); // Асинхронная обработка клиента
                    }
                    else
                    {
                        await Task.Delay(100); // Уменьшаем нагрузку на цикл
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сервера: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(Client client)
        {
            try
            {
                while (client.Work)
                {
                    var receivedMessage = await ReceiveMessageAsync(client.Socket);

                    if (receivedMessage == null)
                        break;

                    CommandMessasge commandMessasge;

                    if (receivedMessage.Command == "Ping")
                        Debug.WriteLine($"Прилетел ping");

                    if (IsTokenExpired(client.ConnectionTime))
                    {
                        commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "Token expired" };
                        await SendMessageAsync(client.Socket, commandMessasge);
                        break;
                    }

                    commandMessasge = new CommandMessasge() { Command = "Pong", Data = "" };
                    await SendMessageAsync(client.Socket, commandMessasge);
                    Debug.WriteLine($"Отправил pong");

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки клиента [{client.Token}]: {ex.Message}");
            }
            finally
            {
                lock (_connectedClients)
                {
                    _connectedClients.Remove(client); // Удаляем клиента из списка
                }
                client.Socket.Close();
                Console.WriteLine($"Клиент [{client.Token}] отключен.");
            }
        }

        private async Task SendMessageAsync(Socket socket, CommandMessasge commandMessasge)
        {
            try
            {
                using (var stream = new NetworkStream(socket))
                {
                    var message = JsonConvert.SerializeObject(commandMessasge);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    Debug.WriteLine($"Отправлено: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        private async Task<CommandMessasge> ReceiveMessageAsync(Socket socket)
        {
            var buffer = new byte[_sizeBuffer];
            int bytesRead;
            try
            {
                using (var stream = new NetworkStream(socket))
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

        public void ListConnectedClients()
        {
            lock (_connectedClients)
            {
                if (_connectedClients.Any())
                {
                    Console.WriteLine("Подключенные клиенты:");
                    foreach (var client in _connectedClients)
                    {
                        Console.WriteLine($"Токен: {client.Token}, подключен: {client.ConnectionTime}, подключен уже: {(DateTime.UtcNow - client.ConnectionTime).TotalSeconds}.");
                    }
                }
                else
                {
                    Console.WriteLine("Нет подключенных клиентов.");
                }
            }
        }

        private bool IsTokenExpired(DateTime connectionTime)
        {
            // Получаем текущее время
            DateTime currentTime = DateTime.UtcNow;

            // Вычисляем разницу во времени в секундах
            TimeSpan timeElapsed = currentTime - connectionTime;

            // Проверяем, прошло ли больше секунд, чем указано в tokenLifetime
            return (ulong)timeElapsed.TotalSeconds > _tokenLifetime;
        }
    }
}
