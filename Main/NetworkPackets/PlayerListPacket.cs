using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PlayerListPacket : IPacket
{
    public struct PlayerData
    {
        public int Id;
        public Vector3 Position;
    }
    
    public const PacketId PacketID = PacketId.PlayerListPacket;

    public List<PlayerData> Players = new();

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Players.Count);
        foreach (var data in Players)
        {
            writer.Put(data.Id);
            writer.Put(data.Position);
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
                Position = reader.GetVector3()
            });
        }
    }
}