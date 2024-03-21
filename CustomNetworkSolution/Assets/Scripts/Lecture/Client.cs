using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Lecture
{
    [DefaultExecutionOrder(-1000)]
    public class Client : MonoBehaviour
    {
        private Socket _clientSocket;

        public static Client client { get; private set; }


        //NEEDS to happy first ALWAYS
        void OnEnable()
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Debug.Log("Client Starting");
            if (client && client != this)
            {
                Destroy(gameObject);
                return;
            }
            client = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Client Ready");
        }

        public async Task<int> SendToAsync(ArraySegment<byte> bytes, SocketFlags flags,  IPEndPoint endPoint)
        { 
            return await _clientSocket.SendToAsync(bytes, flags, endPoint);
        }
    
    

        private void OnDestroy()
        {
            if (_clientSocket == null) return;
            try
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _clientSocket.Close();
            }

            Debug.Log("Client Stopping");
        }
    }
}
