using Steamworks;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Network.Packets.Game;

public class ClientSyncPacket : BasePacket
{
    public const PacketId PacketID = PacketId.ClientSync;
    
    public int PlayerID = -1;
    public Vector3 Position;
    public PlayerNetworkData.Shared Data = new();
    
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override int DeliveryMask => Constants.k_nSteamNetworkingSend_UnreliableNoNagle;
    
    public override void Send(Buffer writer)
    {
        writer.Write(PlayerID);
        writer.Write(Position);
        writer.Write(Data.MoveHorizontal);
        writer.Write(Data.MoveVertical);
        writer.Write(Data.SprintToggleButton);
        writer.Write(Data.SprintButton);
        writer.Write(Data.InteractButton);
        writer.Write(Data.CallNightButton);
        writer.Write(Data.CommandUnitsButton);
    }

    public override void Receive(Buffer reader)
    {
        PlayerID = reader.ReadInt32();
        Position = reader.ReadVector3();
        Data.MoveHorizontal = reader.ReadFloat();
        Data.MoveVertical = reader.ReadFloat();
        Data.SprintToggleButton = reader.ReadBoolean();
        Data.SprintButton = reader.ReadBoolean();
        Data.InteractButton = reader.ReadBoolean();
        Data.CallNightButton = reader.ReadBoolean();
        Data.CommandUnitsButton = reader.ReadBoolean();
    }
}