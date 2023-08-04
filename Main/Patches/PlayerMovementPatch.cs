using System.Collections;
using HarmonyLib;
using Pathfinding.RVO;
using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Patches;

static class PlayerMovementPatch
{
	private static readonly int Moving = Animator.StringToHash("Moving");
	private static readonly int Sprinting = Animator.StringToHash("Sprinting");

	public const float MaximumDeviance = 3.0f;
	public const float MaximumDevianceSquared = MaximumDeviance * MaximumDeviance;

	public static Vector3 SpawnLocation { get; private set; }
	
	public static void Apply()
	{
		On.PlayerMovement.Awake += Awake;
		On.PlayerMovement.Start += Start;
        On.PlayerMovement.Update += Update;
    }

	private static bool _firstInitialization = true;
	private static void Awake(On.PlayerMovement.orig_Awake original, PlayerMovement self)
	{
		original(self);
		
		var vanillaPlayer = self.gameObject.GetComponent<PlayerNetworkData>() == null;
		if (vanillaPlayer)
		{
			Plugin.Instance.Network.InitializeDefaultPlayer(self.gameObject);
		}
	}
	
	private static void Start(On.PlayerMovement.orig_Start original, PlayerMovement self)
	{
		original(self);
		
		var vanillaPlayer = self.gameObject.GetComponent<PlayerNetworkData>() == null;
		if (vanillaPlayer)
		{
			Plugin.Instance.StartCoroutine(ReinstanciatePlayers(self));
		}
	}

	private static IEnumerator ReinstanciatePlayers(PlayerMovement self)
	{
		yield return new WaitForEndOfFrame();
		SpawnLocation = self.transform.position;
		// First initialization happens when we enter level select or the tutorial for the first time.
		if (_firstInitialization)
		{
			// We need this otherwise we get a null value in Camera.Main when starting a level.
			yield return new WaitForEndOfFrame();
			Plugin.Instance.Network.Local();
			_firstInitialization = false;
		}
		else
		{
			Plugin.Instance.Network.ReinstanciatePlayers();
			if (Plugin.Instance.Network.Server && EnemySpawner.instance != null)
			{
				GlobalData.Balance = EnemySpawner.instance.goldBalanceAtStart;
			}
		}
	}
	
	private static void Update(On.PlayerMovement.orig_Update original, PlayerMovement self)
    {
	    var playerNetworkData = self.GetComponent<PlayerNetworkData>();
	    if (playerNetworkData == null)
	    {
		    return;
	    }
	    
        var input = Traverse.Create(self).Field<Rewired.Player>("input").Value;
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
		Vector2 zero = new Vector2(playerNetworkData.SharedData.MoveVertical, playerNetworkData.SharedData.MoveHorizontal);
		if (LocalGamestate.Instance.PlayerFrozen)
		{
			zero = Vector2.zero;
		}
		
		Vector3 normalized = Vector3.ProjectOnPlane(viewTransform.Value.forward, Vector3.up).normalized;
		Vector3 normalized2 = Vector3.ProjectOnPlane(viewTransform.Value.right, Vector3.up).normalized;
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
		if (controller.Value.enabled)
		{
			if (controller.Value.isGrounded)
			{
				yVelocity.Value = 0f;
			}
			else
			{
				yVelocity.Value += -9.81f * Time.deltaTime;
			}
			
			velocity.Value += Vector3.up * yVelocity.Value;

			if (!playerNetworkData.IsLocal)
			{
				var deltaPosition = playerNetworkData.SharedData.Position - self.transform.position;
				if (playerNetworkData.TeleportNext || deltaPosition.sqrMagnitude > MaximumDevianceSquared)
				{
					if (!playerNetworkData.TeleportNext)
					{
						Plugin.Log.LogInfo("MaximumDeviance reached");
					}
					
					Plugin.Log.LogInfo($"Teleporting {playerNetworkData.id} from {self.transform.position} to {playerNetworkData.SharedData.Position}");
					self.TeleportTo(playerNetworkData.SharedData.Position);
					playerNetworkData.TeleportNext = false;
				}
				else
				{
					velocity.Value = Vector3.Lerp(deltaPosition, velocity.Value, 0.5f);
					controller.Value.Move(Vector3.Lerp(deltaPosition, velocity.Value * Time.deltaTime, 0.5f));
				}
			}
			else if (playerNetworkData.TeleportNext)
			{
				Plugin.Log.LogInfo($"Teleporting {playerNetworkData.id} from {self.transform.position} to {playerNetworkData.SharedData.Position}");
				self.TeleportTo(playerNetworkData.SharedData.Position);
				playerNetworkData.TeleportNext = false;
			}
			else
			{
				controller.Value.Move(velocity.Value * Time.deltaTime);
				playerNetworkData.SharedData.Position = self.transform.position;
			}
		}
    }
}
