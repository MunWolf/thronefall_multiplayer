using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;

namespace ThronefallMP.Network.Sync;

// Used to get ping information and keep the session alive.
public class PingPongSync : BaseSync
{
    protected override float ForceUpdateTimer => 2f;

    protected override BasePacket CreateSyncPacket(CSteamID peer)
    {
        return SyncPingPacket.WithTime();
    }

    protected override bool Compare(CSteamID peer, BasePacket current, BasePacket last)
    {
        // We only send the packets when the ForceUpdateTimer is up.
        return true;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID is
            SyncPingPacket.PacketID or
            SyncPongPacket.PacketID or
            SyncPingInfoPacket.PacketID;
    }

    protected override void HandlePacket(CSteamID peer, BasePacket packet)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (packet.TypeID)
        {
            case SyncPingPacket.PacketID:
            {
                var id = new SteamNetworkingIdentity();
                id.SetSteamID(peer);
                Plugin.Instance.Network.SendSingle(SyncPongPacket.FromPing((SyncPingPacket)packet), id);
                break;
            }
            case SyncPongPacket.PacketID:
            {
                var sync = (SyncPongPacket)packet;
                Plugin.Instance.Network.Send(SyncPingInfoPacket.FromPong(peer, sync), true);
                break;
            }
            default:
            {
                var sync = (SyncPingInfoPacket)packet;
                Plugin.Instance.PlayerManager.Get(sync.Peer).Ping = sync.Ping;
                Plugin.Log.LogInfoFiltered("Ping", $"Received {peer.m_SteamID} with ping {sync.Ping}");
                break;
            }
        }
    }
}