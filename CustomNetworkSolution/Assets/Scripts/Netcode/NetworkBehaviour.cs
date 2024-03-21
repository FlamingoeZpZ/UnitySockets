using System;
using Lecture;
using UnityEngine;

namespace Netcode
{
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        //protected class ServerRpcAttribute : Attribute { }

        private NetworkObject networkObject;

        public bool IsOwner => networkObject.IsOwner;
        //public ulong ObjectId => networkObject.Id;

        public int RequiredUpdateLength { get; protected set; }
        //protected byte[] UploadBuffer;
        //protected byte[] PreviousBuffer;
        //protected byte[] Buffer;
    
        protected abstract void InitializeBuffers();
        //We'd generate code for net vars usually, but in this case... Whatever...
        //We be out here coding like it's C++ lmao
        protected abstract void ReceiveUpdate(ref byte[] bytes,int idx);
        protected abstract void SendUpdate(ref byte[] bytes);
    
        protected virtual void OnEnable()
        {
            networkObject = GetComponent<NetworkObject>();
            networkObject.OwnershipChanged += OnOwnershipChanged; 
            InitializeBuffers();
            OnOwnershipChanged(IsOwner);
            //When a function is called by a child, we know
            //FindSubClassFuncs();
            print("Net Behaviour Enabled: " + GetType());
        }

        private void OnDisable()
        {
            networkObject.OwnershipChanged -= OnOwnershipChanged; 
        }

        public void OwnerUpdate(ref byte[] bytes, ref int index)
        {
            //When working with big arrays... We increase the probability of Garbage Collection if we're not careful.
            byte[] update = new byte[RequiredUpdateLength];
            SendUpdate(ref update);
            //So by doing a constrained copy, the size of "bytes" can remain the same, meaning we always will only do one allocation.
            Buffer.BlockCopy(update, 0, bytes, index, RequiredUpdateLength);
            index += RequiredUpdateLength;
        }

        public void ClientUpdate(ref byte[] bytes, ref int index)
        {
            print($"Updating object {RequiredUpdateLength}+{index}/{bytes.Length}");
            ReceiveUpdate(ref bytes, index);
            index += RequiredUpdateLength;
        }

        protected virtual void OnOwnershipChanged(bool state) { }

        //Custom instansiation should go in here, so it's easily accessible.
     //   public static T NetworkInstantiate<T>(T toSpawn, Vector3 initialLocation, Quaternion initialRotation) where T : NetworkBehaviour
     //   {
     //       return NetworkInstantiate(toSpawn, initialLocation, initialRotation, NetworkManager.ServerPlayerId);
     //   }
        //Slightly scuffed to have this here, but I want it to work like Mono where any class can just Instansiate()
        public static T NetworkInstantiate<T>(T toSpawn, Vector3 initialLocation, Quaternion initialRotation, ulong ownerID) where T : NetworkBehaviour
        {
            //If we have authority
        
            //Execute on all clients
            //Spawn the object
            
            T output = Instantiate(toSpawn, initialLocation, initialRotation);

            NetworkObject networkObject = output.GetComponent<NetworkObject>();
        
            //Tell the network that this object exists.
            //NetworkManager.AddToUpdateQueue(ownerID, networkObject.Id , networkObject);
        
            //Assign ID.
            networkObject.AssignId(ownerID, ownerID == TCPServer.info.LocalUserId);
        
            //Give back the object to whoever needs it.
            return output;
        }

        //Rip that idea, I don't have time
        /*
    public void FindSubClassFuncs()
    {
        //https://learn.microsoft.com/en-us/dotnet/api/system.type.getmethods?view=net-8.0
        //https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/
        //https://stackoverflow.com/questions/64926889/generate-code-for-classes-with-an-attribute
        //Declared only removes overriden functions which shrinks the list by a lot. I think it's worthwhile to have...
        var methods = GetType().GetMethods(BindingFlags.DeclaredOnly |  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        print($"Methods in {GetType().Name}: marked as ServerRpc");
        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<ServerRpcAttribute>() == null) continue;
            print($"{method.Name}");
        }

        /*
         * Great so we have the functions... Question is, how do we make the functions do things...
         * We can use the AOP pattern to inject reflected functionality into these functions, and send messages?
         * But then, we need an interface, and how do we describe an interface when we don't even know what the function is?
         * Do I abuse the IL and inject code? That's like a level 10 sin?
         *
         *
         * Source Generator?
         * Source gen has potential, but how do I even inject this into Unity?
         * Also, how exactly do I do this... When the method is called, if it's marked as a ServerRPC then it should :
         * 1) If has authority no authority
         * 1.a) tell server to execute this function with my information and include userID? and return;
         * 2) Else, Execute code.
         * If ClientRPC
         * 1) If has no authority return
         * 2) Tell all clients to execute this function with my information
         *
         * So it's a hybrid reflection roslyn gen sol?
         */
        //}
        /* protected void Success(object [] args)
    {
        print("Yippie!");
    }*/


   


    }
}