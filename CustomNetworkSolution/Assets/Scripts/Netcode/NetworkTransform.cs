//#define UseSerialized // If this is enabled, then it will use the serialized version. (Hint: Serialized version is a lot worse.)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Netcode
{
    public class NetworkTransform : NetworkBehaviour
    {
#if UseSerialized
        [Serializable]
        public struct TransformInfo
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public TransformInfo(Transform t)
            {
                //This is worse than just doing bytes btw.
                Position = t.position;
                Rotation = t.rotation;
                Scale = t.localScale;
            }
        }
#else
    //These are no longer useful, because the TransformInfo is sending all information... And going around that can lead to silly issues.
    [Header("Replication Rules")]
    [SerializeField] private bool position = true;
    [SerializeField] private bool rotation = true;
    [SerializeField] private bool scale = false;
    
#endif
        //If we do not own the object, then we should be replicating it's position and sending it...
        //First of all, how do we determine who owns what object....
        //When a client connects to the game, we need to spawn an object.

        protected override void OnOwnershipChanged(bool state)
        {
            base.OnOwnershipChanged(state);
#if UseSerialized
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = !state;
#else
                if (rotation && TryGetComponent(out Rigidbody rb)) rb.isKinematic = !state;
#endif
        }

        protected override void InitializeBuffers()
        {
#if UseSerialized
           // RequiredUpdateLength = Marshal.SizeOf<TransformInfo>();
           XmlSerializer serializer = new XmlSerializer(typeof(TransformInfo));
           StringBuilder stringBuilder = new StringBuilder();
           using (XmlWriter writer = XmlWriter.Create(stringBuilder))
           {
               serializer.Serialize(writer, new TransformInfo());
           }
            RequiredUpdateLength = Encoding.UTF8.GetBytes(stringBuilder.ToString()).Length;
           
#else
        int num = 0;
        if (position) num += 3; 
        if (rotation) num += 4;
        if (scale) num += 3;
        RequiredUpdateLength = num * sizeof(float); // number of bytes, 4 * 10
#endif
            print($"TransformInfo is {RequiredUpdateLength} bytes long.");
        }

        protected override void ReceiveUpdate(ref byte[] bytes, int start)
        {
            Transform trs = transform; // Only get the C++ ref once.
            //Retrieve data.
            //var x = await UDPServer.ReceiveFromAsync(_buffer, SocketFlags.None, StaticUtilities.ClientAnyEndPoint);
#if UseSerialized
            TransformInfo tr;
            using (var stringReader = new StringReader(Encoding.UTF8.GetString(bytes, 0, RequiredUpdateLength)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TransformInfo[]));
                tr = (TransformInfo)serializer.Deserialize(stringReader);
            }

        
            trs.SetPositionAndRotation(tr.Position, tr.Rotation);
            trs.localScale = tr.Scale;
#else
        int num = start;
        if (position)
        {
            trs.position = bytes.ToVector3(num);
            num += 3 * sizeof(float);
        }
        if (rotation)
        {
            trs.rotation =  bytes.ToQuaternion(num);
            num += 4 * sizeof(float); 
        } 
        if (scale) trs.localScale =  bytes.ToVector3(num);
#endif




        //This solution is actually just slower on the run time for no reason besides it can.



        }

        protected override void SendUpdate(ref byte[] bytes)
        {  
#if UseSerialized
            string xml;
            using (var stringWriter = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(TransformInfo));
                serializer.Serialize(stringWriter, new TransformInfo(transform));
                xml = stringWriter.ToString();
            }
//Not tested, but should work
            bytes = Encoding.UTF8.GetBytes(xml);
#else
            int n = 0;
        if (position)
        {
            transform.position.ToBytes(ref bytes, n);
            n += sizeof(float) * 3;
        }
        if (rotation)
        {
            transform.rotation.ToBytes(ref bytes, n);
            n += sizeof(float) * 4; 
        }

        if (scale)
        {
            transform.localScale.ToBytes(ref bytes,  n);
            n += sizeof(float) * 3;
        }
       
        
        //Doesn't factor previous because I'm lazy, but it could reduce netload.
        //Check if we've been a silly boy
        /*
        if (PreviousBuffer.SequenceEqual(UploadBuffer))
        {
            return; //Not sure what'll happen if we return nothing to be honest...
        }
        //We need to create a deep copy, not a shallow copy
        //Array.Copy(UploadBuffer, PreviousBuffer, UploadBuffer.Length);
        */
        return;
#endif
        }
    }
}
