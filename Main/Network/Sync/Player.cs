using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;

namespace ThronefallMP.Network.Sync;

public class Player : BaseSync
{
    public Player() : base(1) {}

    protected override int Hash(CSteamID sender)
    {
        var players = Plugin.Instance.PlayerManager.GetAllPlayers();
        var hash = 0;
        foreach (var player in players)
        {
            // Don't include the player requesting the sync.
            if (sender == player.SteamID)
            {
                continue;
            }
            
            hash = (
                hash,
                player.Object != null ? player.Object.transform.position : player.SpawnLocation,
                player.Shared.MoveHorizontal,
                player.Shared.MoveVertical,
                player.Shared.SprintToggleButton,
                player.Shared.SprintButton,
                player.Shared.InteractButton,
                player.Shared.CallNightButton,
                player.Shared.CallNightFill,
                player.Shared.CommandUnitsButton
            ).GetHashCode();
        }
        
        return hash;
    }

    protected override IEnumerable<(CSteamID peer, BasePacket packet)> CreateSyncPackets(IEnumerable<CSteamID> ids)
    {
        var packet = (SyncPlayerPacket)CreateSyncPacket(CSteamID.Nil);
        var list = packet.PlayerData;
        foreach (var id in ids)
        {
            var player = Plugin.Instance.PlayerManager.Get(id);
            packet.PlayerData = list.Where(p => p.Id != player.Id).ToList();
            yield return (id, packet);
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID sender)
    {
        var packet = new SyncPlayerPacket();
        var players = Plugin.Instance.PlayerManager.GetAllPlayers();
        foreach (var player in players)
        {
            // Don't include the player requesting the sync.
            if (sender == player.SteamID)
            {
                continue;
            }
            
            packet.PlayerData.Add(new SyncPlayerPacket.Player()
            {
                Id = player.Id,
                SpawnId = player.SpawnID,
                Shared = player.Shared,
            });
        }
        
        return packet;
    }

    protected override bool CanHandle(BasePacket packet)
    {
        return packet is SyncPlayerPacket;
    }

    protected override void Handle(CSteamID sender, BasePacket packet)
    {
        var sync = (SyncPlayerPacket)packet;
        foreach (var player in sync.PlayerData)
        {
            var data = Plugin.Instance.PlayerManager.Get(player.Id);
            data.SpawnID = player.SpawnId;
            data.Shared.Set(player.Shared);
        }
    }
}