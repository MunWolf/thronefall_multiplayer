using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Administration;

public class PeerListPacket : BasePacket
{
    public struct PlayerData
    {
        public ushort Id;
        public CSteamID SteamId;
        public byte SpawnId;
        public Vector3 Position;
    }
    
    public const PacketId PacketID = PacketId.PeerSync;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }

    public List<PlayerData> Players = new();
    
    public override void Send(Buffer writer)
    {
        writer.Write(Players.Count);
        foreach (var data in Players)
        {
            writer.Write(data.Id);
            writer.Write(data.SteamId);
            writer.Write(data.SpawnId);
            writer.Write(data.Position);
        }
    }

    public override void Receive(Buffer reader)
    {
        Players.Clear();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; ++i)
        {
            Players.Add(new PlayerData
            {
                Id = reader.ReadUInt16(),
                SteamId = reader.ReadSteamID(),
                SpawnId = reader.ReadByte(),
                Position = reader.ReadVector3()
            });
        }
    }
}