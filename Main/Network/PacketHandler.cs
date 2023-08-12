using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Patches;
using ThronefallMP.UI;

namespace ThronefallMP.Network;

public enum PacketId
{
    // These 3 will always have the same packetid between versions.
    Approval,
    Disconnect,
    PeerSync,
    
    Balance,
    BuildOrUpgrade,
    CancelBuild,
    CommandAdd,
    CommandHoldPosition,
    CommandPlace,
    ConfirmBuild,
    Damage,
    DayNight,
    EnemySpawn,
    Heal,
    ManualAttack,
    PlayerSync,
    Position,
    Respawn,
    ScaleHp,
    TransitionToScene,
}

public static class PacketHandler
{
    public static bool AwaitingConnectionApproval;
    
    private static readonly Dictionary<PacketId, Action<SteamNetworkingIdentity, BasePacket>> Handlers = new()
    {
        { ApprovalPacket.PacketID, HandleApproval },
        { DisconnectPacket.PacketID, HandleDisconnect },
        { PeerSyncPacket.PacketID, HandlePeerSync },
        
        { BalancePacket.PacketID, HandleBalance },
        { BuildOrUpgradePacket.PacketID, HandleBuildOrUpgrade },
        { CancelBuildPacket.PacketID, HandleCancelBuild },
        { CommandAddPacket.PacketID, HandleCommandAdd },
        { CommandPlacePacket.PacketID, HandleCommandPlace },
        { CommandHoldPositionPacket.PacketID, HandleCommandHoldPosition },
        { ConfirmBuildPacket.PacketID, HandleConfirmBuild },
        { DamagePacket.PacketID, HandleDamage },
        { DayNightPacket.PacketID, HandleDayNight },
        { EnemySpawnPacket.PacketID, HandleEnemySpawn },
        { HealPacket.PacketID, HandleHeal },
        { ManualAttackPacket.PacketID, HandleManualAttack },
        { PlayerSyncPacket.PacketID, HandlePlayerSync },
        { PositionPacket.PacketID, HandlePosition },
        { RespawnPacket.PacketID, HandleRespawn },
        { ScaleHpPacket.PacketID, HandleScaleHp },
        { TransitionToScenePacket.PacketID, HandleTransitionToScene },
    };

    public static void HandlePacket(SteamNetworkingIdentity sender, BasePacket basePacket)
    {
        var found = Handlers.TryGetValue(basePacket.TypeID, out var handler);
        if (found)
        {
            Plugin.Log.LogDebugFiltered("PacketHandler", $"Handling {basePacket.TypeID} packet");
            handler(sender, basePacket);
        }
        else
        {
            Plugin.Log.LogWarningFiltered("PacketHandler", $"No handler for packet {basePacket.TypeID}.");
        }
    }

    private static void HandlePeerSync(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (PeerSyncPacket)ipacket;

        if (AwaitingConnectionApproval)
        {
            // Currently we only allow joining a lobby if we are in level select.
            SceneTransitionManagerPatch.DisableTransitionHook = true;
            SceneTransitionManager.instance.TransitionFromNullToLevelSelect();
            SceneTransitionManagerPatch.DisableTransitionHook = false;
            UIManager.CloseAllPanels();
            AwaitingConnectionApproval = false;
        }
        
        Plugin.Log.LogInfoFiltered("PacketHandler", "Received player list");
        var steamId = SteamUser.GetSteamID();
        Plugin.Instance.PlayerManager.LocalId = 0;
        foreach (var data in packet.Players)
        {
            if (data.SteamId == steamId)
            {
                Plugin.Instance.PlayerManager.LocalId = data.Id;
            }
            
            var player = Plugin.Instance.PlayerManager.Create(data.SteamId, data.Id);
            player.Shared.Position = data.Position;
            if (player.Object == null)
            {
                continue;
            }
            
            player.Controller.enabled = false;
            player.Object.transform.position = data.Position;
            player.Controller.enabled = true;
        }
    }

    private static void HandlePlayerSync(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (PlayerSyncPacket)ipacket;
        var data = Plugin.Instance.PlayerManager.Get(packet.PlayerID)?.Shared;
        if (data != null)
        {
            data.Set(packet.Data);
        }
    }

