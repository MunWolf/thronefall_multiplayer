using Steamworks;

namespace ThronefallMP.Network.Packets.Administration;

public class ApprovalPacket : BasePacket
{
    public const PacketId PacketID = PacketId.Approval;

    public bool SameVersion;
    public string Password;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.NetworkManagement;
    public override bool CanHandle(CSteamID sender)
    {
        return Plugin.Instance.Network.Server;
    }
    
    public override void Send(Buffer writer)
    {
        writer.Write(Plugin.VersionString);
        writer.Write(Password);
    }

    public override void Receive(Buffer reader)
    {
        Plugin.Log.LogInfo("Reading version");
        SameVersion = reader.ReadString() == Plugin.VersionString;
        Plugin.Log.LogInfo("Reading password");
        Password = reader.ReadString();
    }
}