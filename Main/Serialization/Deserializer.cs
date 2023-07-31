using System;
using UnityEngine;

namespace ThronefallMP;

public class Deserializer
{
    public byte[] Data;
    public int Pointer;
    
    private bool CanRead(int size)
    {
        return Data.Length <= Pointer + size;
    }
    
    public bool ReadBool()
    {
        if (CanRead(sizeof(bool)))
        {
            Pointer = Data.Length;
            return false;
        }

        var output = Data[Pointer] != 0;
        Pointer += sizeof(float);
        return output;
    }
    
    public int ReadInt()
    {
        if (CanRead(sizeof(int)))
        {
            Pointer = Data.Length;
            return 0;
        }

        var output = BitConverter.ToInt32(Data, Pointer);
        Pointer += sizeof(int);
        return output;
    }
    
    public float ReadFloat()
    {
        if (CanRead(sizeof(float)))
        {
            Pointer = Data.Length;
            return 0.0f;
        }

        var output = BitConverter.ToSingle(Data, Pointer);
        Pointer += sizeof(float);
        return output;
    }
    
    public Vector3 ReadVector3()
    {
        Vector3 output;
        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        return output;
    }
}