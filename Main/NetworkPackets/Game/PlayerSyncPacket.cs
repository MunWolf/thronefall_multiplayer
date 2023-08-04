using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class PlayerSyncPacket : IPacket
{
    public const PacketId PacketID = PacketId.PlayerSyncPacket;
    
    public int PlayerID = -1;
    public PlayerNetworkData.Shared Data;
    
    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableSequenced;
    public int Channel => 0;
    
    public void Send(NetBuffer writer)
    {
        writer.Write(PlayerID);
        writer.Write(Data.Position);
        writer.Write(Data.MoveHorizontal);
        writer.Write(Data.MoveVertical);
        writer.Write(Data.SprintToggleButton);
        writer.Write(Data.SprintButton);
        writer.Write(Data.InteractButton);
        writer.Write(Data.CallNightButton);
        writer.Write(Data.CallNightFill);
        writer.Write(Data.CommandUnitsButton);
    }

    public void Receive(NetBuffer reader)
    {
        PlayerID = reader.ReadInt32();
        Data.Position = reader.ReadVector3();
        Data.MoveHorizontal = reader.ReadFloat();
        Data.MoveVertical = reader.ReadFloat();
        Data.SprintToggleButton = reader.ReadBoolean();
        Data.SprintButton = reader.ReadBoolean();
        Data.InteractButton = reader.ReadBoolean();
        Data.CallNightButton = reader.ReadBoolean();
        Data.CallNightFill = reader.ReadFloat();
        Data.CommandUnitsButton = reader.ReadBoolean();
    }
}