    private static void HandleTransitionToScene(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (TransitionToScenePacket)ipacket;
        PerkManager.instance.CurrentlyEquipped.Clear();
        Plugin.Log.LogInfoFiltered("PacketHandler", $"-------- Loading Level {packet.Level} --------");
        foreach (var perk in packet.Perks)
        {
            var equippable = EquippableConverters.Convert(perk);
            PerkManager.instance.CurrentlyEquipped.Add(equippable);
            Plugin.Log.LogInfoFiltered("PacketHandler", $"- Perk {perk} : {equippable}");
        }
        
        SceneTransitionManagerPatch.DisableTransitionHook = true;
        var gameplayScene = Traverse.Create(SceneTransitionManager.instance).Field<string>("comingFromGameplayScene");
        gameplayScene.Value = packet.ComingFromGameplayScene;
        SceneTransitionManager.instance.TransitionFromNullToLevel(packet.Level);
        SceneTransitionManagerPatch.DisableTransitionHook = false;
    }

    private static void HandleDayNight(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (DayNightPacket)ipacket;
        if (packet.Night)
        {
            NightCallPatch.TriggerNightFall();
        }
    }

    private static void HandleEnemySpawn(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (EnemySpawnPacket)ipacket;
        EnemySpawnerPatch.SpawnEnemy(packet.Wave, packet.Spawn, packet.Position, packet.Id, packet.Coins);
    }

    private static void HandleDamage(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (DamagePacket)ipacket;
        HpPatch.InflictDamage(
            packet.Target,
            packet.Source,
            packet.Damage,
            packet.CausedByPlayer,
            packet.InvokeFeedbackEvents
        );
    }

    private static void HandleHeal(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (HealPacket)ipacket;
        HpPatch.Heal(packet.Target, packet.Amount);
    }

    private static void HandleScaleHp(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (ScaleHpPacket)ipacket;
        HpPatch.ScaleHp(packet.Target, packet.Multiplier);
    }

    private static void HandlePosition(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (PositionPacket)ipacket;
        var target = packet.Target.Get();
        if (target != null)
        {
            target.transform.position = packet.Position;
        }
    }

    private static void HandleRespawn(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (PositionPacket)ipacket;
        var target = packet.Target.Get();
        if (target == null)
        {
            return;
        }
                
        switch (packet.Target.Type)
        {
            case IdentifierType.Ally:
            {
                var hp = target.GetComponent<Hp>();
                UnitRespawnerForBuildingsPatch.RevivePlayerUnit(hp, packet.Position);
                break;
            }
            case IdentifierType.Invalid:
            case IdentifierType.Player:
            case IdentifierType.Building:
            case IdentifierType.Enemy:
            default:
                Plugin.Log.LogWarningFiltered("PacketHandler", $"Received unhandled respawn packet for {packet.Target.Type}:{packet.Target.Id}");
                break;
        }
        target.transform.position = packet.Position;
    }

    private static void HandleCommandAdd(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (CommandAddPacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player);
        if (player.Object == null)
        {
            return;
        }
        
        var command = player.Object.GetComponent<CommandUnits>();
        foreach (var unit in packet.Units)
        {
            var component = unit.Get()?.GetComponent<PathfindMovementPlayerunit>();
            if (component != null)
            {
                CommandUnitsPatch.AddUnit(command, component);
            }
        }
    }

    private static void HandleCommandPlace(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (CommandPlacePacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player);
        if (player.Object == null)
        {
            return;
        }
        
        var command = player.Object.GetComponent<CommandUnits>();
        CommandUnitsPatch.EmitWaypoint(command, packet.Units.Count > 0);
        foreach (var unit in packet.Units)
        {
            var component = unit.Unit.Get()?.GetComponent<PathfindMovementPlayerunit>();
            if (component != null)
            {
                CommandUnitsPatch.PlaceUnit(command, component, unit.Home);
            }
        }
    }

    private static void HandleCommandHoldPosition(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (CommandHoldPositionPacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player);
        if (player.Object == null)
        {
            return;
        }
        
        var command = player.Object.GetComponent<CommandUnits>();
        if (packet.Units.Count > 0)
        {
            CommandUnitsPatch.PlayHoldSound(command);
        }
        
        foreach (var unit in packet.Units)
        {
            var component = unit.Unit.Get()?.GetComponent<PathfindMovementPlayerunit>();
            if (component != null)
            {
                CommandUnitsPatch.HoldPosition(component, unit.Home);
            }
        }
    }

