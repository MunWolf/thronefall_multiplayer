using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThronefallMP;

public class Serializer
{
    public readonly List<byte> Data = new();

    public void Write(bool item)
    {
        Data.Add((byte)(item ? 1 : 0));
    }

    public void Write(int item)
    {
        foreach (var value in BitConverter.GetBytes(item))
        {
            Data.Add(value);
        }
    }
    
    public void Write(float item)
    {
        foreach (var value in BitConverter.GetBytes(item))
        {
            Data.Add(value);
        }
    }
    
    public void Write(Vector3 item)
    {
        Write(item.x);
        Write(item.y);
        Write(item.z);
    }
}