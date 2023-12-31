﻿using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;

namespace ThronefallMP.Network.Packets.PlayerCommand;

public class CommandAddPacket : BasePacket
{
    public const PacketId PacketID = PacketId.CommandAdd;

    public ushort Player;
    public List<IdentifierData> Units = new();

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public override void Send(Buffer writer)
    {
        writer.Write(Player);
        writer.Write((byte)Units.Count);
        foreach (var unit in Units)
        {
            writer.Write(unit);
        }
    }

    public override void Receive(Buffer reader)
    {
        Player = reader.ReadUInt16();
        var count = reader.ReadByte();
        Units.Clear();
        for (var i = 0; i < count; ++i)
        {
            Units.Add(reader.ReadIdentifierData());
        }
    }
}