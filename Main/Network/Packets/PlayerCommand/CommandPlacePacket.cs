using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.PlayerCommand;

public class CommandPlacePacket : BasePacket
{
    public struct UnitData
    {
        public IdentifierData Unit;
        public Vector3 Home;
    }
    
    public const PacketId PacketID = PacketId.CommandPlace;

    public int Player;
    public List<UnitData> Units = new();

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Player);
        writer.Write(Units.Count);
        foreach (var unit in Units)
        {
            writer.Write(unit.Unit);
            writer.Write(unit.Home);
        }
    }

    public override void Receive(Buffer reader)
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