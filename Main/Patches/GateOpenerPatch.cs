using HarmonyLib;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class GateOpenerPatch
{
    public static void Apply()
    {
        On.GateOpener.Update += Update;
    }

    private static void Update(On.GateOpener.orig_Update original, GateOpener self)
    {
		if (!TagManager.instance)
		{
			return;
		}
		
		var barsInitPosition = Traverse.Create(self).Field<Vector3>("barsInitPosition");
		var doorLInitRotation = Traverse.Create(self).Field<Vector3>("doorLInitRotation");
		var doorRInitRotation = Traverse.Create(self).Field<Vector3>("doorRInitRotation");
		var open = Traverse.Create(self).Field<bool>("open");
		var openAnimationClock = Traverse.Create(self).Field<float>("openAnimationClock");
		//var players = Traverse.Create(self).Field<IReadOnlyList<TaggedObject>>("players");
		//var playerUnits = Traverse.Create(self).Field<IReadOnlyList<TaggedObject>>("playerUnits");
		//players.Value = TagManager.instance.Players;
		//playerUnits.Value = TagManager.instance.PlayerUnits;
		//original(self);

		var openMethod = Traverse.Create(self).Method("Open");
		var closeMethod = Traverse.Create(self).Method("Close");
		
		if (open.Value)
		{
			var flag = true;
			foreach (var unit in TagManager.instance.PlayerUnits)
			{
				if (!unit.gameObject.activeInHierarchy ||
				    !(Vector3.Distance(self.transform.position, unit.transform.position) <= self.clearDistance) ||
				    !unit.Tags.Contains(TagManager.ETag.AUTO_Alive) ||
				    !(Vector3.Distance(unit.transform.position, unit.GetComponent<PathfindMovementPlayerunit>().HomePosition) >= 0.5f))
				{
					continue;
				}
				
				flag = false;
				break;
			}
			
			foreach (var player in TagManager.instance.Players)
			{
				if (player.gameObject.activeInHierarchy && Vector3.Distance(self.transform.position, player.transform.position) <= self.openDistance && player.Tags.Contains(TagManager.ETag.AUTO_Alive))
				{
					flag = false;
				}
			}
			
			if (flag)
			{
				closeMethod.GetValue();
			}
		}
		else
		{
			foreach (var unit in TagManager.instance.PlayerUnits)
			{
				if (!unit.gameObject.activeInHierarchy ||
				    !(Vector3.Distance(self.transform.position, unit.transform.position) <= self.openDistance) ||
				    !unit.Tags.Contains(TagManager.ETag.AUTO_Alive) ||
				    !(Vector3.Distance(unit.transform.position, unit.GetComponent<PathfindMovementPlayerunit>().HomePosition) >= 0.5f))
				{
					continue;
				}
				
				openMethod.GetValue();
				return;
			}
			
			foreach (var player in TagManager.instance.Players)
			{
				if (player.gameObject.activeInHierarchy && Vector3.Distance(self.transform.position, player.transform.position) <= self.openDistance && player.Tags.Contains(TagManager.ETag.AUTO_Alive))
				{
					openMethod.GetValue();
				}
			}
		}
		
		switch (open.Value)
		{
			case true when openAnimationClock.Value < self.animationTime:
			{
				openAnimationClock.Value += Time.deltaTime;
				if (openAnimationClock.Value > self.animationTime)
				{
					openAnimationClock.Value = self.animationTime;
				}

				break;
			}
			case false when openAnimationClock.Value > 0f:
			{
				openAnimationClock.Value -= Time.deltaTime;
				if (openAnimationClock.Value < 0f)
				{
					openAnimationClock.Value = 0f;
				}

				break;
			}
		}
		
		var mode = self.mode;
		switch (mode)
		{
			case GateOpener.Mode.Door:
				var num = Mathf.SmoothStep(0f, self.maxAngle, openAnimationClock.Value / self.animationTime);
				self.doorL.rotation = Quaternion.Euler(doorLInitRotation.Value + Vector3.forward * num);
				self.doorR.rotation = Quaternion.Euler(doorRInitRotation.Value + Vector3.forward * -num);
				break;
			case GateOpener.Mode.Bars:
				self.bars.transform.position = Vector3.Slerp(
					barsInitPosition.Value,
					barsInitPosition.Value + self.openPositionOffset,
					openAnimationClock.Value / self.animationTime
				);
				break;
		}
    }
}