using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Lecture;
using UnityEngine;

namespace Netcode
{
    [DefaultExecutionOrder(-1)]
    public class NetworkManager : MonoBehaviour
    {

        //Having a dictionary of lists is not ideal, garbage collection will be problematic... 

        //public static ulong ServerPlayerId;

        //Owner ID, Object ID, Object Reference
        private static readonly Dictionary<ulong, Dictionary<int, NetworkObject>> NetworkObjects = new();



        [SerializeField] private NetworkTransform playerPrefab;
        [SerializeField] private Transform spawnPosition;
        [SerializeField] private NetworkBehaviour clientPrefab;


        //TODO TCP and UDP servers that handle messages.
        public TCPServer tcp;
        public UDPServer udp;

        public static string userName { get; private set; }

        public static NetworkManager Instance { get; private set; }


        public async Task<bool> Initialize(string name, string ip)
        {
            userName = name;
            try
            {
                IPAddress adr = IPAddress.Parse(ip);
                tcp = new TCPServer(name, adr, 8888);
                await tcp.Connect();
                udp = new UDPServer(adr, 8889);
                return true;
                
            }
            catch (Exception e)
            {
                return false;
            }

        }


        //NEEDS to happy first ALWAYS

        void OnEnable()
        {
            Debug.Log("NetworkManager Starting");
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }



        #region old

        /*
            [ReliableServerRPC] // this doesn't do anything rn... just looks cool
        public async void SpawnObject()
        {
            //I need to do more research...
        }


        private bool _isConnecting;
        public void StartAsServer()
        {
            if (_isConnecting) return;
            _isConnecting = true;
            Debug.Log("Starting as Server");
            // Implement your server logic here
        }

        public void StartAsHost()
        {
            if (_isConnecting) return;
            _isConnecting = true;
            Debug.Log("Starting as Host");
            // Implement your host logic here
            server = new TCPServer();

        }

        public void StartAsClient()
        {
            if (_isConnecting) return;
            _isConnecting = true;

            // Implement your client logic here... Give to owner?
            NetworkBehaviour.NetworkInstantiate(playerPrefab, spawnPosition.position, spawnPosition.rotation, LocalPlayerId);


            Debug.Log("Starting as Client");
        }
*/

        #endregion

        //This would need to be an RPC
        public static void AddToUpdateQueue(ulong ownerID, int myId, NetworkObject newObject)
        {
            if (NetworkObjects.TryGetValue(ownerID, out var x))
            {
                x.Add(myId, newObject);
                if(ownerID == TCPServer.info.LocalUserId) BytesNeeded += newObject.GetRequiredBytes() + sizeof(int);
            }
            else
            {
                NetworkObjects.Add(ownerID,
                    new Dictionary<int, NetworkObject>
                        { { myId, newObject } }); //Create a new dictionary and add to that.
                if(ownerID == TCPServer.info.LocalUserId)BytesNeeded += newObject.GetRequiredBytes() + sizeof(ulong) + sizeof(int);
                
            }
        }


        //This would need to be an RPC
        public static void RemoveFromUpdateQueue(ulong ownerID, int toRemove)
        {
            if (NetworkObjects.TryGetValue(ownerID, out var x)) x.Remove(toRemove);
            else Debug.LogError($"Tried to remove object from owner id {ownerID}, and from object id {toRemove}");
        }


        public void UdpReceiveUpdate(ref byte[] packet, int len)
        {
            //When we receive a packet, we need to extract the ulong so we know the id to update... But, the id will be different between clients... Shit.
            //How can we ensure the objects id will remain the same?
            //When an object is created, we grant ownership (Authority), on ownership granted, we generate the object,

            //Okay, we've just received a packet, what do we do?

            //1) Who's it from?
            int index = 0;
            //Possible interleaving issues...
            ulong userID = BitConverter.ToUInt64(packet, index);//BitConverter.ToUInt64(packet,0); Why does BitConverter not work??
            index += sizeof(ulong);

            int l = 0;
            StringBuilder str = new();
            foreach (byte b in packet)
            {
                if (l == len) break;
                str.Append(b);
            }
            print("Rec --> " + str);
            if (!NetworkObjects.TryGetValue(userID, out var x))
            {
                foreach (var VARIABLE in NetworkObjects)
                {
                    print("Keys: " + VARIABLE.Key);
                }
                
                Debug.LogError("Who does this guy think they are??? (Invalid USER ID)" + userID +", " + packet);
                return;
            }

            //We're making an assumption that packets are always from one dude, and never a big merge pile
            while (index < len)
            {
                str.Clear();
                for(int i = index; i < len; ++i)
                {
                    str.Append(packet[i]);
                }
                print("inner Rec --> " + str);
                int objectID = BitConverter.ToInt32(packet, index);
                index += sizeof(int);
                if (!x.TryGetValue(objectID, out var y))
                {
                    Debug.LogError("I've never met this OBJECT in my life: (Invalid OBJECT ID)" + objectID);
                    return;
                }
                y.ReceiveUpdate(ref packet, ref index);
            }
        }

