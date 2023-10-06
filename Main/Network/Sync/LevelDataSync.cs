using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Patches;
using ThronefallMP.UI;
using ThronefallMP.UI.Dialogs;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class LevelDataSync : BaseSync
{
    private class LevelRequest
    {
        public string To;
        public string From;
        public readonly Dictionary<int, Equipment> SelectedWeapons = new();
    }

    private const float ResendWeaponRequestTime = 4f;
    
    private LevelRequest _activeRequest;
    private WeaponDialog _activeDialog;
    
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
            var equipment = Equip.Convert(item.name);
            if (equipment is Equipment.LongBow or Equipment.LightSpear or Equipment.HeavySword)
            {
                continue;
            }
            
            packet.Perks.Add(equipment);
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
            RestartLevelPacket.PacketID or
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

        if (packet.To == packet.From)
        {
            Plugin.Instance.Network.Send(new RestartLevelPacket(), true);
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
            if (Equip.Weapons.Contains(perk))
            {
                _activeRequest.SelectedWeapons[Plugin.Instance.PlayerManager.Get(peer).Id] = perk;
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

    private void ResendWeaponRequest()
    {
        var id = new SteamNetworkingIdentity();
        var request = new WeaponRequestPacket();
        foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            if (_activeRequest.SelectedWeapons.ContainsKey(player.Id))
            {
                continue;
            }
            
            id.SetSteamID(player.SteamID);
            Plugin.Instance.Network.SendSingle(request, id);
        }
    }

    private IEnumerator RequestHandler()
    {
        var timer = 0f;
        while (
            _activeRequest != null &&
            !Plugin.Instance.PlayerManager.GetAllPlayers().All(
                p => _activeRequest.SelectedWeapons.ContainsKey(p.Id)
            ))
        {
            timer += Time.deltaTime;
            if (timer > ResendWeaponRequestTime)
            {
                ResendWeaponRequest();
                timer = 0f;
            }
            
            yield return null;
        }

        if (_activeRequest == null)
        {
            yield break;
        }
        
        foreach (var weapon in _activeRequest.SelectedWeapons)
        {
            var player = Plugin.Instance.PlayerManager.Get(weapon.Key);
            player.Weapon = weapon.Value;
            if (player.Id == Plugin.Instance.PlayerManager.LocalId)
            {
                Equip.EquipEquipment(player.Weapon);
            }
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
            if (player.Id == Plugin.Instance.PlayerManager.LocalId)
            {
                Equip.EquipEquipment(player.Weapon);
            }
        }
        
        if (packet.Level != SceneTransitionManagerPatch.CurrentScene)
        {
            SceneTransitionManagerPatch.Transition(packet.Level, SceneTransitionManagerPatch.CurrentScene);
        }
    }

    private void HandleWeaponRequestPacket()
    {
        if (_activeDialog != null)
        {
            return;
        }
        
        _activeDialog = UIManager.CreateWeaponDialog();
    }

    private void HandleWeaponResponsePacket(CSteamID peer, WeaponResponsePacket packet)
    {
        if (_activeRequest == null)
        {
            return;
        }
        
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
            case RestartLevelPacket.PacketID:
                SceneTransitionManagerPatch.Transition(
                    SceneTransitionManagerPatch.CurrentScene,
                    SceneTransitionManagerPatch.CurrentScene
                );
                break;
            case WeaponRequestPacket.PacketID:
                HandleWeaponRequestPacket();
                break;
            case WeaponResponsePacket.PacketID:
                HandleWeaponResponsePacket(peer, (WeaponResponsePacket)packet);
                break;
        }
    }
}