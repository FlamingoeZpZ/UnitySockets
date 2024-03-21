using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Lecture
{
    public static class UDPClient
    {
        private static readonly Socket ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

  
        public static async Task<int> SendToAsync(ArraySegment<byte> bytes, SocketFlags flags,  IPEndPoint endPoint)
        { 
            return await ClientSocket.SendToAsync(bytes, flags, endPoint);
        }
    }
}
