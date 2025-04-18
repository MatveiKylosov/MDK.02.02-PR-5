﻿using Common;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        private readonly string _username;
        private readonly string _password;

        public TCPClient(string serverIp, int serverPort, string username, string password)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _username = username;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var clientSocket = new TcpClient(_serverIp, _serverPort);

                if (!clientSocket.Connected)
                {
                    Console.WriteLine("Не удалось подключиться к серверу.");
                    return;
                }

                var networkStream = clientSocket.GetStream();
                var receivedMessage = await ReceiveMessageAsync(networkStream);

                if (receivedMessage == null || receivedMessage.Command == "Disconnect")
                {
                    Console.WriteLine($"Не удалось подключиться к серверу. Ответ от сервера - {(receivedMessage != null ? receivedMessage.Data : "")}");
                    return;
                }

                _client = new Client(clientSocket);

                // Запуск обработки сообщений от сервера в отдельном потоке
                _ = Task.Run(() => HandleServerResponseAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task HandleServerResponseAsync()
        {
            bool isRunning = true;
            string msg = "";
            try
            {
                CommandMessasge commandMessasge;
                commandMessasge = new CommandMessasge() { Command = "Auth", Data = $"{_username}&{_password}" };
                await SendMessageAsync(_client.NetworkStream, commandMessasge);

                var receivedMessage = await ReceiveMessageAsync(_client.NetworkStream);
                if (receivedMessage == null)
                {
                    isRunning = false;
                }
                else if(receivedMessage.Command == "Disconnect")
                {
                    isRunning = false;
                    msg = "Отключён сервером";
                }
                else if(receivedMessage.Command == "Client")
                {

                    var client = JsonConvert.DeserializeObject<Common.Client>(receivedMessage.Data);
                    _client.Token = client.Token;
                    _client.ConnectionTime = client.ConnectionTime;
                    _client.Work = client.Work;
                    isRunning = true;
                    Console.WriteLine("Подключение успешно установлено.");
                }

                while (isRunning)
                {
                    if (!_client.Work)
                    {
                        commandMessasge = new CommandMessasge() { Command = "Disconnect", Data = "" };
                        await SendMessageAsync(_client.NetworkStream, commandMessasge);
                        _client.Work = false;
                        break;
                    }

                    commandMessasge = new CommandMessasge() { Command = "Ping", Data = "" };
                    await SendMessageAsync(_client.NetworkStream, commandMessasge);

                    receivedMessage = await ReceiveMessageAsync(_client.NetworkStream);

                    if (receivedMessage == null)
                    {
                        Console.WriteLine($"Ошибка получения ответа от сервера");
                        break;
                    }
                    else if (receivedMessage.Command == "Pong")
                    {
                        await Task.Delay(100);
                    }
                    else if (receivedMessage.Command == "Disconnect")
                    {
                        _client.Work = false;
                        Console.WriteLine("Получена команда Disconnect. Закрытие соединения...");
                        msg = "Отключён сервером";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки ответа от сервера: {ex.Message}");
            }
            finally{
                Console.WriteLine(msg);
                _client.Work = false;
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
                    Debug.WriteLine($"Отправлено: {message}", "Client");
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
                    Debug.WriteLine($"Получено: {JsonConvert.DeserializeObject<CommandMessasge>(receivedMessage)}", "Client");
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

        public void PrintStatus()
        {
            if (_client == null || !_client.Work )
            {
                Console.WriteLine("Клиент не подключён.");
                return;
            }

            lock (_client)
            {
                Console.WriteLine($"Токен: [{_client.Token}], время подключения: {_client.ConnectionTime.ToString("HH:mm:ss dd.MM")}, подключён уже {(long)Math.Floor((DateTime.UtcNow - _client.ConnectionTime).TotalSeconds)} секунд.");
            }
        }

        public void CloseConnection()
        {
            if (_client == null)
            {
                Debug.WriteLine("Клиент не подключён.");
                return;
            }

            lock (_client)
            {
                _client.Work = false;
            }
        }
    }
}
