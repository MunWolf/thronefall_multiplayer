using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PeerSyncPacket : IPacket
{
    public struct PlayerData
    {
        public int Id;
        public Vector3 Position;
    }
    
    public const PacketId PacketID = PacketId.PeerSyncPacket;

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public List<PlayerData> Players = new();
    public int LocalPlayer;
    
    public void Send(Buffer writer)
    {
        writer.Write(LocalPlayer);
        writer.Write(Players.Count);
        foreach (var data in Players)
        {
            writer.Write(data.Id);
            writer.Write(data.Position);
        }
    }

    public void Receive(Buffer reader)
    {
        LocalPlayer = reader.ReadInt32();
        Players.Clear();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; ++i)
        {
            Players.Add(new PlayerData
            {
                Id = reader.ReadInt32(),
                Position = reader.ReadVector3()
            });
        }
    }
}