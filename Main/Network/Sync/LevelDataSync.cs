using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Patches;
using ThronefallMP.UI;

namespace ThronefallMP.Network.Sync;

public class LevelDataSync : BaseSync
{
    private class LevelRequest
    {
        public string To;
        public string From;
        public readonly Dictionary<int, Equipment> SelectedWeapons = new();
    }

    private LevelRequest _activeRequest;
    
    protected override bool ShouldUpdate =>
        Plugin.Instance.Network.Server &&
        SceneTransitionManagerPatch.CurrentScene != "_StartMenu";
    
    protected override BasePacket CreateSyncPacket(CSteamID peer)
    {
        var packet = new SyncLevelDataPacket { Level = SceneTransitionManagerPatch.CurrentScene };
        foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            packet.PlayerData.Add(new SyncLevelDataPacket.Player
            {
                PlayerId = player.Id,
                SpawnId = player.SpawnID,
                Weapon = player.Weapon
            });
        }
            
        foreach (var item in PerkManager.instance.CurrentlyEquipped)
        {
            packet.Perks.Add(Equip.Convert(item.name));
        }
        
        return packet;
    }

    protected override bool Compare(CSteamID peer, BasePacket current, BasePacket last)
    {
        var a = (SyncLevelDataPacket)current;
        var b = (SyncLevelDataPacket)last;
        if (a.Perks.Count != b.Perks.Count || a.PlayerData.Count != b.PlayerData.Count)
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

        for (var i = 0; i < a.PlayerData.Count; ++i)
        {
            var pa = a.PlayerData[i];
            var pb = b.PlayerData[i];
            if ((pa.PlayerId, pa.SpawnId, pa.Weapon) != (pb.PlayerId, pb.SpawnId, pb.Weapon))
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
            RequestLevelPacket.PacketID or
            WeaponRequestPacket.PacketID or
            WeaponResponsePacket.PacketID;
    }

    private void HandleRequestPacket(CSteamID peer, RequestLevelPacket packet)
    {
        if (_activeRequest != null)
        {
            return;
        }
        
        if (packet.From != SceneTransitionManagerPatch.CurrentScene)
        {
            var id = new SteamNetworkingIdentity();
            id.SetSteamID(peer);
            Plugin.Instance.Network.SendSingle(CreateSyncPacket(peer), id);
            return;
        }

        if (packet.To == "_LevelSelect")
        {
            SceneTransitionManagerPatch.Transition(packet.To, packet.From);
            return;
        }
        
        _activeRequest = new LevelRequest
        {
            To = packet.To,
            From = packet.From
        };
        Equip.ClearEquipments();
        foreach (var perk in packet.Perks)
        {
            switch (perk)
            {
                case Equipment.LongBow:
                case Equipment.LightSpear:
                case Equipment.HeavySword:
                    _activeRequest.SelectedWeapons[Plugin.Instance.PlayerManager.Get(peer).Id] = perk;
                    break;
                default:
                    Equip.EquipEquipment(perk);
                    break;
            }
        }

        Plugin.Log.LogInfo("Sending weapon request.");
        var sentByServer = peer == Plugin.Instance.PlayerManager.LocalPlayer.SteamID;
        var request = new WeaponRequestPacket();
        Plugin.Instance.Network.Send(
            request,
            !sentByServer,
            peer
        );
        
        Plugin.Instance.StartCoroutine(RequestHandler());
    }

    private IEnumerator RequestHandler()
    {
        while (
            _activeRequest != null &&
            !Plugin.Instance.PlayerManager.GetAllPlayers().All(
                p => _activeRequest.SelectedWeapons.ContainsKey(p.Id)
            ))
        {
            yield return null;
        }

        if (_activeRequest == null)
        {
            yield break;
        }
        
        foreach (var weapon in _activeRequest.SelectedWeapons)
        {
            Plugin.Instance.PlayerManager.Get(weapon.Key).Weapon = weapon.Value;
        }

        SceneTransitionManagerPatch.Transition(_activeRequest.To, _activeRequest.From);
        _activeRequest = null;
    }

    private static void HandleSyncPacket(SyncLevelDataPacket packet)
    {
        Equip.ClearEquipments();
        foreach (var perk in packet.Perks)
        {
            Equip.EquipEquipment(perk);
        }

        foreach (var data in packet.PlayerData)
        {
            var player = Plugin.Instance.PlayerManager.Get(data.PlayerId);
            player.SpawnID = data.SpawnId;
            player.Weapon = data.Weapon;
        }
        
        if (packet.Level != SceneTransitionManagerPatch.CurrentScene)
        {
            SceneTransitionManagerPatch.Transition(packet.Level, null);
        }
    }

    private void HandleWeaponRequestPacket(CSteamID peer)
    {
        UIManager.CreateWeaponDialog();
    }

    private void HandleWeaponResponsePacket(CSteamID peer, WeaponResponsePacket packet)
    {
        _activeRequest.SelectedWeapons[Plugin.Instance.PlayerManager.Get(peer).Id] = packet.Weapon;
    }
    
    protected override void HandlePacket(CSteamID peer, BasePacket packet)
    {
        switch (packet.TypeID)
        {
            case SyncLevelDataPacket.PacketID:
                HandleSyncPacket((SyncLevelDataPacket)packet);
                break;
            case RequestLevelPacket.PacketID:
                HandleRequestPacket(peer, (RequestLevelPacket)packet);
                break;
            case WeaponRequestPacket.PacketID:
                HandleWeaponRequestPacket(peer);
                break;
            case WeaponResponsePacket.PacketID:
                HandleWeaponResponsePacket(peer, (WeaponResponsePacket)packet);
                break;
        }
    }
}