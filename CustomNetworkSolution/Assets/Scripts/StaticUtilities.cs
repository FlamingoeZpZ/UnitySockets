using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

//Thank you chat GPT
[AttributeUsage(AttributeTargets.Method)]
public class ReliableServerRPCAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class UnreliableServerRPCAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class ClientRPCAttribute : Attribute { }

public static class StaticUtilities
{

    #region Conversions

    #region Vector3

    public static byte[] ToBytes(this Vector3 vector3)
    {
        float[] floats = { vector3.x, vector3.y, vector3.z };
        byte[] bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static void ToBytes(this Vector3 vector3, [NotNull] ref byte[] bytes, int initialOffset)
    {
        if (initialOffset < 0 || initialOffset > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(initialOffset),
                "Initial offset is out of range or insufficient space in byte array.");

        float[] floats = { vector3.x, vector3.y, vector3.z };
        Buffer.BlockCopy(floats, 0, bytes, initialOffset, floats.Length * sizeof(float));
    }
    
    public static Vector3 ToVector3(this byte[] bytes, int startIndex)
    {
        if (startIndex < 0 || startIndex > bytes.Length)
            throw new ArgumentException("Invalid start index or insufficient data in byte array.", nameof(startIndex));

        float[] floats = new float[3];
        Buffer.BlockCopy(bytes, startIndex, floats, 0, floats.Length * sizeof(float));

       
        
        return new Vector3(floats[0], floats[1], floats[2]);
    }

    #endregion

    #region Quaternion

    public static byte[] ToBytes(this Quaternion quaternion)
    {
        float[] floats = { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        byte[] bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static void ToBytes(this Quaternion quaternion, [NotNull] ref byte[] bytes, int initialOffset)
    {
        if (initialOffset < 0 || initialOffset > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(initialOffset),
                "Initial offset is out of range or insufficient space in byte array.");
        
        float[] floats = { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        Buffer.BlockCopy(floats, 0, bytes, initialOffset , floats.Length * sizeof(float));
    }

    public static Quaternion ToQuaternion(this byte[] bytes, int startIndex)
    {
        if (startIndex < 0 || startIndex > bytes.Length)
            throw new ArgumentException("Invalid start index or insufficient data in byte array.", nameof(startIndex));

        float[] floats = new float[4];
        Buffer.BlockCopy(bytes, startIndex, floats, 0, floats.Length * sizeof(float));
        
        
        return new Quaternion(floats[0], floats[1], floats[2], floats[3]);
    }

    #endregion

    #endregion
}