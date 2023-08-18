using System.Collections.Generic;
using HarmonyLib;
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
        public readonly float MinWeight;
        public readonly float MaxWeight;
        public readonly float MinDistance;
        public readonly float MaxDistance;

        public DevianceConstant(float minWeight, float maxWeight, float minDistance, float maxDistance)
        {
            MinWeight = minWeight;
            MaxWeight = maxWeight;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }
    }

    private const float TeleportDistance = 100f;
    private readonly Dictionary<IdentifierType, DevianceConstant> _devianceConstants = new()
    {
        { IdentifierType.Player, new DevianceConstant(0.05f, 0.4f, 15f, 30f) },
        { IdentifierType.Ally, new DevianceConstant(0.1f, 0.7f, 15f, 30f) },
        { IdentifierType.Enemy, new DevianceConstant(0.1f, 0.7f, 15f, 30f) }
    };

    protected override bool ShouldUpdate => true;

    private Vector3 CalculatePosition(IdentifierType type, float speed, Vector3 a, Vector3 b)
    {
        var distance = (a - b).magnitude;
        // We are far enough away that we teleported, return the actual location.
        if (distance >= TeleportDistance)
        {
            return b;
        }
        
        //var speedModifier = (speed - 10f) / 20f + 10f;
        
        // var ping = Plugin.Instance.PlayerManager.Get(id).Ping;
        // ping = ping < constant.MinPing ? 0 : ping - constant.MinPing;
        // var pingModifier = ping >= constant.PingDifference ? 1f : (float)ping / constant.PingDifference;
        // pingModifier = Mathf.Lerp(1f, constant.PingModifier, pingModifier);
        
        var constant = _devianceConstants[type];
        var maxDistance = constant.MaxDistance;// * speedModifier;
        var distanceWeight = (distance - constant.MinDistance) / (maxDistance - constant.MinDistance);
        distanceWeight = Mathf.Clamp(distanceWeight, 0f, 1f);

        var output = Vector3.Lerp(
            a,
            b,
            Mathf.Lerp(constant.MinWeight, constant.MaxWeight, distanceWeight)
        );
        return output;
    }
    
    protected override float ForceUpdateTimer => 0.5f;

    protected override IEnumerable<(IdentifierData id, GameObject target)> Targets()
    {
        if (Plugin.Instance.Network.Server)
        {
            foreach (var entry in _devianceConstants)
            {
                foreach (var data in Identifier.GetIdentifiers(entry.Key))
                {
                    yield return (new IdentifierData{ Type = entry.Key, Id = data.id}, data.target);
                }
            }
        }
        else if (Plugin.Instance.PlayerManager.LocalPlayer?.Object != null)
        {
            yield return (new IdentifierData
                {
                    Type = IdentifierType.Player,
                    Id = Plugin.Instance.PlayerManager.LocalId
                },
                Plugin.Instance.PlayerManager.LocalPlayer.Object
            );
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

    protected override bool Filter(CSteamID peer, IdentifierData id, GameObject target)
    {
        return id.Type == IdentifierType.Player && Plugin.Instance.PlayerManager.Get(id.Id).SteamID == peer;
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
            var player = target.GetComponent<PlayerNetworkData>();
            var movement = target.GetComponent<PlayerMovement>();
            var speed = movement.Sprinting ? movement.sprintSpeed : movement.speed;
            var heavyArmorEquipped = Traverse.Create(movement).Field<bool>("heavyArmorEquipped");
            var racingHorseEquipped = Traverse.Create(movement).Field<bool>("racingHorseEquipped");
            if (heavyArmorEquipped.Value)
            {
                speed *= PerkManager.instance.heavyArmor_SpeedMultiplyer;
            }
            if (racingHorseEquipped.Value)
            {
                speed *= PerkManager.instance.racingHorse_SpeedMultiplyer;
            }
            
            player.Player.Controller.enabled = false;
            target.transform.position = CalculatePosition(
                IdentifierType.Player,
                speed,
                target.transform.position,
                sync.Position
            );
            player.Player.Controller.enabled = true;
        }
        else
        {
            var speed = sync.Target.Type == IdentifierType.Enemy
                ? target.GetComponent<PathfindMovementEnemy>().movementSpeed
                : target.GetComponent<PathfindMovementPlayerunit>().movementSpeed;
            
            target.transform.position = CalculatePosition(
                sync.Target.Type,
                speed,
                target.transform.position,
                sync.Position
            );
        }
    }
}