using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class CommandPlacePacket : IPacket
{
    public struct UnitData
    {
        public IdentifierData Unit;
        public Vector3 Home;
    }
    
    public const PacketId PacketID = PacketId.CommandPlacePacket;

    public int Player;
    public List<UnitData> Units = new();

    public PacketId TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put(Player);
        writer.Put(Units.Count);
        foreach (var unit in Units)
        {
            writer.Put(unit.Unit);
            writer.Put(unit.Home);
        }
    }

    public void Receive(ref NetPacketReader reader)
    {
        Player = reader.GetInt();
        var count = reader.GetInt();
        Units.Clear();
        for (var i = 0; i < count; ++i)
        {
            Units.Add(new UnitData
            {
                Unit = reader.GetIdentifierData(),
                Home = reader.GetVector3()
            });
        }
    }
}