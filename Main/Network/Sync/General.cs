using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;

namespace ThronefallMP.Network.Sync;

public class General : BaseSync
{
    public General() : base(0) {}

    protected override Mode SyncMode => Mode.Request;

    // Possible syncs
    // DayNight
    protected override int Hash(CSteamID _)
    {
        return (
            GlobalData.Internal.Balance,
            GlobalData.Internal.NetWorth
        ).GetHashCode();
    }

    protected override BasePacket CreateSyncPacket(CSteamID _)
    {
        return new SyncGeneralPacket()
        {
            Balance = GlobalData.Internal.Balance,
            NetWorth = GlobalData.Internal.NetWorth
        };
    }

    protected override bool CanHandle(BasePacket packet)
    {
        return packet is SyncGeneralPacket;
    }

    protected override void Handle(CSteamID sender, BasePacket packet)
    {
        var sync = (SyncGeneralPacket)packet;
        GlobalData.Internal.Balance = sync.Balance;
        GlobalData.Internal.NetWorth = sync.NetWorth;
    }
}