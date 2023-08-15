using System;
using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using ThronefallMP.Patches;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class HpSync : BaseTargetSync
{
    protected override bool HandleDisabledTargets => true;
    protected override float ForceUpdateTimer => 3f;

    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Player))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Player, Id = data.id}, data.target);
        }
        
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Building))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Building, Id = data.id}, data.target);
        }
        
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Ally))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Ally, Id = data.id}, data.target);
        }
        
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Enemy))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Enemy, Id = data.id}, data.target);
        }
        
        foreach (var data in Identifier.GetIdentifiers(IdentifierType.Enemy))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Enemy, Id = data.id}, data.target);
        }
        
        foreach (var data in Identifier.GetDestroyed(IdentifierType.Enemy))
        {
            yield return (new IdentifierData{ Type = IdentifierType.Enemy, Id = data}, null);
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target)
    {
        if (target == null)
        {
            // Max hp doesn't matter as this kills the unit.
            return new SyncHpPacket
            {
                Target = id,
                Hp = int.MinValue,
                MaxHp = 10,
                KnockedOut = true
            };
        }
        
        var hp = target.GetComponent<Hp>();
        return new SyncHpPacket
        {
            Target = id,
            Hp = hp.HpValue,
            MaxHp = hp.maxHp,
            KnockedOut = hp.KnockedOut
        };
    }

    protected override bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last)
    {
        var a = (SyncHpPacket)current;
        var b = (SyncHpPacket)last;
        return Math.Abs(a.Hp - b.Hp) < Helpers.Epsilon
            && Math.Abs(a.MaxHp - b.MaxHp) < Helpers.Epsilon
            && a.KnockedOut == b.KnockedOut;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID is SyncHpPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncHpPacket)packet;
        var target = sync.Target.Get();
        if (target == null)
        {
            return;
        }

        var hp = target.GetComponent<Hp>();
        if (hp.TaggedObj == null)
        {
            return;
        }
        
        Plugin.Log.LogInfoFiltered("HpSync", $"Setting hp to {sync.Hp}/{sync.MaxHp} : Knocked out = {sync.KnockedOut}");
        hp.maxHp = sync.MaxHp;
        var difference = sync.Hp - hp.HpValue;
        HpPatch.AllowHealthChangeOnClient = true;
        if (difference > 0f)
        {
            // If we are not active then don't revive/heal.
            if (!hp.gameObject.activeInHierarchy) {}
            else if (hp.KnockedOut && !sync.KnockedOut)
            {
                hp.Revive(true, sync.Hp / hp.maxHp);
                if (sync.Target.Type == IdentifierType.Ally)
                {
                    var component = hp.GetComponent<PathfindMovementPlayerunit>();
                    component.SnapToNavmesh();
                }
            }
            else if (!sync.KnockedOut)
            {
                hp.Heal(difference);
            }
        }
        else
        {
            // _damageComingFrom does not matter as it calls a pathfinding function that is only handled on the server.
            // causedByPlayer and invokeFeedbackEvents handled by DamageFeedbackPacket
            hp.TakeDamage(-difference, invokeFeedbackEvents: false);
        }

        HpPatch.AllowHealthChangeOnClient = false;
    }
}