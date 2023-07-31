using LiteNetLib;
using LiteNetLib.Utils;
using MonoMod.InlineRT;
using UnityEngine;

namespace ThronefallMP.NetworkPackets;

public class PlayerSyncPacket : IPacket
{
    public const int PacketID = 2;
    public int PlayerID = -1;
    public PlayerNetworkData.Shared Data;
    
    public int TypeID()
    {
        return PacketID;
    }
    
    public void Send(ref NetDataWriter writer)
    {
        writer.Put(PlayerID);
        writer.Put(Data.Position.x);
        writer.Put(Data.Position.y);
        writer.Put(Data.Position.z);
        writer.Put(Data.MoveHorizontal);
        writer.Put(Data.MoveVertical);
        writer.Put(Data.SprintToggleButton);
        writer.Put(Data.SprintButton);
        writer.Put(Data.InteractButton);
    }

    public void Receive(ref NetPacketReader reader)
    {
        PlayerID = reader.GetInt();
        Data.Position = new Vector3
        {
            x = reader.GetFloat(),
            y = reader.GetFloat(),
            z = reader.GetFloat()
        };
        Data.MoveHorizontal = reader.GetFloat();
        Data.MoveVertical = reader.GetFloat();
        Data.SprintToggleButton = reader.GetBool();
        Data.SprintButton = reader.GetBool();
        Data.InteractButton = reader.GetBool();
    }
}