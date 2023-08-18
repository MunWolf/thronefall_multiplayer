using System.Collections.Generic;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Sync;

public class SyncPlayersPacket : BasePacket
{
    public struct Player
    {
        public ushort Id;
        public byte SpawnId;
        public Vector3 Position;
        public PlayerNetworkData.Shared Shared;
    }
    
    public const PacketId PacketID = PacketId.SyncPlayer;
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.SyncPlayer;

    public List<Player> PlayerData = new();
    
    public override void Send(Buffer writer)
    {
        writer.Write(PlayerData.Count);
        foreach (var data in PlayerData)
        {
            writer.Write(data.Id);
            writer.Write(data.SpawnId);
            writer.Write(data.Position);
            writer.Write(data.Shared.MoveHorizontal);
            writer.Write(data.Shared.MoveVertical);
            writer.Write(data.Shared.SprintToggleButton);
            writer.Write(data.Shared.SprintButton);
            writer.Write(data.Shared.InteractButton);
            writer.Write(data.Shared.CallNightButton);
            writer.Write(data.Shared.CallNightFill);
            writer.Write(data.Shared.CommandUnitsButton);
        }
    }

    public override void Receive(Buffer reader)
    {
        PlayerData.Clear();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; ++i)
        {
            PlayerData.Add(new Player
            {
                Id = reader.ReadUInt16(),
                SpawnId = reader.ReadByte(),
                Position = reader.ReadVector3(),
                Shared = new PlayerNetworkData.Shared
                {
                    MoveHorizontal = reader.ReadFloat(),
                    MoveVertical = reader.ReadFloat(),
                    SprintToggleButton = reader.ReadBoolean(),
                    SprintButton = reader.ReadBoolean(),
                    InteractButton = reader.ReadBoolean(),
                    CallNightButton = reader.ReadBoolean(),
                    CallNightFill = reader.ReadFloat(),
                    CommandUnitsButton = reader.ReadBoolean()
                },
            });
        }
    }
}