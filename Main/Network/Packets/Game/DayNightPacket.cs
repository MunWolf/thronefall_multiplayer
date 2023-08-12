using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class DayNightPacket : BasePacket
{
    public const PacketId PacketID = PacketId.DayNight;

    public DayNightCycle.Timestate Timestate;
    public float NightLength;

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Game;

    public override void Send(Buffer writer)
    {
        writer.Write(Timestate == DayNightCycle.Timestate.Day);
        if (Timestate == DayNightCycle.Timestate.Day)
        {
            writer.Write(NightLength);
        }
    }

    public override void Receive(Buffer reader)
    {
        Timestate = reader.ReadBoolean() ? DayNightCycle.Timestate.Day : DayNightCycle.Timestate.Night;
        if (Timestate == DayNightCycle.Timestate.Day)
        {
            NightLength = reader.ReadFloat();
        }
    }
}