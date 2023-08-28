using System;
using System.Text;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Utils;
using UnityEngine;

namespace ThronefallMP.Network;

public class Buffer
{
    public byte[] Data;
    public int ReadHead;
    public int WriteHead;
    public bool LastReadFailed;

    public Buffer()
        : this(32)
    {}

    public Buffer(int capacity)
    {
        Data = new byte[capacity];
    }
    
    private void EnsureSize(int extra)
    {
        if (WriteHead + extra <= Data.Length)
        {
            return;
        }

        Array.Resize(ref Data, Data.Length * 2);
    }

    public bool CanRead(int size)
    {
        return ReadHead + size <= Data.Length;
    }

    private bool CanReadInternal(int size)
    {
        var canRead = CanRead(size);
        LastReadFailed = !canRead;
        return canRead;
    }
    
    public void Write(bool value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(byte value)
    {
        EnsureSize(1);
        Data[WriteHead] = value;
        WriteHead += 1;
    }
    
    public void Write(short value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(ushort value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(int value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(uint value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(long value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(ulong value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(float value)
    {
        var output = BitConverter.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(Half value)
    {
        var output = Half.GetBytes(value);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(string value)
    {
        if (value == null)
        {
            Write((ushort)0);
            return;
        }
        
        var output = Encoding.ASCII.GetBytes(value);
        Write((ushort)output.Length);
        EnsureSize(output.Length);
        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }
    
    public void Write(Vector3 value, bool half = false)
    {
        if (half)
        {
            Write((Half)value.x);
            Write((Half)value.y);
            Write((Half)value.z);
        }
        else
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
    }
    
    public void Write(Quaternion value, bool half = false)
    {
        if (half)
        {
            Write((Half)value.x);
            Write((Half)value.y);
            Write((Half)value.z);
            Write((Half)value.w);
        }
        else
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }
    }
    
    public void Write(PacketId value)
    {
        Write((byte)value);
    }
    
    public void Write(Equipment value)
    {
        Write((byte)value);
    }
    
    public void Write(IdentifierData value)
    {
        Write((byte)value.Type);
        Write(value.Id);
    }
    
    public void Write(CSteamID value)
    {
        var output = BitConverter.GetBytes(value.m_SteamID);
        EnsureSize(output.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(output);
        }

        output.CopyTo(Data, WriteHead);
        WriteHead += output.Length;
    }

    public byte ReadByte()
    {
        if (!CanReadInternal(1))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read byte");
            return 0;
        }

        ReadHead += 1;
        return Data[ReadHead - 1];
    }

    public bool ReadBoolean()
    {
        if (!CanReadInternal(sizeof(bool)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read bool");
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

    public ushort ReadUInt16()
    {
        if (!CanReadInternal(sizeof(ushort)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read int");
            return 0;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(ushort));
        }
        
        var output = BitConverter.ToUInt16(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(ushort));
        }
        
        ReadHead += sizeof(ushort);
        return output;
    }

    public int ReadInt32()
    {
        if (!CanReadInternal(sizeof(int)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read int");
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

    public uint ReadUInt32()
    {
        if (!CanReadInternal(sizeof(uint)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read int");
            return 0;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(uint));
        }
        
        var output = BitConverter.ToUInt32(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(uint));
        }
        
        ReadHead += sizeof(uint);
        return output;
    }

    public long ReadInt64()
    {
        if (!CanReadInternal(sizeof(long)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read long");
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

    public ulong ReadUInt64()
    {
        if (!CanReadInternal(sizeof(ulong)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read long");
            return 0;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(ulong));
        }
        
        var output = BitConverter.ToUInt64(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(ulong));
        }
        
        ReadHead += sizeof(ulong);
        return output;
    }

    public Half ReadHalf()
    {
        if (!CanReadInternal(sizeof(ushort)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read half");
            return new Half(0);
        }
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(ushort));
        }

        var output = Half.ToHalf(Data, ReadHead);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(Data, ReadHead, sizeof(float));
        }

        ReadHead += sizeof(ushort);
        return output;
    }

    public float ReadFloat()
    {
        if (!CanReadInternal(sizeof(float)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read float");
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
        var size = ReadUInt16();
        if (LastReadFailed)
        {
            Plugin.Log.LogInfoFiltered("Buffer", $"Failed to read string length ({old}:{Data.Length})");
            ReadHead = old;
            return "";
        }
        
        if (!CanReadInternal(size))
        {
            Plugin.Log.LogInfoFiltered("Buffer", $"Failed to read string of length {size} ({old}:{Data.Length})");
            ReadHead = old;
            return "";
        }
        
        var output = Encoding.ASCII.GetString(Data, ReadHead, size);
        ReadHead += size;
        return output;
    }

    public Vector3 ReadVector3Half()
    {
        var output = new Vector3();
        if (!CanReadInternal(3 * sizeof(ushort)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read half vector3");
            return output;
        }

        output.x = ReadHalf();
        output.y = ReadHalf();
        output.z = ReadHalf();
        return output;
    }

    public Vector3 ReadVector3()
    {
        var output = new Vector3();
        if (!CanReadInternal(3 * sizeof(float)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read vector3");
            return output;
        }

        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        return output;
    }

    public Quaternion ReadQuaternionHalf()
    {
        var output = new Quaternion();
        if (!CanReadInternal(4 * sizeof(ushort)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read half quaternion");
            return output;
        }

        output.x = ReadHalf();
        output.y = ReadHalf();
        output.z = ReadHalf();
        output.w = ReadHalf();
        return output;
    }

    public Quaternion ReadQuaternion()
    {
        var output = new Quaternion();
        if (!CanReadInternal(4 * sizeof(float)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read quaternion");
            return output;
        }

        output.x = ReadFloat();
        output.y = ReadFloat();
        output.z = ReadFloat();
        output.w = ReadFloat();
        return output;
    }
    
    public PacketId ReadPacketId()
    {
        return (PacketId)ReadByte();
    }
    
    public Equipment ReadEquipment()
    {
        return (Equipment)ReadByte();
    }

    public IdentifierData ReadIdentifierData()
    {
        if (!CanReadInternal(sizeof(byte) + sizeof(short)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read identifier");
            return IdentifierData.Invalid;
        }

        IdentifierData data;
        data.Type = (IdentifierType)ReadByte();
        data.Id = ReadUInt16();
        return data;
    }
    
    public CSteamID ReadSteamID()
    {
        if (!CanReadInternal(sizeof(ulong)))
        {
            Plugin.Log.LogInfoFiltered("Buffer", "Failed to read identifier");
            return CSteamID.Nil;
        }

        return new CSteamID(ReadUInt64());
    }
}