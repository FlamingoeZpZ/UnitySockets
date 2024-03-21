using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace BlindServer
{
    public class TcpServer : IDisposable
    {
        private static ulong _counter;
        [Serializable]
        public struct ServerInfo
        {
            public int MilliDelay;
            public ulong LocalUserId;
        }

        private const int BufferSize = 1024;
        public const int MaxConnections = 8;
        public const int ServerMilliDelay = 20;
        
        [Serializable]
        public struct Message
        {
            public ulong sender;
            public byte functionName;
            public byte[] content;
        }

        XmlSerializer _serializer = new(typeof(Message[]));
        //private readonly IPHostEntry _hostInfo = Dns.GetHostEntry(Dns.GetHostName());
        private readonly IPEndPoint _serverEndPoint;

        private readonly Socket _server;
        

        //It's possible we accidentally overwrite the same buffer we're reading...
        //We should use a seperate buffer for input than for output... or have a flush interval.
        byte[] _receiveBuffer = new byte[BufferSize]; // Surely there's a better way to clear

        public TcpServer(IPAddress ip, int port)
        {
            _serverEndPoint = new IPEndPoint(ip, port);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Starting up server");
            Console.ResetColor();

            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = BufferSize,
                SendBufferSize = BufferSize
            };
            try
            {
                _server.Bind(_serverEndPoint);
                _server.Listen(MaxConnections);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed during server initializing: " + e);
                Console.ResetColor();
                return;
            }

            HandleConnections();
            HeartBeat();
        }

        private async void HandleConnections()
        {
            string name;
            Socket user;
            try
            {
                Console.WriteLine(_serverEndPoint);

                Console.WriteLine("Pending Connection...");
                user = await _server.AcceptAsync();
                Console.WriteLine("Connection Received!");
                int t = await user.ReceiveAsync(_receiveBuffer, SocketFlags.None);
                
                name = Encoding.UTF8.GetString(_receiveBuffer,0, t);

                Console.WriteLine("Successfully registered: " + name);

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed at HandleConnections: " + e);
                Console.ResetColor();
                return;
            }

            int n = Server.TcpClients.Count;
            //Send server information.

            string id = user.RemoteEndPoint!.ToString()!;

            if (!Server.TcpClients.TryAdd(name, new Tuple<ulong, Socket>(++ _counter, user)))
            {
                await _server.DisconnectAsync(true);
                return;
            }
            

            
            //Send all the server information
            
            //TODO" Xml serialize the server info and send it to the client, who must have a copy of the same struct. Error handling is definitely required.

            await using (TextWriter writer = new StringWriter())
            {
                ServerInfo sr = new ServerInfo()
                {
                    LocalUserId = _counter,
                    MilliDelay = ServerMilliDelay
                };

                XmlSerializer ser = new XmlSerializer(typeof(ServerInfo));
                ser.Serialize(writer, sr);
                //string? bts = writer.ToString();
                //serializer.Serialize(writer, new Message(0, bts)); //XML serialization must go first... That hinders like everything...
                await SendMessageToClient(name, writer.ToString());
            }

            await Task.Delay(100); // The packets are being sent together for some reason... Which is causing a read error.
            
            await using (TextWriter writer = new StringWriter())
            {
                _serializer.Serialize(writer, new Message[]{ new(){ 
                    sender = _counter,
                   functionName = 0, 
                   content = Encoding.UTF8.GetBytes($"{name} has connected to the server. There are {n+1} clients connected.")
                },
                new(){ 
                    sender = _counter,
                    functionName = 1, 
                    content = Server.GetAllIds(_counter).SelectMany(BitConverter.GetBytes).ToArray()//Send over all currently connected users.
                }
                });
                //Update all client player counts.
                await SendMessageToAll(ulong.MaxValue, writer.ToString());
            }


            HandleConnections(); // Repeat for all eternity...
            Console.WriteLine("Clients have been told about: " + name +", ID: " + id);

            OnServerListUpdated();

        }

        private void OnServerListUpdated()
        {
            Console.WriteLine("---------------Client list updated-------------------");
            foreach (var pair in Server.TcpClients)
            {
                Console.WriteLine(pair.Key + ": " + pair.Value.Item2.RemoteEndPoint);
            }
            Console.WriteLine("-----------------------------------------------------");
        }


        //Check if all clients are in the lobby.
        //Check if any new messages have been received.
        private async void HeartBeat()
        {
            while (true)
            {
                await Task.Delay(ServerMilliDelay);
                var list = Server.TcpClients.ToArray();

                foreach (var client in list)
                {
                    //Handle bad disconnections.
                    if (client.Value.Item2.Poll(1000, SelectMode.SelectRead) && client.Value.Item2.Available == 0) //If there are no bytes pending.
                    {
                        Console.WriteLine("Client was disconnected, " + client.Key);
                        Server.TcpClients.Remove(client.Key);
                        if (Server.TcpClients.Count > 0)
                        {
                            await using (TextWriter writer = new StringWriter())
                            {
                                _serializer.Serialize(writer, new Message[]{ new (){
                                    sender =  client.Value.Item1, // We need to delete the object
                                    functionName = 3, 
                                    content = Encoding.UTF8.GetBytes($"{client.Key} has disconnected from the server. There are {Server.TcpClients.Count} clients connected.")
                                }});
                                //Update all client player counts.
                                await SendMessageToAll(ulong.MaxValue, writer.ToString());
                            }
                        }

                        OnServerListUpdated();
                        continue;
                    }

                    //
                    if (client.Value.Item2.Available == 0) continue;
                    int len = await client.Value.Item2.ReceiveAsync(_receiveBuffer, SocketFlags.None);
                    if (len == 0) continue;
                    
                    //We actually have zero clue what is in here... and we don't care. We're just here to relay.
                    await SendMessageToAll(client.Value.Item1, _receiveBuffer);
                }
            }
        }

        #region Messaging

        private async Task SendMessageToAll(ulong ignore, string message)
        {
            byte[] b = Encoding.UTF8.GetBytes(message);
            await SendMessageToAll(ignore,  b);
        }

        private async Task SendMessageToClient(string target, string message)
        {
            byte[] b = Encoding.UTF8.GetBytes(message);
            await SendMessageToClient(target, b);
        }

        private async Task SendMessageToAll(ulong ignore, byte[] message)
        {
            foreach (var client in Server.TcpClients)
            {
                if(client.Value.Item1.Equals(ignore)) continue;
                await SendMessageToClient(client.Key, message);
            }
        }

        private async Task SendMessageToClient(string target, byte[] message)
        {
            Console.WriteLine("Sending Message to: " + target + ", " + Encoding.UTF8.GetString(message));
            if(Server.TcpClients.TryGetValue(target, out var s)) 
                await s.Item2.SendAsync(message, SocketFlags.None);
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TCP: Client is already disconnected: " + target );
                Console.ResetColor();
            }
            _receiveBuffer = new byte[BufferSize]; // Surely there's a better way to clear
        }
        #endregion

        #region Management

        

     
        //Try to make sure everything is getting cleaned up!
        private void CloseServer()
        {
            _server.Shutdown(SocketShutdown.Both);
            _server.Close();
            Console.WriteLine("Server shutting down");
        }

        private void Dispose(bool disposing)
        {
            CloseServer();
            if (disposing)
            {
                _server?.Dispose();
            }
        }

        public void Dispose()
        {
            CloseServer();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TcpServer()
        {
            Dispose(false);
        }
        #endregion
    }
}