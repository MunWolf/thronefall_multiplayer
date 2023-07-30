using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThronefallMP;

public class Serializer
{
    public readonly List<byte> Data = new();
    
    public int Pointer = 0;

    private bool CanRead(int size)
    {
        return Data.Count <= Pointer + size;
    }
    
    public bool ReadBool()
    {
        if (CanRead(1))
        {
            Pointer = Data.Count;
            return false;
        }

        return Data[Pointer] != 0;
    }
    
    public float ReadFloat()
    {
        if (CanRead(4))
        {
            Pointer = Data.Count;
            return 0.0f;
        }

        BitConverter.ToSingle(Data, Pointer);
    }
    
    public Vector3 ReadVector3()
    {
        Vector3 output;
        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        return output;
    }
    
    public void Write(bool item)
    {
        
    }
    
    public void Write(float item)
    {
        
    }
    
    public void Write(Vector3 item)
    {
        
    }
}