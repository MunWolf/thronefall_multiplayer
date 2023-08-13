using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Patches;

namespace ThronefallMP.Network.Sync;

public class LevelDataSync : BaseSync
{
    protected override bool ShouldUpdate =>
        Plugin.Instance.Network.Server &&
        SceneTransitionManagerPatch.CurrentScene != "_StartMenu";

    protected override BasePacket CreateSyncPacket(CSteamID peer)
    {
        var packet = new SyncLevelDataPacket { Level = SceneTransitionManagerPatch.CurrentScene };
        foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            packet.Spawns.Add((player.Id, player.SpawnID));
        }
            
        foreach (var item in PerkManager.instance.CurrentlyEquipped)
        {
            packet.Perks.Add(EquipHandler.Convert(item.name));
        }
        
        return packet;
    }

    protected override bool Compare(CSteamID peer, BasePacket current, BasePacket last)
    {
        var a = (SyncLevelDataPacket)current;
        var b = (SyncLevelDataPacket)last;
        if (a.Perks.Count != b.Perks.Count || a.Spawns.Count != b.Spawns.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Perks.Count; ++i)
        {
            if (a.Perks[i] != b.Perks[i])
            {
                return false;
            }
        }

        for (var i = 0; i < a.Spawns.Count; ++i)
        {
            if (a.Spawns[i] != b.Spawns[i])
            {
                return false;
            }
        }
        
        return a.Level == b.Level;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID is
            SyncLevelDataPacket.PacketID or
            RequestLevelPacket.PacketID;
    }

    private void HandleRequestPacket(CSteamID peer, RequestLevelPacket packet)
    {
        if (packet.From != SceneTransitionManagerPatch.CurrentScene)
        {
            var id = new SteamNetworkingIdentity();
            id.SetSteamID(peer);
            Plugin.Instance.Network.SendSingle(CreateSyncPacket(peer), id);
            return;
        }
        
        EquipHandler.ClearEquipments();
        foreach (var perk in packet.Perks)
        {
            EquipHandler.EquipEquipment(perk);
        }
        
        SceneTransitionManagerPatch.Transition(packet.To, packet.From);
    }

    private void HandleSyncPacket(CSteamID peer, SyncLevelDataPacket packet)
    {
        EquipHandler.ClearEquipments();
        foreach (var perk in packet.Perks)
        {
            EquipHandler.EquipEquipment(perk);
        }
        
        if (packet.Level != SceneTransitionManagerPatch.CurrentScene)
        {
            SceneTransitionManagerPatch.Transition(packet.Level, null);
        }

        foreach (var spawn in packet.Spawns)
        {
            Plugin.Instance.PlayerManager.Get(spawn.playerId).SpawnID = spawn.spawnId;
        }
    }
    
    protected override void HandlePacket(CSteamID peer, BasePacket packet)
    {
        if (packet.TypeID == SyncLevelDataPacket.PacketID)
        {
            HandleSyncPacket(peer, (SyncLevelDataPacket)packet);
        }
        else
        {
            HandleRequestPacket(peer, (RequestLevelPacket)packet);
        }
    }
}