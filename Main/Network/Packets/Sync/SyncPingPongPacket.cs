using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncPingPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncPing;

    public uint TimeMs;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;

    public static SyncPingPacket WithTime()
    {
        return new SyncPingPacket()
        {
            TimeMs = (uint)(Time.unscaledTime * 1000)
        };
    }
    
    public override void Send(Buffer writer)
    {
        writer.Write(TimeMs);
    }

    public override void Receive(Buffer reader)
    {
        TimeMs = reader.ReadUInt32();
    }
}

public class SyncPongPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncPong;

    public uint TimeMs;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;

    public static SyncPongPacket FromPing(SyncPingPacket ping)
    {
        return new SyncPongPacket()
        {
            TimeMs = ping.TimeMs
        };
    }
    
    public override void Send(Buffer writer)
    {
        writer.Write(TimeMs);
    }

    public override void Receive(Buffer reader)
    {
        TimeMs = reader.ReadUInt32();
    }
}

public class SyncPingInfoPacket : BasePacket
{
    public const PacketId PacketID = PacketId.SyncPingInfo;

    public CSteamID Peer;
    public uint Ping;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_Unreliable;

    public static SyncPingInfoPacket FromPong(CSteamID sender, SyncPongPacket pong)
    {
        return new SyncPingInfoPacket()
        {
            Peer = sender,
            Ping = (uint)(Time.unscaledTime * 1000) - pong.TimeMs
        };
    }
    
    public override void Send(Buffer writer)
    {
        writer.Write(Peer);
        writer.Write(Ping);
    }

    public override void Receive(Buffer reader)
    {
        Peer = reader.ReadSteamID();
        Ping = reader.ReadUInt32();
    }
}