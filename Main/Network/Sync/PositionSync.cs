using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public class PositionSync : BaseTargetSync
{
    private class DevianceConstant
    {
        public readonly float MaximumDevianceMin;
        public readonly float MaximumDevianceMax;
        public readonly uint MinPing;
        public readonly uint DifferencePing;

        public DevianceConstant(float minDeviance, float maxDeviance, uint minPing, uint maxPing)
        {
            MaximumDevianceMin = minDeviance;
            MaximumDevianceMax = maxDeviance;
            MinPing = minPing;
            DifferencePing = maxPing - MinPing;
        }
    }

    // Maybe we need to take into account the speed of the units as well? 
    private readonly Dictionary<IdentifierType, DevianceConstant> _devianceConstants = new()
    {
        { IdentifierType.Player, new DevianceConstant(1.5f, 4.0f, 100, 500) },
        { IdentifierType.Ally, new DevianceConstant(1.0f, 2.5f, 100, 500) },
        { IdentifierType.Enemy, new DevianceConstant(1.0f, 2.5f, 100, 500) }
    };
    
    private float MaximumDevianceSquared(IdentifierType type, CSteamID id)
    {
        var constant = _devianceConstants[type];
        var ping = Plugin.Instance.PlayerManager.Get(id).Ping;
        ping = ping < constant.MinPing ? 0 : ping - constant.MinPing;
        ping = ping > constant.DifferencePing ? constant.DifferencePing : ping;
        var deviance = Mathf.Lerp(
            constant.MaximumDevianceMin,
            constant.MaximumDevianceMax,
            (float)ping / constant.DifferencePing
        );
        
        return deviance * deviance;
    }
    
    //protected override float ForceUpdateTimer => 2f;

    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        foreach (var entry in _devianceConstants)
        {
            foreach (var data in Identifier.GetIdentifiers(entry.Key))
            {
                yield return (new IdentifierData{ Type = entry.Key, Id = data.id}, data.target);
            }
        }
    }

    protected override BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target)
    {
        return new SyncPositionPacket
        {
            Target = id,
            Position = target.transform.position
        };
    }

    protected override bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last)
    {
        var a = (SyncPositionPacket)current;
        var b = (SyncPositionPacket)last;
        return (a.Position - b.Position).sqrMagnitude < Helpers.EpsilonSqr;
    }

    public override bool CanHandle(BasePacket packet)
    {
        return packet.TypeID == SyncPositionPacket.PacketID;
    }

    public override void Handle(CSteamID peer, BasePacket packet)
    {
        var sync = (SyncPositionPacket)packet;
        var target = sync.Target.Get();
        if (target == null)
        {
            return;
        }

        if (sync.Target.Type == IdentifierType.Player)
        {
            // If we aren't moving then we should always stay where we are.
            var player = target.GetComponent<PlayerNetworkData>();
            if (player.SharedData.MoveHorizontal > 0.01f || player.SharedData.MoveVertical > 0.01f)
            {
                var deltaPosition = sync.Position - target.transform.position;
                if (deltaPosition.sqrMagnitude < MaximumDevianceSquared(sync.Target.Type, player.Player.SteamID))
                {
                    return;
                }
            }
            
            player.Player.Controller.enabled = false;
            player.transform.position = sync.Position;
            player.Player.Controller.enabled = true;
        }
        else
        {
            var deltaPosition = sync.Position - target.transform.position;
            if (deltaPosition.sqrMagnitude < MaximumDevianceSquared(sync.Target.Type, Plugin.Instance.Network.Owner))
            {
                return;
            }
            
            target.transform.position = sync.Position;
        }
    }
}