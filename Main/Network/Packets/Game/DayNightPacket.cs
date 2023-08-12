using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class DayNightPacket : BasePacket
{
    public const PacketId PacketID = PacketId.DayNight;

    public bool Night;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;
    public override bool ShouldPropagate => true;

    public override void Send(Buffer writer)
    {
        writer.Write(Night);
    }

    public override void Receive(Buffer reader)
    {
        Night = reader.ReadBoolean();
    }
}