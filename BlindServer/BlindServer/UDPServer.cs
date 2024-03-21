using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlindServer;

public class UdpServer : IDisposable
{
    private const int bufferSize = 1024;
    
    private readonly IPEndPoint _serverEndPoint;
    private readonly Socket _server;
    private readonly byte[] _buffer = new byte[bufferSize];

    public UdpServer(IPAddress ip, int port)
    {
       
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Starting up server");
        Console.ResetColor();

        //Local host
        //IPAddress ip = IPAddress.Parse(IP);

       // _serverEndPoint = new IPEndPoint(ip, port);
        _serverEndPoint = new IPEndPoint(IPAddress.Any, port);
        _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _server.EnableBroadcast = true;


        try
        {
            //This server is using this IP and port
            _server.Bind(_serverEndPoint);
            Console.WriteLine("UDP Waiting for Data...");

            Update();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("UDP Error connecting server: " +e);
            Console.ResetColor();
            return;
        }
    }

    private async void Update()
    {
        while (true)
        {
            await RetrieveData();
        }
    }

    private async Task RetrieveData()
    {
        try
        {
            var senderInfo = await _server.ReceiveFromAsync(_buffer, SocketFlags.None, _serverEndPoint);
            
            if(!Server.UdpClients.Contains(senderInfo.RemoteEndPoint)) Console.WriteLine("added: " + senderInfo.RemoteEndPoint);
            if (senderInfo.ReceivedBytes == 0) return;
            Server.UdpClients.Add(senderInfo.RemoteEndPoint);
       
          
       
            //Still no clue what's in here, but who cares.
            SendMessageToAll(senderInfo.RemoteEndPoint, _buffer);
        }
        catch (Exception e)
        {
            Server.UdpClients.Clear();// Do bad omg
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("UDP: client disconnected suddenly: " + e);
            Console.ResetColor();
        }

        
    }

    #region Messaging

    private void  SendMessageToAll(EndPoint ignore, string message)
    {
        byte[] b = Encoding.UTF8.GetBytes(message);
        SendMessageToAll(ignore,  b);
    }

    private  async Task SendMessageToClient(string target, string message)
    {
        byte[] b = Encoding.UTF8.GetBytes(message);
        await SendMessageToClient(target, b);
    }

    private async void SendMessageToAll(EndPoint ignore, ArraySegment<byte> message)
    {
        var arr = Server.UdpClients.ToArray();
        foreach (var client in arr)
        {

            if (client.Equals(ignore)) continue;
            
                
                //But this will send over TCP... DO we seriously need another list for UDP clients.
                try
                {
                    /*
                    StringBuilder str = new();
                    foreach (byte b in message)
                    {
                        str.Append(b);
                    }
                    Console.WriteLine(str.ToString()); */
                    int val = await _server.SendToAsync(message, SocketFlags.None, client);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("UDP: Client is already disconnected: " + client);
                    Console.ResetColor();
                    Server.UdpClients.Remove(client);
                }


            //await SendMessageToClient(ignore, message);
        }
    }

    private async Task SendMessageToClient(string target, ArraySegment<byte> message)
    {
        target = target.Substring(0, target.Length - 6);
        foreach (string key in Server.TcpClients.Keys)
        {
            //Yikes we don't possibly know the ports of the other clients anymore
            if(key.Substring(0, key.Length-6).Equals(target))
                if (Server.TcpClients.TryGetValue(key, out var t))
                {
                    /*
                    StringBuilder str = new();
                    foreach (byte b in message)
                    {
                        str.Append(b);
                    }
                    Console.WriteLine(str.ToString());
                    */
                    await _server.SendToAsync(message, SocketFlags.None, t.Item2.RemoteEndPoint!);
                    return;
                }
        }

        //I think the only difference is doing SendTo instead of Send... :p
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("UDP: Client is already disconnected: " + target);
        Console.ResetColor();
            
    }
    #endregion

    #region Management

    //Try to make sure everything is getting cleaned up!
    private void CloseServer()
    {
        _server.Shutdown(SocketShutdown.Both);
        _server.Close();
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

    ~UdpServer()
    {
        Dispose(false);
    }
    #endregion
}
