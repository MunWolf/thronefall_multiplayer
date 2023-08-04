using LiteNetLib;
using LiteNetLib.Utils;
using MonoMod.InlineRT;
using ThronefallMP.Components;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PlayerSyncPacket : IPacket
{
    public const PacketId PacketID = PacketId.PlayerSyncPacket;
    
    public int PlayerID = -1;
    public PlayerNetworkData.Shared Data;
    
    public PacketId TypeID()
    {
        return PacketID;
    }
    
    public void Send(ref NetDataWriter writer)
    {
        writer.Put(PlayerID);
        writer.Put(Data.Position);
        writer.Put(Data.MoveHorizontal);
        writer.Put(Data.MoveVertical);
        writer.Put(Data.SprintToggleButton);
        writer.Put(Data.SprintButton);
        writer.Put(Data.InteractButton);
        writer.Put(Data.CallNightButton);
        writer.Put(Data.CallNightFill);
        writer.Put(Data.CommandUnitsButton);
    }

    public void Receive(ref NetPacketReader reader)
    {
        PlayerID = reader.GetInt();
        Data.Position = reader.GetVector3();
        Data.MoveHorizontal = reader.GetFloat();
        Data.MoveVertical = reader.GetFloat();
        Data.SprintToggleButton = reader.GetBool();
        Data.SprintButton = reader.GetBool();
        Data.InteractButton = reader.GetBool();
        Data.CallNightButton = reader.GetBool();
        Data.CallNightFill = reader.GetFloat();
        Data.CommandUnitsButton = reader.GetBool();
    }
}