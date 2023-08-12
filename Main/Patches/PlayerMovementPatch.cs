using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Pathfinding.RVO;
using Steamworks;
using ThronefallMP.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThronefallMP.Patches;

static class PlayerMovementPatch
{
	private static readonly int Moving = Animator.StringToHash("Moving");
	private static readonly int Sprinting = Animator.StringToHash("Sprinting");

	private const float MaximumDevianceMin = 1.5f;
	private const float MaximumDevianceMax = 4.0f;
	private const int MinPing = 100;
	private const int MaxPing = 500;
	private const int DifferencePing = MaxPing - MinPing;
	public static float MaximumDevianceSquared(CSteamID id)
	{
		var ping = Plugin.Instance.Network.Ping(id);
		ping = Mathf.Clamp(ping - MinPing, 0, DifferencePing);
		var deviance = Mathf.Lerp(MaximumDevianceMin, MaximumDevianceMax, (float)ping / DifferencePing);
		return deviance * deviance;
	}
	
	public static void Apply()
	{
		On.PlayerMovement.Awake += Awake;
		On.PlayerMovement.Start += Start;
        On.PlayerMovement.Update += Update;
        On.CameraRig.Start += Start;
	}

	// Noop because we do this work in PlayerMovement.Awake instead
	private static void Start(On.CameraRig.orig_Start original, CameraRig self) {}

	private static void Awake(On.PlayerMovement.orig_Awake original, PlayerMovement self)
	{
		var vanillaPlayer = self.GetComponent<PlayerNetworkData>() == null;
		if (!vanillaPlayer)
		{
			return;
		}
		
		var rig = self.GetComponentInChildren<CameraRig>();
		Traverse.Create(rig).Field<Quaternion>("startRotation").Value = rig.transform.rotation;
		Traverse.Create(rig).Field<Transform>("cameraTarget").Value = rig.transform.parent;
		rig.transform.SetParent(null);
		
		Plugin.Instance.PlayerManager.SetPrefab(self.gameObject);
		self.enabled = false;
		Object.Destroy(self.gameObject);
	}
	
	
	private static void Start(On.PlayerMovement.orig_Start original, PlayerMovement self)
	{
		var vanillaPlayer = self.gameObject.GetComponent<PlayerNetworkData>() == null;
		if (!vanillaPlayer)
		{
			original(self);
		}
	}

	private static void Update(On.PlayerMovement.orig_Update original, PlayerMovement self)
    {
	    var playerNetworkData = self.GetComponent<PlayerNetworkData>();
	    if (playerNetworkData == null)
	    {
		    return;
	    }
	    
        var hp = Traverse.Create(self).Field<Hp>("hp").Value;
        var rvoController = Traverse.Create(self).Field<RVOController>("rvoController").Value;
        var heavyArmorEquipped = Traverse.Create(self).Field<bool>("heavyArmorEquipped").Value;
        var racingHorseEquipped = Traverse.Create(self).Field<bool>("racingHorseEquipped").Value;
        
        var velocity = Traverse.Create(self).Field<Vector3>("velocity");
        var yVelocity = Traverse.Create(self).Field<float>("yVelocity");
        var viewTransform = Traverse.Create(self).Field<Transform>("viewTransform");
        var sprintingToggledOn = Traverse.Create(self).Field<bool>("sprintingToggledOn");
        var sprinting = Traverse.Create(self).Field<bool>("sprinting");
        var moving = Traverse.Create(self).Field<bool>("moving");
        var desiredMeshRotation = Traverse.Create(self).Field<Quaternion>("desiredMeshRotation");
        var controller = Traverse.Create(self).Field<CharacterController>("controller");

        if (viewTransform.Value == null)
        {
	        return;
        }
        
        // Normal code
		var zero = new Vector2(playerNetworkData.SharedData.MoveVertical, playerNetworkData.SharedData.MoveHorizontal);
		if (LocalGamestate.Instance.PlayerFrozen)
		{
			zero = Vector2.zero;
		}
		
		var normalized = Vector3.ProjectOnPlane(viewTransform.Value.forward, Vector3.up).normalized;
		var normalized2 = Vector3.ProjectOnPlane(viewTransform.Value.right, Vector3.up).normalized;
		velocity.Value = Vector3.zero;
		velocity.Value += normalized * zero.x;
		velocity.Value += normalized2 * zero.y;
		velocity.Value = Vector3.ClampMagnitude(velocity.Value, 1f);
		var shouldToggleSprint = playerNetworkData.SharedData.SprintToggleButton && !playerNetworkData.PlayerMovementSprintLast;
		playerNetworkData.PlayerMovementSprintLast = playerNetworkData.SharedData.SprintToggleButton;
		if (shouldToggleSprint)
		{
			sprintingToggledOn.Value = !sprintingToggledOn.Value;
		}
		if (sprintingToggledOn.Value && playerNetworkData.SharedData.SprintButton)
		{
			sprintingToggledOn.Value = false;
		}
		sprinting.Value = (playerNetworkData.SharedData.SprintButton || sprintingToggledOn.Value) && hp.HpPercentage >= 1f;
		velocity.Value *= (sprinting.Value ? self.sprintSpeed : self.speed);
		if (heavyArmorEquipped && DayNightCycle.Instance.CurrentTimestate == DayNightCycle.Timestate.Night)
		{
			velocity.Value *= PerkManager.instance.heavyArmor_SpeedMultiplyer;
		}
		if (racingHorseEquipped)
		{
			velocity.Value *= PerkManager.instance.racingHorse_SpeedMultiplyer;
		}
		rvoController.velocity = velocity.Value;
		moving.Value = velocity.Value.sqrMagnitude > 0.1f;
		if (moving.Value)
		{
			desiredMeshRotation.Value = Quaternion.LookRotation(velocity.Value.normalized, Vector3.up);
		}
		if (desiredMeshRotation.Value != self.meshParent.rotation)
		{
			self.meshParent.rotation = Quaternion.RotateTowards(self.meshParent.rotation, desiredMeshRotation.Value, self.maxMeshRotationSpeed * Time.deltaTime);
		}
		
		self.meshAnimator.SetBool(Moving, moving.Value);
		self.meshAnimator.SetBool(Sprinting, sprinting.Value);
		if (!controller.Value.enabled)
		{
			return;
		}
		
		if (controller.Value.isGrounded)
		{
			yVelocity.Value = 0f;
		}
		else
		{
			yVelocity.Value += -9.81f * Time.deltaTime;
		}
			
		velocity.Value += Vector3.up * yVelocity.Value;
		controller.Value.Move(velocity.Value * Time.deltaTime);

		var yFallThroughMapDetection = Traverse.Create(self).Field<float>("yFallThroughMapDetection");
		if (!(self.transform.position.y < yFallThroughMapDetection.Value))
		{
			return;
		}
				
		velocity.Value = Vector3.zero;
		yVelocity.Value = 0f;
		self.TeleportTo(
			Utils.GetSpawnLocation(
				Plugin.Instance.PlayerManager.SpawnLocation,
				playerNetworkData.Player.SpawnID
			)
		);
    }
}
