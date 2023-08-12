using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Patches;

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
                Position = player.Object != null ? player.Object.transform.position : player.SpawnLocation,
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
            if (player.Id == Plugin.Instance.PlayerManager.LocalId)
            {
                continue;
            }
            
            data.Shared.Set(player.Shared);
            if (data.Object == null)
            {
                continue;
            }
            
            var deltaPosition = player.Position - data.Object.transform.position;
            if (deltaPosition.sqrMagnitude < PlayerMovementPatch.MaximumDevianceSquared(data.SteamID))
            {
                continue;
            }
            
            Plugin.Log.LogInfo($"MaximumDeviance reached, moving player to {player.Position}");
            data.Controller.enabled = false;
            data.Object.transform.position = player.Position;
            data.Controller.enabled = true;
        }
    }
}