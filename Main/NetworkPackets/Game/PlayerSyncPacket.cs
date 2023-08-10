using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class PlayerSyncPacket : IPacket
{
    public const PacketId PacketID = PacketId.PlayerSyncPacket;
    
    public int PlayerID = -1;
    public PlayerNetworkData.Shared Data;
    
    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_UnreliableNoNagle;
    public int Channel => 0;
    
    public void Send(Buffer writer)
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

    public void Receive(Buffer reader)
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