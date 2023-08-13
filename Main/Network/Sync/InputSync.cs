using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class InputSync : BaseTargetSync
{
    protected override bool ShouldUpdate => true;
    protected override bool HandleDisabledTargets => true;

    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        if (Plugin.Instance.Network.Server)
        {
            foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
            {
                yield return (
                    new IdentifierData{ Type = IdentifierType.Player, Id = player.Id },
                    player.Object
                );
            }
        }
        else if (Plugin.Instance.PlayerManager.LocalPlayer != null)
        {
            yield return (
                new IdentifierData{ Type = IdentifierType.Player, Id = Plugin.Instance.PlayerManager.LocalId },
                Plugin.Instance.PlayerManager.LocalPlayer.Object
            );
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target)
    {
        var packet = new SyncPlayerInputPacket { PlayerID = id.Id };
        packet.Data.Set(Plugin.Instance.PlayerManager.Get(id.Id).Shared);
        return packet;
    }

    protected override bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last)
    {
        var a = (SyncPlayerInputPacket)current;
        var b = (SyncPlayerInputPacket)last;
        return a.Data.Compare(b.Data);
    }

    protected override bool Filter(CSteamID peer, IdentifierData id, GameObject target)
    {
        return Plugin.Instance.PlayerManager.Get(id.Id).SteamID == peer;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncPlayerInputPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncPlayerInputPacket)packet;
        var player = Plugin.Instance.PlayerManager.Get(sync.PlayerID);
        if (player == null || player.Id == Plugin.Instance.PlayerManager.LocalId)
        {
            return;
        }
        
        player.Shared.Set(sync.Data);
    }
}