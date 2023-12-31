﻿using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncAllyPathfinderPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncAllyPathfinder;
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.SyncUnit;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;

    public ushort Ally;
    public IdentifierData TargetObject;
    public Vector3 HomePosition;
    public bool HasReachedHomePositionAlready;
    public bool Slowed;
    public ushort PathIndex;
    public List<Vector3> Path = new();
    
    public override void Send(Buffer writer)
    {
        writer.Write(Ally);
        writer.Write(TargetObject);
        writer.Write(HomePosition);
        writer.Write(Slowed);
        writer.Write(PathIndex);
        writer.Write((ushort)Path.Count);
        foreach (var point in Path)
        {
            writer.Write(point);
        }
    }

    public override void Receive(Buffer reader)
    {
        Path.Clear();
        Ally = reader.ReadUInt16();
        TargetObject = reader.ReadIdentifierData();
        HomePosition = reader.ReadVector3();
        Slowed = reader.ReadBoolean();
        PathIndex = reader.ReadUInt16();
        var count = reader.ReadUInt16();
        for (var i = 0; i < count; ++i)
        {
            Path.Add(reader.ReadVector3());
        }
    }
}
