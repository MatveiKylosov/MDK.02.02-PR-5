using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private List<IPAddress> _blacklistClients = new List<IPAddress>();

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
                        CommandMessasge commandMessasge = new CommandMessasge();
                        var tcpClient = await listener.AcceptTcpClientAsync();

                        // Проверка на черный список
                        var clientIp = (IPAddress)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                        bool disconnect = false;

                        lock (_connectedClients)
                        {
                            disconnect = _connectedClients.Count >= _maxClients;
                            commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "No available seats" };
                        }

                        lock (_blacklistClients)
                        {
                            disconnect = disconnect || _blacklistClients.Any(x => x.Equals(clientIp));
                            commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "Blocked" };
                        }

                        if (disconnect)
                        {
                            await SendMessageAsync(tcpClient.Client, commandMessasge);
                            tcpClient.Close();
                            continue;
                        }
                        commandMessasge = new CommandMessasge() { Command = "Wait", Data = "Wait data" };
                        await SendMessageAsync(tcpClient.Client, commandMessasge);
                        var client = new Client(tcpClient.Client);

                        lock (_connectedClients)
                        {
                            _connectedClients.Add(client);
                        }

                        // Асинхронная обработка клиента
                        _ = HandleClientAsync(client);
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
            bool faileAduthorization = false;
            try
            {
                bool isRunning = true;
                CommandMessasge commandMessasge;

                var recvLogData = await ReceiveMessageAsync(client.Socket);
                isRunning = recvLogData != null;

                if (isRunning)
                {
                    var logData = recvLogData.Data.Split('&');
                    if (recvLogData.Command != "Auth" || logData.Length != 2 || !await UserInDatabaseAsync(logData[0], logData[1]))
                    {
                        commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "Wrong username or password" };
                        await SendMessageAsync(client.Socket, commandMessasge);
                        isRunning = false;
                        faileAduthorization = true;
                    }
                }

                if (isRunning)
                {
                    var sendClient = new Common.Client
                    {
                        Token = Guid.NewGuid().ToString(),
                        ConnectionTime = DateTime.UtcNow,
                        Work = true
                    };

                    commandMessasge = new CommandMessasge() { Command = "Client", Data = JsonConvert.SerializeObject(sendClient) };
                    await SendMessageAsync(client.Socket, commandMessasge);

                    client.Token = sendClient.Token;
                    client.ConnectionTime = sendClient.ConnectionTime;
                    client.Work = sendClient.Work;

                    Console.WriteLine($"Новый клиент: {sendClient.Token}");
                }

                while (isRunning)
                {
                    var receivedMessage = await ReceiveMessageAsync(client.Socket);

                    if (receivedMessage == null)
                    {
                        break;
                    }

                    if(receivedMessage.Command == "Disconnect")
                    {
                        break;
                    }

                    if (IsTokenExpired(client.ConnectionTime) || !client.Work)
                    {
                        commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "Token expired" };
                        await SendMessageAsync(client.Socket, commandMessasge);
                        break;
                    }

                    commandMessasge = new CommandMessasge() { Command = "Pong", Data = "" };
                    await SendMessageAsync(client.Socket, commandMessasge);

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки клиента [{client.Token}]: {ex.Message}");
            }
            finally
            {
                IPEndPoint remoteIpEndPoint = client.Socket.RemoteEndPoint as IPEndPoint;
                lock (_connectedClients)
                {
                    _connectedClients.Remove(client); // Удаляем клиента из списка
                }
                client.Socket.Close();


                Console.WriteLine($"Клиент [{(!faileAduthorization ? client.Token : remoteIpEndPoint.Address.ToString())}] отключен. {(faileAduthorization ? "Причина: неудачная авторизация." : "")}");
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
                    Debug.WriteLine($"Получено {message}", "Server");
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
                    Debug.WriteLine($"Получено {JsonConvert.DeserializeObject<CommandMessasge>(receivedMessage)}", "Server");
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
                        Console.WriteLine($"Токен: {client.Token}, подключен: {client.ConnectionTime}, подключен уже: {(long)Math.Floor((DateTime.UtcNow - client.ConnectionTime).TotalSeconds)} секунд.");
                    }
                }
                else
                {
                    Console.WriteLine("Нет подключенных клиентов.");
                }
            }
        }

        public void Disconnect(string Token = "")
        {
            lock (_connectedClients) 
            {
                var client = _connectedClients.FirstOrDefault(x => x.Token == Token);
                if (client == null)
                {
                    Console.WriteLine($"Клиента с токеном [{Token}] не существует");
                    return;
                }
                client.Work = false;
            }
        }

        public void DisconnectAll()
        {
            lock (_connectedClients)
            {
                foreach (var client in _connectedClients)
                {
                    Disconnect(client.Token);
                }
            }
        }

        public void BlockClient(string Token)
        {
            lock (_connectedClients)
            {
                lock (_blacklistClients)
                {
                    var client = _connectedClients.FirstOrDefault(x => x.Token == Token);
                    if (client == null)
                    {
                        Console.WriteLine($"Клиента с токеном [{Token}] не существует.");
                        return;
                    }

                    var ip = (IPAddress)((IPEndPoint)client.Socket.RemoteEndPoint).Address;
                    if(_blacklistClients.Any(e => e == ip))
                    {
                        Console.WriteLine($"Данный пользователь уже заблокирован.");
                        return;
                    }

                    _blacklistClients.Add(ip);

                    client.Work = false;
                }
            }
        }

        private bool IsTokenExpired(DateTime connectionTime)
        {
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan timeElapsed = currentTime - connectionTime;

            return (ulong)timeElapsed.TotalSeconds > _tokenLifetime;
        }

        private async Task<bool> UserInDatabaseAsync(string username, string passwordUser)
        {
            var connectionString = "Server=192.168.0.111;Database=PR5;User=root;Password=dawda6358;";

            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM users WHERE username = @usernameString and passwordUser = @passwordUserString";
                using (var command = new MySqlConnector.MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@usernameString", username);
                    command.Parameters.AddWithValue("@passwordUserString", passwordUser);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

    }
}