        public static int BytesNeeded;
        public async Task SendUDPUpdate()
        {
            byte[] bytes = new byte[BytesNeeded]; // This would be better if it wasn't just a guess.
            ulong user = TCPServer.info.LocalUserId;
            print(user);
            if (!NetworkObjects.TryGetValue(user, out var x))
            {
                Debug.LogError("Who does this guy think they are??? (Invalid USER ID)" + user);
                return;
            }

            Buffer.BlockCopy(BitConverter.GetBytes(user),  0, bytes, 0,  sizeof(ulong));
            int index = sizeof(ulong);
            
            //We're making an assumption that packets are always from one dude, and never a big merge pile
            foreach (var kvp in x)
            {
                //Buffer.BlockCopy(BitConverter.GetBytes(kvp.Key), 0, bytes , index, sizeof(int));
                //index += sizeof(int);
                kvp.Value.SendUpdate(ref bytes, ref index);
            }
            
            await udp.UDPSend(bytes);
        }


        //Handles TCP shenanigans
        public void ReceivedMessageFromServer(Message msg)
        {
            //Do what needs to be done.
            
            //First, let's convert to string... yikes... could serialize or just alloc 1 byte for message type...
            print($"Interpretting message from: {msg.sender} for function {msg.functionName}, with {msg.content?.Length}Bytes");
            switch (msg.functionName)
            {
                case 0:
                    Instance.Message(ref msg.content);
                    break;
                case 1:
                    Instance.InitializePlayer(msg.sender, ref msg.content);
                    break;
                case 2:
                    Instance.SpawnPlayerObject(ref msg.content);
                    break;
                case 3:
                    Instance.PlayerQuit(msg.sender, ref msg.content);
                    break;
                default:
                Debug.LogError("Could not find id: " + msg.functionName);
                    break;
            }
        }

        public void TcpSendMessageToServer(byte execType, string message)
        {
            TcpSendMessageToServer(execType, Encoding.UTF8.GetBytes(message)); //This is redundant, but by passing a byte array in every other instance saves passing around copies.
        }
        
        //I got lazy.
        readonly XmlSerializer _serializer = new(typeof(Message[]));
        public void TcpSendMessageToServer(byte execType, byte[] message)
        {
            using TextWriter writer = new StringWriter();
            _serializer.Serialize(writer, new Message[]{ new (){
                sender = TCPServer.info.LocalUserId,
                functionName = execType,
                content = message
            }
            });
            
            //Unless I'm being dumb, I think serialization may be questionable...
            string xml = writer.ToString();
            print("Sending: " + xml);
            tcp.SendMessage(Encoding.UTF8.GetBytes(xml));
        }

        private void Message(ref byte[] bytes)
        {
            TextManager.Instance.ReceiveMessageFromServer(Encoding.UTF8.GetString(bytes));
        }

        private void InitializePlayer(ulong id, ref byte[] bytes)
        {
            print("Initialize Player Event Received");
            //Spawn player object, and assign it ownership
            NetworkBehaviour.NetworkInstantiate(playerPrefab, transform.position, Quaternion.identity, id);
            
            //If its our object, and we've just joined, we need to replicate everything else that already exists...
            if (id == TCPServer.info.LocalUserId)
            {
                //But how do we know?
                //We can serialize GameObjects as illegal as it is... May be the only way to spawn an object and instance it...
                //What if we just don't care right now... And summon a box for each client... I think in the future, the server will have to store a serialized reference of each object
                //But it's not possible to serialize a GameObject on an off server because wtf is a GameObject --> It doesn't matter, cus the Unity client is the only thing that looks at it anyways.

                int idx = 0;
                while (idx < bytes.Length)
                {
                    ulong otherID = BitConverter.ToUInt64(bytes, idx);
                    idx += sizeof(ulong);
                    NetworkBehaviour.NetworkInstantiate(playerPrefab, transform.position, Quaternion.identity, otherID);
                }
                udp.UDPHeartbeat();
            }

        }

        private void SpawnPlayerObject(ref byte[] bytes)
        {
            print("Spawn Player Object Received");
            
            
            //Spawn object and assign it ownership.
        }

        private void PlayerQuit(ulong target, ref byte[] bytes)
        {
            print("Player Quit Event Received");
            Message(ref bytes);

            //Delete all objects...
            var x = NetworkObjects[target].ToArray();
            foreach (var pair in x)
            {
                Destroy(pair.Value.gameObject);
            }
            
            //If client, just kill all
            if (target == TCPServer.info.LocalUserId)
            {
                NetworkObjects.Clear();
            }
            //If not, kill client
            else
            {
                NetworkObjects.Remove(target);
            }
        }

        private void OnDestroy()
        {
            Quit();
        }

        public void Quit()
        {
            //Destroy both.
            tcp.Quit();
            udp.Close();
        }

        public int NumOwnedObject(ulong ownerId)
        {
            if (!NetworkObjects.TryGetValue(ownerId, out var x)) return 0;
            return x.Count + 1;
        }
    }
}
