using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Administration;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.PlayerCommand;
using ThronefallMP.Network.Sync;
using ThronefallMP.Patches;
using ThronefallMP.UI;

namespace ThronefallMP.Network;

public enum PacketId
{
    // These 3 will always have the same packetid between versions.
    Approval,
    Disconnect,
    PeerSync,
    
    SyncPing,
    SyncPong,
    SyncPingInfo,
    SyncLevelData,
    SyncResource,
    SyncPlayer,
    SyncPlayerInput,
    SyncAllyPathfinder,
    SyncEnemyPathfinder,
    SyncPosition,
    SyncHp,
    
    DayNight,
    EnemySpawn,
    DamageFeedback,
    RequestLevel,
    WeaponRequest,
    WeaponResponse,
    
    BuildOrUpgrade,
    CancelBuild,
    CommandAdd,
    CommandHoldPosition,
    CommandPlace,
    ConfirmBuild,
    ManualAttack,
}

public static class PacketHandler
{
    public static bool AwaitingConnectionApproval;
    
    private static readonly Dictionary<PacketId, Action<SteamNetworkingIdentity, BasePacket>> Handlers = new()
    {
        { ApprovalPacket.PacketID, HandleApproval },
        { DisconnectPacket.PacketID, HandleDisconnect },
        { PeerListPacket.PacketID, HandlePeerList },
        
        { DamageFeedbackPacket.PacketID, HandleDamageFeedback },
        { DayNightPacket.PacketID, HandleDayNight },
        { EnemySpawnPacket.PacketID, HandleEnemySpawn },
        
        { BuildOrUpgradePacket.PacketID, HandleBuildOrUpgrade },
        { CancelBuildPacket.PacketID, HandleCancelBuild },
        { CommandAddPacket.PacketID, HandleCommandAdd },
        { CommandPlacePacket.PacketID, HandleCommandPlace },
        { CommandHoldPositionPacket.PacketID, HandleCommandHoldPosition },
        { ConfirmBuildPacket.PacketID, HandleConfirmBuild },
        { ManualAttackPacket.PacketID, HandleManualAttack },
    };

    public static void HandlePacket(SteamNetworkingIdentity sender, BasePacket packet)
    {
        if (SyncManager.HandlePacket(sender, packet))
        {
            Plugin.Log.LogDebugFiltered("PacketHandler", $"Packet {packet.TypeID} handled by sync");
            return;
        }
        
        var found = Handlers.TryGetValue(packet.TypeID, out var handler);
        if (found)
        {
            Plugin.Log.LogDebugFiltered("PacketHandler", $"Handling {packet.TypeID} packet");
            handler(sender, packet);
        }
        else
        {
            Plugin.Log.LogWarningFiltered("PacketHandler", $"No handler for packet {packet.TypeID}.");
        }
    }

    private static void HandlePeerList(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (PeerListPacket)ipacket;

        if (AwaitingConnectionApproval)
        {
            // Currently we only allow joining a lobby if we are in level select.
            SceneTransitionManager.instance.TransitionFromNullToLevelSelect();
            UIManager.LobbyListPanel.CloseConnectingDialog();
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
            
            var player = Plugin.Instance.PlayerManager.CreateOrGet(data.SteamId, data.Id);
            player.SpawnID = data.SpawnId;
            if (player.Object == null && !Plugin.Instance.PlayerManager.InstantiatePlayer(player, data.Position))
            {
                continue;
            }
            
            player.Controller.enabled = false;
            player.Object.transform.position = data.Position;
            player.Controller.enabled = true;
        }
    }

    private static void HandleDayNight(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (DayNightPacket)ipacket;
        if (packet.Timestate == DayNightCycle.Instance.CurrentTimestate)
        {
            return;
        }
        
        if (Plugin.Instance.Network.Server && sender.GetSteamID() != Network.SteamId)
        {
            Plugin.Instance.Network.Send(packet);
        }
        
        if (packet.Timestate == DayNightCycle.Timestate.Night)
        {
            NightCallPatch.TriggerNightFall();
        }
        else
        {
            if (TagManager.instance != null && EnemySpawner.instance != null)
            {
                EnemySpawner.instance.StopSpawnAfterWaveAndReset();
                foreach (var enemy in Identifier.GetGameObjects(IdentifierType.Enemy))
                {
                    HpPatch.AllowHealthChangeOnClient = true;
                    enemy.GetComponent<Hp>().TakeDamage(9999, null, true);
                    HpPatch.AllowHealthChangeOnClient = false;
                }
            }
            
            Traverse.Create(DayNightCycle.Instance).Field<float>("currentNightLength").Value = packet.NightLength;
            var switchToDayCoroutine = Traverse.Create(DayNightCycle.Instance).Method("SwitchToDayCoroutine");
            DayNightCycle.Instance.StartCoroutine(switchToDayCoroutine.GetValue<IEnumerator>());
        }
    }

    private static void HandleEnemySpawn(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (EnemySpawnPacket)ipacket;
        EnemySpawnerPatch.SpawnEnemy(packet.Wave, packet.Spawn, packet.Position, packet.Id, packet.Coins);
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
        if (player?.Object == null)
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
            
        UIManager.LobbyListPanel.CloseConnectingDialog();
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
        if (GlobalData.Balance < info.Cost || info.CurrentLevel > packet.Level)
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
            GlobalData.LocalBalanceDelta = 0;
        }
        else
        {
            Plugin.Log.LogInfoFiltered("PacketHandler", $"Building {packet.BuildingId}:{packet.Level}:{packet.Choice}");
        }
        
        HpPatch.AllowHealthChangeOnClient = true;
        BuildSlotPatch.HandleUpgrade(
            packet.PlayerID,
            packet.BuildingId,
            packet.Level,
            packet.Choice
        );
        HpPatch.AllowHealthChangeOnClient = false;
    }

    private static void HandleDamageFeedback(SteamNetworkingIdentity sender, BasePacket ipacket)
    {
        var packet = (DamageFeedbackPacket)ipacket;
        var target = packet.Target.Get();
        if (target == null)
        {
            return;
        }

        var hp = target.GetComponent<Hp>();
        hp.OnReceiveDamage?.Invoke(packet.CausedByPlayer);
    }
}