    private static void HandleManualAttack(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (ManualAttackPacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player)?.Object;
        if (player == null)
        {
            return;
        }
        
        var attack = player.GetComponent<PlayerInteraction>().EquippedWeapon;
        attack.TryToAttack();
    }

    private static void HandleBalance(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (BalancePacket)ipacket;
        var delta = GlobalData.Internal.Balance - packet.Balance;
        if (delta == 0)
        {
            return;
        }
        
        GlobalData.Internal.Balance = packet.Balance;
        GlobalData.Internal.Networth = packet.Networth;
    }

    private static void HandleApproval(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (ApprovalPacket)ipacket;
        Plugin.Log.LogInfoFiltered("PacketHandler", $"Handling approval of {sender.GetSteamID64()}");
        if (!packet.SameVersion)
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"{sender.GetSteamID64()} has wrong version");
            Plugin.Instance.Network.KickPeer(sender.GetSteamID(), DisconnectPacket.Reason.WrongVersion);
        }
        else if (Plugin.Instance.Network.Authenticate(packet.Password))
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"{sender.GetSteamID64()} Authenticated");
            Plugin.Instance.Network.AddPlayer(sender.GetSteamID());
        }
        else
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Authentication of {sender.GetSteamID64()} failed");
            Plugin.Instance.Network.KickPeer(sender.GetSteamID(), DisconnectPacket.Reason.WrongPassword);
        }
    }

    private static void HandleDisconnect(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (DisconnectPacket)ipacket;
        AwaitingConnectionApproval = false;
        Plugin.Log.LogInfoFiltered("PacketHandler", $"Disconnected with reason {packet.DisconnectReason}");
        var message = packet.DisconnectReason switch
        {
            DisconnectPacket.Reason.Kicked => "You were kicked!",
            DisconnectPacket.Reason.WrongPassword => "You gave the wrong password.",
            DisconnectPacket.Reason.WrongVersion => "Different multiplayer mod version.",
            _ => "Unknown"
        };
            
        UIManager.CreateMessageDialog("Disconnected", message);
    }

    private static void HandleBuildOrUpgrade(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        if (!Plugin.Instance.Network.Server)
        {
            return;
        }
        
        var packet = (BuildOrUpgradePacket)ipacket;
        var info = BuildSlotPatch.GetUpgradeInfo(packet.BuildingId, packet.Level, packet.Choice);
        if (GlobalData.Balance < info.Cost)
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Cancel building {packet.BuildingId}:{packet.Level}:{packet.Choice} for {info.Cost}");
            Plugin.Instance.Network.SendSingle(new CancelBuildPacket()
            {
                BuildingId = packet.BuildingId
            }, sender);
        }
        else
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Confirmed building {packet.BuildingId}:{packet.Level}:{packet.Choice} for {info.Cost}");
            Plugin.Instance.Network.Send(new ConfirmBuildPacket()
            {
                BuildingId = packet.BuildingId,
                Level = packet.Level,
                Choice = packet.Choice,
                PlayerID = Plugin.Instance.PlayerManager.Get(sender.GetSteamID()).Id
            }, true);
            GlobalData.Balance -= info.Cost;
        }
    }

    private static void HandleCancelBuild(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (CancelBuildPacket)ipacket;
        Plugin.Log.LogInfoFiltered("PacketHandler", $"Local build of {packet.BuildingId} cancelled");
        BuildSlotPatch.CancelBuild(packet.BuildingId);
        GlobalData.LocalBalanceDelta = 0;
    }

    private static void HandleConfirmBuild(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (ConfirmBuildPacket)ipacket;
        if (packet.PlayerID == Plugin.Instance.PlayerManager.LocalPlayer.Id)
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Local build of {packet.BuildingId}:{packet.Level}:{packet.Choice} confirmed");
            GlobalData.Internal.Balance += GlobalData.LocalBalanceDelta;
            GlobalData.Internal.Networth += GlobalData.LocalBalanceDelta;
            GlobalData.LocalBalanceDelta = 0;
        }
        else
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Building {packet.BuildingId}:{packet.Level}:{packet.Choice}");
        }
        
        BuildSlotPatch.HandleUpgrade(
            packet.PlayerID,
            packet.BuildingId,
            packet.Level,
            packet.Choice
        );
    }
}