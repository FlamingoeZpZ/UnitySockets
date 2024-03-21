using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Netcode;
using UnityEngine;

namespace Lecture
{
    [Serializable]
    public struct Message
    {
        public ulong sender;
        public byte functionName;
        public byte[] content;
    }
    
    [Serializable]
    public struct ServerInfo
    {
        public int MilliDelay;
        public ulong LocalUserId;
    }

    public class UDPServer  : IDisposable
    {
        private readonly Socket _server;
        private const int BufferSize = 1024;
        private byte[] _receiveBuffer = new byte[BufferSize];
        private readonly IPEndPoint _serverEndpoint;
        private bool isRunning;
        public UDPServer(IPAddress ip, int port)
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverEndpoint = new IPEndPoint(ip, port);
            isRunning = true;
        }

        ~UDPServer()
        {
            Dispose(false);
        }

        public void Close()
        {
            isRunning = false;
            if (_server == null) return;
            Debug.Log("Quitting on UDP");
            try
            {
                _server.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _server.Close();
            }
        }

        public async Task UDPSend(byte[] packet)
        {
            //Debug.Log("Sending UDP packet: " + packet.Length +"B");
            await _server.SendToAsync(packet, SocketFlags.None, _serverEndpoint);
        }

        private async void UDPReceive()
        {
            while (isRunning)
            {
                //Handle Heartbeat
                //Using ReceiveMessageFrom just crashes it...? It's not interally implemented or soemthing in this version of c#?
              
                var n = await _server.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, _serverEndpoint);
                Debug.Log("Update Received");
                NetworkManager.Instance.UdpReceiveUpdate(ref _receiveBuffer, n.ReceivedBytes);
                /*
                 string xml = Encoding.UTF8.GetString(_receiveBuffer,0,n.ReceivedBytes);
                Debug.Log(xml);
                using var reader = new StringReader(xml);
                var serializer = new XmlSerializer(typeof(Message));
                NetworkManager.Instance.ReceivedMessageFromServer((Message)serializer.Deserialize(reader)!);
                */
            }
        }
        
        public async void UDPHeartbeat()
        {
            UDPReceive();
            while (isRunning)
            {
                await NetworkManager.Instance.SendUDPUpdate();
                await Task.Delay(TCPServer.info.MilliDelay);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            Close();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _server?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class TCPServer
    {
        public static ServerInfo info;
       
        private const int MaxTries = 10;
        
        
        //Received from server.
        private const int BufferSize = 1024;

        private readonly string _name;
        private readonly Socket _client;
        private readonly IPEndPoint _endPoint;

        private byte[] _receiveBuffer = new byte[BufferSize];
        private bool _isRunning;
        
        
        public TCPServer(string clientName, IPAddress ip, int port)
        {
            _name = clientName;
            
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _endPoint = new IPEndPoint(ip, port);
        }

        public async Task<bool> Connect()
        {
            int attempts = 0;
            while (!_client.Connected)
            {
                await Task.Delay(info.MilliDelay);
                await TryConnect();
                if (++attempts >= MaxTries) return false;
            }
            _isRunning = true;
            BeginUpdate();
            return true;
        }

        private async Task TryConnect()
        {
            try
            {
                _client.Connect(_endPoint);
                Debug.Log("My name is: " + _name);
                await _client.SendAsync(Encoding.ASCII.GetBytes(_name), SocketFlags.None);
                //When a client connects, we must inform them of a few things...
                _receiveBuffer = new byte[BufferSize];
                int m =await _client.ReceiveAsync(_receiveBuffer, SocketFlags.None);
                //First we need to parse our ID
                string xml = Encoding.UTF8.GetString(_receiveBuffer,0,m);
                Debug.Log(xml);
                ServerInfo serverInfo;
                using (var reader = new StringReader(xml))
                {
                    var serializer = new XmlSerializer(typeof(ServerInfo));
                    info = (ServerInfo)serializer.Deserialize(reader)!;
                }
                Debug.Log($"Server Info: ID: {info.LocalUserId}, Millis {info.MilliDelay}");
            }
            catch (Exception e)
            { 
               Debug.LogError("Failed to connect: " + e);
            }
        }

        public async void BeginUpdate()
        {
            //We're always running.
            while (_isRunning)
            {
                await HeartBeat();
                await Task.Delay(info.MilliDelay);
            }
        }

        private async Task HeartBeat()
        {
            int receive = 0;
            Debug.Log("Heartbeat");
            try
            {
                _receiveBuffer = new byte[BufferSize];
                receive = await _client.ReceiveAsync(_receiveBuffer, SocketFlags.None);
            }
            catch (SocketException e)
            {
                Debug.LogError("Failed to read: " + e);
                Quit();
            }
            if (receive == 0) return;
            string xml = Encoding.UTF8.GetString(_receiveBuffer,0,receive);
            Debug.Log(xml);
            using var reader = new StringReader(xml);
            var serializer = new XmlSerializer(typeof(Message[]));
            Message[] msgs = (Message[])serializer.Deserialize(reader)!;
            foreach (var msg in msgs) NetworkManager.Instance.ReceivedMessageFromServer(msg);
        }

        public void Quit()
        {
            if (_client == null) return;
            Debug.Log("Quitting on TCP");
            _isRunning = false;
            try
            {
                _client.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                // ignored
                Debug.LogError("Error while quitting: " + e);
            }
            finally
            {
                _client.Close();
            }
        }

        /*
        public async void SendMessage(byte[] bytes)
        {
            try
            {
                if (bytes.Length >= BufferSize) throw new Exception("Message too long");
                await _client.SendAsync(bytes, SocketFlags.None);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send message: " + e);
            }
        } */
        
        public async void SendMessage(byte[] bytes)
        {
            try
            {
                if (bytes.Length >= BufferSize) throw new Exception("Message too long");
                await _client.SendAsync(bytes, SocketFlags.None);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send message: " + e);
            }
        }

        private void Dispose(bool disposing)
        {
            Quit();
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public void Dispose()
        {
            Quit();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TCPServer()
        {
            Dispose(false);
        }

    }
}
