using System;
using System.Linq;
using UnityEngine;

namespace Netcode
{
    [Serializable]
    public class NetworkObject : MonoBehaviour
    {
        //We need an ID so we don't replicate an object too many times
        //When the object is built, we need to assign some id. It really doesn't matter what our ID is.
        public int MyId { get; private set; }
        public ulong OwnerId { get; private set; }
        private byte[] idAsBytes = new byte[sizeof(int)];
    

        //We need to know if we own this object
        public bool IsOwner { get; private set; }
        public Action<bool> OwnershipChanged { get; set; }

        //We need to know what systems this object owns to replicate
        private NetworkBehaviour[] networkBehaviours;

        public int GetRequiredBytes()
        {
            return networkBehaviours.Sum(x => x.RequiredUpdateLength);
        }


        private void OnEnable()
        {
            //Remember all of our targets, regenerate them if we're being stupid.
            networkBehaviours = GetComponentsInChildren<NetworkBehaviour>();
        
            if(OwnerId != 0) // If owner id is 0, we belong to server, and it don't make sense rn
            //Enable Updates
            NetworkManager.AddToUpdateQueue(OwnerId, MyId, this);
            // OwnershipChanged = state => _response =  
        }

        private void OnDisable()
        {
            //Disable Updates
            NetworkManager.RemoveFromUpdateQueue(OwnerId, MyId);
            OwnershipChanged = null;
        }

        
    
        //When we receive an update from the server
        public void ReceiveUpdate(ref byte[] bytes, ref int index)
        {
            //We need a way to determine how many bytes each object is responsible for, and moving through each...
            //The first few bytes will be our address, but the network manager will process that.
            //Buffer.BlockCopy(idAsBytes, 0, bytes, index, sizeof(int));
            //index += sizeof(int);
            
            foreach (NetworkBehaviour networkBehaviour in networkBehaviours)
            {
                networkBehaviour.ClientUpdate(ref bytes, ref index); // Can we send a byte chunk instead?
            }
        }
        //When we send an update to the server.
        public void SendUpdate(ref byte[] bytes, ref int index)
        {
            
            Buffer.BlockCopy(idAsBytes, 0, bytes, index, sizeof(int));
            index += sizeof(int);
            
            //We need to iterate through each of our components, and send one big message.
            foreach (NetworkBehaviour networkBehaviour in networkBehaviours)
            {
                networkBehaviour.OwnerUpdate(ref bytes, ref index); // Can we send a byte chunk instead?
            }
        }

        //Is this risky being public?
        public void AssignId(ulong ownerId, bool isOwner)
        {
            OwnerId = ownerId;
            MyId = NetworkManager.Instance.NumOwnedObject(ownerId) + 1; 
            idAsBytes = BitConverter.GetBytes(MyId);
            IsOwner = isOwner;
            
            print("New Owner Assigned");
            
            //Enable Updates
            NetworkManager.AddToUpdateQueue(OwnerId, MyId, this);
            OwnershipChanged?.Invoke(isOwner);
            
            
        }
    }
}


//A promising possibility to use an abstract factory, but I want to stay to what Unity did.
/*
public interface INetworkComponent
{
    public byte[] UpdateData();
    public void SendData(byte[] dataOut);
}
*/