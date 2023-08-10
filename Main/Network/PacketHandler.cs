using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;
using ThronefallMP.NetworkPackets.Game;
using ThronefallMP.Patches;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThronefallMP.Network;

public enum PacketId
{
    ApprovalPacket,
    PeerSyncPacket,
    
    BalancePacket,
    BuildOrUpgradePacket,
    CommandAddPacket,
    CommandPlacePacket,
    CommandHoldPositionPacket,
    DamagePacket,
    DayNightPacket,
    EnemySpawnPacket,
    HealPacket,
    ManualAttack,
    PlayerSyncPacket,
    PositionPacket,
    RespawnPacket,
    ScaleHpPacket,
    TransitionToScenePacket,
    SpawnCoinPacket,
}

public static class PacketHandler
{
    private static readonly Dictionary<PacketId, Action<SteamNetworkingIdentity, IPacket>> Handlers = new()
    {
        { ApprovalPacket.PacketID, HandleApproval },
        { PeerSyncPacket.PacketID, HandlePeerSync },
        
        { BalancePacket.PacketID, HandleBalance },
        { BuildOrUpgradePacket.PacketID, HandleBuildOrUpgrade },
        { CommandAddPacket.PacketID, HandleCommandAdd },
        { CommandHoldPositionPacket.PacketID, HandleCommandHoldPosition },
        { CommandPlacePacket.PacketID, HandleCommandPlace },
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
        { SpawnCoinPacket.PacketID, HandleSpawnCoin },
    };

    public static void HandlePacket(SteamNetworkingIdentity sender, IPacket packet)
    {
        var found = Handlers.TryGetValue(packet.TypeID, out var handler);
        if (found)
        {
            handler(sender, packet);
        }
        else
        {
            Plugin.Log.LogWarning($"No handler for packet {packet.TypeID}.");
        }
    }

    private static void HandlePeerSync(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (PeerSyncPacket)ipacket;
        Plugin.Log.LogInfo("Received player list");
        Plugin.Instance.PlayerManager.LocalId = packet.LocalPlayer;
        foreach (var data in packet.Players)
        {
            if (Plugin.Instance.PlayerManager.Get(data.Id) == null)
            {
                Plugin.Log.LogInfo($"Creating player {data.Id} at {data.Position}");
            }
            else
            {
                Plugin.Log.LogInfo($"Player {data.Id} exists at {data.Position}");
            }
            
            Plugin.Instance.PlayerManager.Create(data.Id);
        }
    }

    private static void HandlePlayerSync(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (PlayerSyncPacket)ipacket;
        var data = Plugin.Instance.PlayerManager.Get(packet.PlayerID).Shared;
        if (data != null)
        {
            data.Set(packet.Data);
        }
    }

    private static void HandleTransitionToScene(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (TransitionToScenePacket)ipacket;
        PerkManager.instance.CurrentlyEquipped.Clear();
        Plugin.Log.LogInfo($"-------- Loading Level {packet.Level} --------");
        foreach (var perk in packet.Perks)
        {
            var equippable = EquippableConverters.Convert(perk);
            PerkManager.instance.CurrentlyEquipped.Add(equippable);
            Plugin.Log.LogInfo($"- Perk {perk} : {equippable}");
        }
        
        SceneTransitionManagerPatch.DisableTransitionHook = true;
        var gameplayScene = Traverse.Create(SceneTransitionManager.instance).Field<string>("comingFromGameplayScene");
        gameplayScene.Value = packet.ComingFromGameplayScene;
        SceneTransitionManager.instance.TransitionFromNullToLevel(packet.Level);
        SceneTransitionManagerPatch.DisableTransitionHook = false;
    }

    private static void HandleBuildOrUpgrade(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (BuildOrUpgradePacket)ipacket;
        BuildSlotPatch.HandleUpgrade(packet.BuildingId, packet.Level, packet.Choice);
    }

    private static void HandleDayNight(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (DayNightPacket)ipacket;
        if (packet.Night)
        {
            NightCallPatch.TriggerNightFall();
        }
    }

    private static void HandleEnemySpawn(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (EnemySpawnPacket)ipacket;
        EnemySpawnerPatch.SpawnEnemy(packet.Wave, packet.Spawn, packet.Position, packet.Id, packet.Coins);
    }

    private static void HandleDamage(SteamNetworkingIdentity sender, IPacket ipacket)
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

    private static void HandleHeal(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (HealPacket)ipacket;
        HpPatch.Heal(packet.Target, packet.Amount);
    }

    private static void HandleScaleHp(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (ScaleHpPacket)ipacket;
        HpPatch.ScaleHp(packet.Target, packet.Multiplier);
    }

    private static void HandlePosition(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (PositionPacket)ipacket;
        var target = packet.Target.Get();
        if (target != null)
        {
            target.transform.position = packet.Position;
        }
    }

    private static void HandleRespawn(SteamNetworkingIdentity sender, IPacket ipacket)
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
                Plugin.Log.LogWarning($"Received unhandled respawn packet for {packet.Target.Type}:{packet.Target.Id}");
                break;
        }
        target.transform.position = packet.Position;
    }

    private static void HandleCommandAdd(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (CommandAddPacket)ipacket;
        var command = Plugin.Instance.PlayerManager.Get(packet.Player).Object.GetComponent<CommandUnits>();
        foreach (var unit in packet.Units)
        {
            var component = unit.Get()?.GetComponent<PathfindMovementPlayerunit>();
            if (component != null)
            {
                CommandUnitsPatch.AddUnit(command, component);
            }
        }
    }

    private static void HandleCommandPlace(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (CommandPlacePacket)ipacket;
        var command = Plugin.Instance.PlayerManager.Get(packet.Player).Object.GetComponent<CommandUnits>();
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

    private static void HandleCommandHoldPosition(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (CommandHoldPositionPacket)ipacket;
        var command = Plugin.Instance.PlayerManager.Get(packet.Player).Object.GetComponent<CommandUnits>();
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

    private static void HandleManualAttack(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (ManualAttackPacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player).Object;
        if (player == null)
        {
            return;
        }
        
        var attack = player.GetComponent<PlayerInteraction>().EquippedWeapon;
        attack.TryToAttack();
    }

    private static void HandleBalance(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (BalancePacket)ipacket;
        GlobalData.Internal.Balance += packet.Delta;
        var data = Plugin.Instance.PlayerManager.LocalPlayer.Data;
        if (packet.Delta > 0)
        {
            GlobalData.Internal.Networth += packet.Delta;
        }
        
        var player = data == null ? null : data.GetComponent<PlayerInteraction>();
        if (player == null)
        {
            return;
        }
        
        var action = packet.Delta > 0 ? player.onBalanceGain : player.onBalanceSpend;
        action.Invoke(Mathf.Abs(packet.Delta));
    }

    private static void HandleSpawnCoin(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (SpawnCoinPacket)ipacket;
        var player = Plugin.Instance.PlayerManager.Get(packet.Player).Object.GetComponent<PlayerInteraction>();
        Object.Instantiate(BuildSlotPatch.CoinPrefab, packet.Position, packet.Rotation)
            .GetComponent<Coin>().SetTarget(player);
    }

    private static void HandleApproval(SteamNetworkingIdentity sender, IPacket ipacket)
    {
        var packet = (ApprovalPacket)ipacket;
        if (Plugin.Instance.Network.Authenticate(packet.Password))
        {
            Plugin.Instance.Network.AddPlayer(sender);
        }
        else
        {
            // TODO: Send kick packet.
        }
    }
}