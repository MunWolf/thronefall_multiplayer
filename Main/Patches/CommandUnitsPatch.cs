using System.Collections.Generic;
using System.Data.Common;
using HarmonyLib;
using Pathfinding;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using ThronefallMP.Network.Packets.PlayerCommand;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class CommandUnitsPatch
{
    public static void Apply()
    {
        On.CommandUnits.Update += Update;
        On.CommandUnits.PlaceCommandedUnitsAndCalculateTargetPositions += PlaceCommandedUnitsAndCalculateTargetPositions;
        On.CommandUnits.MakeUnitsInBufferHoldPosition += MakeUnitsInBufferHoldPosition;
    }

    private static void Update(On.CommandUnits.orig_Update original, CommandUnits self)
    {
        var units = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommanding");
        var data = self.GetComponent<PlayerNetworkData>();
        if (data == null)
        {
            return;
        }
        
        if (self.commanding)
        {
            HandleCommanding(self, data);
        }
        else
        {
            HandleNotCommanding(self, units.Value, data);
        }
        
        for (var i = units.Value.Count - 1; i >= 0; i--)
        {
            var unit = units.Value[i];
            unit.HomePosition = self.transform.position;
            if (!unit.enabled)
            {
                units.Value.RemoveAt(i);
                self.OnUnitRemove(unit);
            }
        }
        
        self.commandingIndicator.SetActive(units.Value.Count > 0 && !data.SharedData.CommandUnitsButton);
        if (!data.SharedData.CommandUnitsButton && self.rangeIndicator.Active)
        {
            self.rangeIndicator.Deactivate();
        }
    }

    private static void HandleNotCommanding(CommandUnits self, List<PathfindMovementPlayerunit> commanding, PlayerNetworkData data)
    {
        var tagManager = Traverse.Create(self).Field<TagManager>("tagManager");
        var hpPlayer = Traverse.Create(self).Field<Hp>("hpPlayer");
        if (data.SharedData.CommandUnitsButton && !data.CommandUnitsButtonLast && !self.rangeIndicator.Active)
        {
            self.rangeIndicator.Activate();
        }
        
        var units = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommanding");
        if (data.SharedData.CommandUnitsButton && hpPlayer.Value.HpValue > 0f)
        {
            if (!Plugin.Instance.Network.Server)
            {
                return;
            }
            
            var toAdd = new List<IdentifierData>();
            foreach (var taggedObject in TagManager.instance.PlayerUnits)
            {
                if (!(tagManager.Value.MeasureDistanceToTaggedObject(taggedObject, self.transform.position) <= self.attractRange))
                {
                    continue;
                }
                
                var unit = taggedObject.GetComponent<PathfindMovementPlayerunit>();
                var followingPlayer = Traverse.Create(unit).Field<bool>("followingPlayer");
                if (!followingPlayer.Value && !units.Value.Contains(unit))
                {
                    toAdd.Add(new IdentifierData(unit.GetComponent<Identifier>()));
                }
            }

            if (toAdd.Count > 0)
            {
                var packet = new CommandAddPacket
                {
                    Player = data.id,
                    Units = toAdd
                };

                Plugin.Instance.Network.Send(packet, true);
            }
        }
        else if (units.Value.Count > 0)
        {
            self.commanding = true;
        }
    }

    private static void HandleCommanding(CommandUnits self, PlayerNetworkData data)
    {
        var hpPlayer = Traverse.Create(self).Field<Hp>("hpPlayer");
        var timeSincePlace = Traverse.Create(self).Field<float>("timeSincePlace");
        var switchedToHold = Traverse.Create(self).Field<bool>("switchedToHold");
        if (data.SharedData.CommandUnitsButton && !data.CommandUnitsButtonLast || hpPlayer.Value.HpValue <= 0f)
        {
            self.PlaceCommandedUnitsAndCalculateTargetPositions();
            timeSincePlace.Value = 0f;
            switchedToHold.Value = false;
        }
        
        if (data.SharedData.CommandUnitsButton && hpPlayer.Value.HpValue > 0f)
        {
            // Maybe need to explicitly handle this by sending a hold position packet?
            timeSincePlace.Value += Time.deltaTime;
            if (timeSincePlace.Value > self.holdToHoldPositionTime && !switchedToHold.Value)
            {
                switchedToHold.Value = true;
                self.MakeUnitsInBufferHoldPosition();
            }
        }
        
        if ((!data.SharedData.CommandUnitsButton && data.CommandUnitsButtonLast) || hpPlayer.Value.HpValue <= 0f)
        {
            self.commanding = false;
            timeSincePlace.Value = 0f;
        }
        
        data.CommandUnitsButtonLast = data.SharedData.CommandUnitsButton;
    }

    private static void PlaceCommandedUnitsAndCalculateTargetPositions(On.CommandUnits.orig_PlaceCommandedUnitsAndCalculateTargetPositions orig, CommandUnits self)
    {
        if (!Plugin.Instance.Network.Server || !self.commanding)
        {
            return;
        }
        
        var units = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommanding");
        var astarPath = Traverse.Create(self).Field<AstarPath>("astarPath");
        var nearestConstraint = Traverse.Create(self).Field<NNConstraint>("nearestConstraint");
	    for (var i = 0; i < units.Value.Count; i++)
	    {
		    var vector = 
                Quaternion.AngleAxis(360f * i / units.Value.Count, Vector3.up) *
                Vector3.right * self.unitDistanceMoveStep;
            var randomVector = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);
            units.Value[i].HomePosition = astarPath.Value.GetNearest(
                self.transform.position + vector + randomVector * self.unitDistanceMoveStep * 0.1f,
                nearestConstraint.Value
            ).position;
	    }
        
	    for (var j = 0; j < self.maxPositioningRepeats; j++)
	    {
		    var flag = false;
		    for (var k = 0; k < units.Value.Count; k++)
		    {
			    for (var l = k + 1; l < units.Value.Count; l++)
                {
                    var difference =
                        units.Value[k].HomePosition -
                        units.Value[l].HomePosition;
                    
                    if (!(difference.sqrMagnitude <= (self.unitDistanceFromEachOther * self.unitDistanceFromEachOther)))
                    {
                        continue;
                    }
                    
                    var vector2 = (units.Value[k].HomePosition - units.Value[l].HomePosition).normalized * self.unitDistanceMoveStep;
                    units.Value[k].HomePosition = astarPath.Value.GetNearest(
                        units.Value[k].HomePosition + vector2, nearestConstraint.Value).position;
                    units.Value[l].HomePosition = astarPath.Value.GetNearest(
                        units.Value[l].HomePosition - vector2, nearestConstraint.Value).position;
                    flag = true;
                }
		    }
            
		    if (!flag)
		    {
			    break;
		    }
	    }

        var unitBuffer = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommandingBuffer");
        unitBuffer.Value.Clear();
        unitBuffer.Value.AddRange(units.Value);
        
        var id = self.GetComponent<Identifier>();
        var packet = new CommandPlacePacket { Player = id.Id };
        foreach (var unit in units.Value)
        {
            packet.Units.Add(new CommandPlacePacket.UnitData
            {
                Unit = new IdentifierData(unit.GetComponent<Identifier>()),
                Home = unit.HomePosition
            });
        }
        
        Plugin.Instance.Network.Send(packet, true);
    }

    private static void MakeUnitsInBufferHoldPosition(On.CommandUnits.orig_MakeUnitsInBufferHoldPosition orig, CommandUnits self)
    {
        if (!Plugin.Instance.Network.Server)
        {
            return;
        }

        var packet = new CommandHoldPositionPacket();
        var unitBuffer = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommandingBuffer");
        foreach (var unit in unitBuffer.Value)
        {
            packet.Units.Add(
                new CommandHoldPositionPacket.UnitData
                {
                    Unit = new IdentifierData(unit.GetComponent<Identifier>()),
                    Home = unit.HomePosition
                }
            );
        }
        
        Plugin.Instance.Network.Send(packet, true);
    }

    public static void EmitWaypoint(CommandUnits self, bool playSound)
    {
        var audioManager = Traverse.Create(self).Field<ThronefallAudioManager>("audioManager");
        if (playSound)
        {
            var audioSet = Traverse.Create(self).Field<AudioSet>("audioSet");
            audioManager.Value.PlaySoundAsOneShot(
                audioSet.Value.PlaceCommandingUnits,
                0.35f,
                0.9f + Random.value * 0.2f,
                audioManager.Value.mgSFX,
                10
            );
        }
        
        self.dropWaypointFx.Emit(self.drowWaypointParticleCount);
    }
    
    public static void PlaceUnit(CommandUnits self, PathfindMovementPlayerunit unit, Vector3 home)
    {
        self.OnUnitRemove(unit);
        foreach (var autoAttack in unit.GetComponents<AutoAttack>())
        {
            if (autoAttack.GetComponent<Hp>().HpValue > 0f)
            {
                autoAttack.enabled = true;
            }
        }

        unit.HomePosition = home;
        var units = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommanding");
        units.Value.Remove(unit);
    }

    public static void PlayHoldSound(CommandUnits self)
    {
        var audioManager = Traverse.Create(self).Field<ThronefallAudioManager>("audioManager");
        var audioSet = Traverse.Create(self).Field<AudioSet>("audioSet");
        audioManager.Value.PlaySoundAsOneShot(
            audioSet.Value.HoldPosition,
            0.45f,
            0.9f + Random.value * 0.2f,
            audioManager.Value.mgSFX,
            10
        );
    }

    public static void HoldPosition(PathfindMovementPlayerunit unit, Vector3 home)
    {
        unit.HomePosition = home;
        unit.HoldPosition = true;
    }

    public static void AddUnit(CommandUnits self, PathfindMovementPlayerunit unit)
    {
        var audioManager = Traverse.Create(self).Field<ThronefallAudioManager>("audioManager");
        var audioSet = Traverse.Create(self).Field<AudioSet>("audioSet");
        var units = Traverse.Create(self).Field<List<PathfindMovementPlayerunit>>("playerUnitsCommanding");
        var upgradeManager = Traverse.Create(self).Field<PlayerUpgradeManager>("playerUPgradeManager");
        if (units.Value.Contains(unit))
        {
            return;
        }
        
        audioManager.Value.PlaySoundAsOneShot(
            audioSet.Value.AddedUnitToCommanding,
            0.55f,
            0.7f + units.Value.Count * 0.025f,
            audioManager.Value.mgSFX,
            50
        );
        
        units.Value.Add(unit);
        unit.FollowPlayer(true);
        var componentInChildren = unit.GetComponentInChildren<MaterialFlasherFX>();
        if (componentInChildren)
        {
            componentInChildren.SetSelected(true);
        }
        unit.GetComponent<TaggedObject>().Tags.Add(TagManager.ETag.AUTO_Commanded);
        
        if (upgradeManager.Value.commander)
        {
             unit.movementSpeed *= UpgradeCommander.instance.moveSpeedMultiplicator;
             return;
        }
        
        foreach (var autoAttack in unit.GetComponents<AutoAttack>())
        {
            autoAttack.enabled = false;
        }
    }
}