using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;

namespace ThronefallMP.Network.Sync;

public class ResourceSync : BaseSync
{
    protected override BasePacket CreateSyncPacket(CSteamID peer)
    {
        return new SyncResourcePacket
        {
            Balance = GlobalData.Internal.Balance,
            NetWorth = GlobalData.Internal.NetWorth
        };
    }

    protected override bool Compare(CSteamID peer, BasePacket current, BasePacket last)
    {
        var a = (SyncResourcePacket)current;
        var b = (SyncResourcePacket)last;

        return a.Balance == b.Balance &&
               a.NetWorth == b.NetWorth;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncResourcePacket.PacketID;
    }

    protected override void HandlePacket(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncResourcePacket)packet;
        GlobalData.Internal.Balance = sync.Balance;
        GlobalData.Internal.NetWorth = sync.NetWorth;
    }
}