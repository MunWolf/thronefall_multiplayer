using System;
using System.Collections.Generic;
using ThronefallMP.NetworkPackets;
using ThronefallMP.Patches;

namespace ThronefallMP;

public enum PacketId
{
    BuildOrUpgradePacket,
    CommandAddPacket,
    CommandPlacePacket,
    CommandHoldPositionPacket,
    DamagePacket,
    DayNightPacket,
    EnemySpawnPacket,
    HealPacket,
    ManualAttack,
    PlayerListPacket,
    PlayerSyncPacket,
    PositionPacket,
    RespawnPacket,
    ScaleHpPacket,
    TransitionToScenePacket,
}

public static class PacketHandler
{
    private static readonly Dictionary<PacketId, Action<IPacket>> Handlers = new()
    {
        { BuildOrUpgradePacket.PacketID, HandleBuildOrUpgrade },
        { CommandAddPacket.PacketID, HandleCommandAdd },
        { CommandHoldPositionPacket.PacketID, HandleCommandHoldPosition },
        { CommandPlacePacket.PacketID, HandleCommandPlace },
        { DamagePacket.PacketID, HandleDamage },
        { DayNightPacket.PacketID, HandleDayNight },
        { EnemySpawnPacket.PacketID, HandleEnemySpawn },
        { HealPacket.PacketID, HandleHeal },
        { ManualAttackPacket.PacketID, HandleManualAttack },
        { PlayerListPacket.PacketID, HandlePlayerList },
        { PlayerSyncPacket.PacketID, HandlePlayerSync },
        { PositionPacket.PacketID, HandlePosition },
        { RespawnPacket.PacketID, HandleRespawn },
        { ScaleHpPacket.PacketID, HandleScaleHp },
        { TransitionToScenePacket.PacketID, HandleTransitionToScene },
    };

    public static void HandlePacket(IPacket packet)
    {
        var found = Handlers.TryGetValue(packet.TypeID(), out var handler);
        if (found)
        {
            handler(packet);
        }
        else
        {
            Plugin.Log.LogWarning($"No handler for packet {packet.TypeID()}.");
        }
    }

    private static void HandlePlayerList(IPacket ipacket)
    {
        var packet = (PlayerListPacket)ipacket;
        Plugin.Log.LogInfo("Received player list");
        foreach (var data in packet.Players)
        {
            if (Plugin.Instance.Network.GetPlayerData(data.Id) == null)
            {
                Plugin.Log.LogInfo($"Creating player {data.Id}");
                Plugin.Instance.Network.CreatePlayer(data.Id);
                var playerData = Plugin.Instance.Network.GetPlayerData(data.Id);
                playerData.SharedData.Position = data.Position;
                playerData.TeleportNext = true;
            }
            else
            {
                Plugin.Log.LogInfo($"Player {data.Id} exists");
                Plugin.Instance.Network.GetPlayerData(data.Id).SharedData.Position = data.Position;
            }
        }
    }

    private static void HandlePlayerSync(IPacket ipacket)
    {
        var packet = (PlayerSyncPacket)ipacket;
        var data = Plugin.Instance.Network.GetPlayerData(packet.PlayerID);
        if (data != null)
        {
            data.SharedData = packet.Data;
        }
    }

    private static void HandleTransitionToScene(IPacket ipacket)
    {
        var packet = (TransitionToScenePacket)ipacket;
        SceneTransitionManagerPatch.DisableTransitionHook = true;
        SceneTransitionManager.instance.TransitionFromLevelSelectToLevel(packet.Level);
        SceneTransitionManagerPatch.DisableTransitionHook = false;
    }

    private static void HandleBuildOrUpgrade(IPacket ipacket)
    {
        var packet = (BuildOrUpgradePacket)ipacket;
        BuildSlotPatch.HandleUpgrade(packet.BuildingId, packet.Level, packet.Choice);
    }

    private static void HandleDayNight(IPacket ipacket)
    {
        var packet = (DayNightPacket)ipacket;
        if (packet.Night)
        {
            NightCallPatch.TriggerNightFall();
        }
    }

    private static void HandleEnemySpawn(IPacket ipacket)
    {
        var packet = (EnemySpawnPacket)ipacket;
        EnemySpawnerPatch.SpawnEnemy(packet.Wave, packet.Spawn, packet.Position, packet.Id);
    }

    private static void HandleDamage(IPacket ipacket)
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

    private static void HandleHeal(IPacket ipacket)
    {
        var packet = (HealPacket)ipacket;
        HpPatch.Heal(packet.Target, packet.Amount);
    }

    private static void HandleScaleHp(IPacket ipacket)
    {
        var packet = (ScaleHpPacket)ipacket;
        HpPatch.ScaleHp(packet.Target, packet.Multiplier);
    }

    private static void HandlePosition(IPacket ipacket)
    {
        var packet = (PositionPacket)ipacket;
        var target = packet.Target.Get();
        if (target != null)
        {
            target.transform.position = packet.Position;
        }
    }

    private static void HandleRespawn(IPacket ipacket)
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

    private static void HandleCommandAdd(IPacket ipacket)
    {
        var packet = (CommandAddPacket)ipacket;
        var command = Plugin.Instance.Network.GetPlayerData(packet.Player).GetComponent<CommandUnits>();
        foreach (var unit in packet.Units)
        {
            var component = unit.Get()?.GetComponent<PathfindMovementPlayerunit>();
            if (component != null)
            {
                CommandUnitsPatch.AddUnit(command, component);
            }
        }
    }

    private static void HandleCommandPlace(IPacket ipacket)
    {
        var packet = (CommandPlacePacket)ipacket;
        var command = Plugin.Instance.Network.GetPlayerData(packet.Player).GetComponent<CommandUnits>();
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

    private static void HandleCommandHoldPosition(IPacket ipacket)
    {
        var packet = (CommandHoldPositionPacket)ipacket;
        var command = Plugin.Instance.Network.GetPlayerData(packet.Player).GetComponent<CommandUnits>();
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

    private static void HandleManualAttack(IPacket ipacket)
    {
        var packet = (ManualAttackPacket)ipacket;
        var player = Plugin.Instance.Network.GetPlayerData(packet.Player);
        if (player == null)
        {
            return;
        }
        
        var attack = player.GetComponent<PlayerInteraction>().EquippedWeapon;
        attack.TryToAttack();
    }
}