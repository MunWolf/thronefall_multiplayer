using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PlayerListPacket : IPacket
{
    public struct PlayerData
    {
        public int Id;
        public Vector3 Position;
    }
    
    public const int PacketID = 1;

    public List<PlayerData> Players = new();

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Players.Count);
        foreach (var data in Players)
        {
            writer.Put(data.Id);
            writer.Put(data.Position.x);
            writer.Put(data.Position.y);
            writer.Put(data.Position.z);
        }
    }

    public void Receive(ref NetPacketReader reader)
    {
        Players.Clear();
        var count = reader.GetInt();
        for (var i = 0; i < count; ++i)
        {
            Players.Add(new PlayerData
            {
                Id = reader.GetInt(),
                Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat())
            });
        }
    }
}