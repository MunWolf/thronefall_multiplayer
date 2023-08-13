using Steamworks;

namespace ThronefallMP.Network.Packets.Administration;

public class DisconnectPacket : BasePacket
{
    public enum Reason
    {
        Kicked,
        WrongPassword,
        WrongVersion
    }
    
    public const PacketId PacketID = PacketId.Disconnect;

    public Reason DisconnectReason;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.IsServer(sender);
    }
    
    public override void Send(Buffer writer)
    {
        Plugin.Log.LogInfo($"Sending disconnect reason {DisconnectReason}");
        writer.Write((int)DisconnectReason);
    }

    public override void Receive(Buffer reader)
    {
        DisconnectReason = (Reason)reader.ReadInt32();
        Plugin.Log.LogInfo($"Receiving disconnect reason {DisconnectReason}");
    }
}