using HarmonyLib;
using ThronefallMP.NetworkPackets;
using UnityEngine;

namespace ThronefallMP.Patches;

public class EnemySpawnerPatch
{
    public static void Apply()
    {
	    On.EnemySpawner.Update += Update;
    }

    public static void Update(On.EnemySpawner.orig_Update original, EnemySpawner self)
    {
        if (!self.SpawningInProgress)
        {
            return;
        }
        
        // TODO: Sunc this instead.
        var lastSpawnPeriodDuration = Traverse.Create(self).Field<float>("lastSpawnPeriodDuration");
        lastSpawnPeriodDuration.Value += Time.deltaTime;
        if (Plugin.Instance.Network.Server)
        {
	        for (var i = 0; i < self.waves[self.Wavenumber].spawns.Count; i++)
	        {
		        UpdateSpawn(self.waves[self.Wavenumber].spawns[i], self.Wavenumber, i);
	        }
        }

        if (!self.waves[self.Wavenumber].HasFinished())
        {
            return;
        }

        if (!self.InfinitelySpawning)
        {
            self.StopSpawnAfterWaveAndReset();
            return;
        }

        if (TagManager.instance.CountAllTaggedObjectsWithTag(TagManager.ETag.EnemyOwned) > 0)
        {
            return;
        }

        foreach (var spawn in self.waves[self.Wavenumber].spawns)
        {
            spawn.Reset(false);
        }
    }

    private static int _nextEnemyId;
    private static void UpdateSpawn(Spawn self, int waveNumber, int spawnIndex)
    {
	    var finished = Traverse.Create(self).Field<bool>("finished");
		if (finished.Value)
		{
			return;
		}
		
		var waitBeforeNextSpawn = Traverse.Create(self).Field<float>("waitBeforeNextSpawn");
		waitBeforeNextSpawn.Value -= Time.deltaTime;
		if (waitBeforeNextSpawn.Value > 0f)
		{
			return;
		}
		
		waitBeforeNextSpawn.Value = self.interval;
		Vector3 randomPointOnSpawnLine = self.GetRandomPointOnSpawnLine();

		var packet = new EnemySpawnPacket
		{
			Wave = waveNumber,
			Spawn = spawnIndex,
			Id = _nextEnemyId,
			Position = randomPointOnSpawnLine
		};
		
		Plugin.Instance.Network.Send(packet);
		var enemy = SpawnEnemy(self, randomPointOnSpawnLine, _nextEnemyId);
		++_nextEnemyId;
		
		var spawnedUnits = Traverse.Create(self).Field<int>("spawnedUnits");
		var goldCoinsPerEnemy = Traverse.Create(self).Field<int[]>("goldCoinsPerEnemy");
		if (goldCoinsPerEnemy.Value.Length > spawnedUnits.Value)
		{
			// TODO: Coins spawning should be synced so only do this on the server.
			enemy.GetComponentInChildren<Hp>().coinCount = goldCoinsPerEnemy.Value[spawnedUnits.Value];
		}
		
		spawnedUnits.Value++;
		if (spawnedUnits.Value >= self.count)
		{
			finished.Value = true;
		}
    }

    public static void SpawnEnemy(int waveNumber, int spawnIndex, Vector3 position, int id)
    {
	    var spawn = EnemySpawner.instance.waves[waveNumber].spawns[spawnIndex];
	    var spawnedUnits = Traverse.Create(spawn).Field<int>("spawnedUnits");
	    var finished = Traverse.Create(spawn).Field<bool>("finished");
	    
	    SpawnEnemy(spawn, position, id);
	    spawnedUnits.Value++;
	    if (spawnedUnits.Value >= spawn.count)
	    {
		    finished.Value = true;
	    }
    }

    private static GameObject SpawnEnemy(Spawn self, Vector3 position, int id)
    {
		GameObject gameObject;
		if (self.spawnLine == self.enemyPrefab.transform)
		{
			gameObject = self.enemyPrefab;
			gameObject.SetActive(true);
			var found = gameObject.TryGetComponent<Identifier>(out var identifier);
			if (!found)
			{
				identifier = gameObject.AddComponent<Identifier>();
				identifier.SetIdentity(IdentifierType.Enemy, id);
			}
		}
		else
		{
			gameObject = Object.Instantiate(self.enemyPrefab, position, Quaternion.identity);
			gameObject.AddComponent<Identifier>()
				.SetIdentity(IdentifierType.Enemy, id);
			var instance = EnemySpawnManager.instance;
			if (instance.weaponOnSpawn)
			{
				instance.weaponOnSpawn.Attack(
					position + Vector3.up * instance.weaponAttackHeight,
					null, 
					Vector3.forward,
					gameObject.GetComponent<TaggedObject>()
				);
			}
		}
		
		var tauntTheTurtle = Traverse.Create(self).Field<bool>("tauntTheTurtle");
		if (tauntTheTurtle.Value)
		{
			var hpTemp = Traverse.Create(self).Field<Hp>("tauntTheTurtle");
			hpTemp.Value = gameObject.GetComponentInChildren<Hp>();
			hpTemp.Value.maxHp *= PerkManager.instance.tauntTheTurtle_hpMultiplyer;
			hpTemp.Value.Heal(float.MaxValue);
		}
		
		var tauntTheTiger = Traverse.Create(self).Field<bool>("tauntTheTiger");
		if (tauntTheTiger.Value)
		{
			foreach (var attack in gameObject.GetComponentsInChildren<AutoAttack>())
			{
				attack.DamageMultiplyer *= PerkManager.instance.tauntTheTiger_damageMultiplyer;
			}
			
			foreach (var hp in gameObject.GetComponentsInChildren<Hp>())
			{
				hp.DamageMultiplyer *= PerkManager.instance.tauntTheTiger_damageMultiplyer;
			}
		}
		
		var tauntTheFalcon = Traverse.Create(self).Field<bool>("tauntTheFalcon");
		if (tauntTheFalcon.Value)
		{
			foreach (var pathfindMovementEnemy in gameObject.GetComponentsInChildren<PathfindMovementEnemy>())
			{
				pathfindMovementEnemy.movementSpeed *= PerkManager.instance.tauntTheFalcon_speedMultiplyer;
				pathfindMovementEnemy.agroTimeWhenAttackedByPlayer *= PerkManager.instance.tauntTheFalcon_chasePlayerTimeMultiplyer;
			}
		}

		return gameObject;
    }
}