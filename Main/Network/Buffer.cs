using System;
using System.Text;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network;

public class Buffer
{
    public byte[] Data;
    public int ReadHead;
    public int WriteHead;

    public Buffer()
        : this(32)
    {}

    public Buffer(int capacity)
    {
        Data = new byte[capacity];
    }
    
    private void EnsureSize(int extra)
    {
        if (WriteHead + extra < Data.Length)
        {
            return;
        }

        Array.Resize(ref Data, Data.Length * 2);
    }

    public bool CanRead(int size)
    {
        return ReadHead + size < Data.Length;
    }
    
    public void Write(bool value)
    {
        EnsureSize(sizeof(bool));
        var output = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += sizeof(bool);
    }
    
    public void Write(byte value)
    {
        EnsureSize(1);
        Data[WriteHead] = value;
        WriteHead += 1;
    }
    
    public void Write(int value)
    {
        EnsureSize(sizeof(int));
        var output = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(long value)
    {
        EnsureSize(sizeof(long));
        var output = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(float value)
    {
        EnsureSize(sizeof(float));
        var output = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(string value)
    {
        var output = Encoding.ASCII.GetBytes(value);
        Write(output.Length);
        EnsureSize(output.Length);
        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }
    
    public void Write(Quaternion value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
        Write(value.w);
    }
    
    public void Write(IdentifierData value)
    {
        Write((int)value.Type);
        Write(value.Id);
    }

    public bool ReadBoolean()
    {
        if (!CanRead(sizeof(bool)))
        {
            return false;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(bool));
        }
        
        var output = BitConverter.ToBoolean(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(bool));
        }
        
        ReadHead += sizeof(bool);
        return output;
    }

    public int ReadInt32()
    {
        if (!CanRead(sizeof(int)))
        {
            return 0;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(int));
        }
        
        var output = BitConverter.ToInt32(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(int));
        }
        
        ReadHead += sizeof(int);
        return output;
    }

    public long ReadInt64()
    {
        if (!CanRead(sizeof(long)))
        {
            return 0;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(long));
        }
        
        var output = BitConverter.ToInt64(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(long));
        }
        
        ReadHead += sizeof(long);
        return output;
    }

    public float ReadFloat()
    {
        if (!CanRead(sizeof(float)))
        {
            return 0;
        }
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(float));
        }

        var output = BitConverter.ToSingle(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(float));
        }
        
        ReadHead += sizeof(float);
        return output;
    }
    
    public string ReadString()
    {
        var old = ReadHead;
        var size = ReadInt32();
        if (!CanRead(size))
        {
            ReadHead = old;
            return "";
        }
        
        var output = Encoding.ASCII.GetString(Data, ReadHead, size);
        ReadHead += size;
        return output;
    }

    public Vector3 ReadVector3()
    {
        var output = new Vector3();
        if (!CanRead(3 * sizeof(float)))
        {
            return output;
        }

        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        return output;
    }

    public Quaternion ReadQuaternion()
    {
        var output = new Quaternion();
        if (!CanRead(4 * sizeof(float)))
        {
            return output;
        }

        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        output.w = ReadFloat();
        return output;
    }

    public IdentifierData ReadIdentifierData()
    {
        if (!CanRead(2 * sizeof(int)))
        {
            return IdentifierData.Invalid;
        }

        IdentifierData data;
        data.Type = (IdentifierType)ReadInt32();
        data.Id = ReadInt32();
        return data;
    }
}