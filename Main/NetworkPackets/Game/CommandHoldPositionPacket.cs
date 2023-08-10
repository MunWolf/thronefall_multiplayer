using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class CommandHoldPositionPacket : IPacket
{
    public struct UnitData
    {
        public IdentifierData Unit;
        public Vector3 Home;
    }
    
    public const PacketId PacketID = PacketId.CommandHoldPositionPacket;

    public int Player;
    public List<UnitData> Units = new();

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(Player);
        writer.Write(Units.Count);
        foreach (var unit in Units)
        {
            writer.Write(unit.Unit);
            writer.Write(unit.Home);
        }
    }

    public void Receive(Buffer reader)
    {
        Player = reader.ReadInt32();
        var count = reader.ReadInt32();
        Units.Clear();
        for (var i = 0; i < count; ++i)
        {
            Units.Add(new UnitData
            {
                Unit = reader.ReadIdentifierData(),
                Home = reader.ReadVector3()
            });
        }
    }
}