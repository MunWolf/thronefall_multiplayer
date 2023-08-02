using HarmonyLib;
using ThronefallMP.NetworkPackets;
using UnityEngine;

namespace ThronefallMP.Patches;

public class EnemySpawnerPatch
{
    public static void Apply()
    {
	    On.EnemySpawner.Update += Update;
	    On.EnemySpawner.OnStartOfTheDay += OnStartOfTheDay;
    }

    private static void OnStartOfTheDay(On.EnemySpawner.orig_OnStartOfTheDay original, EnemySpawner self)
    {
	    var treasureHunterActive = Traverse.Create(self).Field<bool>("treasureHunterActive");
	    var old = treasureHunterActive.Value;
	    treasureHunterActive.Value = false;
	    original(self);
	    treasureHunterActive.Value = old;
	    if (self.FinalWaveComingUp && old && Plugin.Instance.Network.Server)
	    {
		    GlobalData.Balance += PerkManager.instance.treasureHunterGoldAmount;
	    }
    }

    private static void Update(On.EnemySpawner.orig_Update original, EnemySpawner self)
    {
        if (!self.SpawningInProgress)
        {
            return;
        }
        
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
		var randomPointOnSpawnLine = self.GetRandomPointOnSpawnLine();

		var coins = 0;
		var spawnedUnits = Traverse.Create(self).Field<int>("spawnedUnits");
		var goldCoinsPerEnemy = Traverse.Create(self).Field<int[]>("goldCoinsPerEnemy");
		if (goldCoinsPerEnemy.Value.Length > spawnedUnits.Value)
		{
			coins = goldCoinsPerEnemy.Value[spawnedUnits.Value];
		}

		var packet = new EnemySpawnPacket
		{
			Wave = waveNumber,
			Spawn = spawnIndex,
			Id = _nextEnemyId,
			Position = randomPointOnSpawnLine,
			Coins = coins
		};
		
		Plugin.Instance.Network.Send(packet, true);
		++_nextEnemyId;
		
		spawnedUnits.Value++;
		if (spawnedUnits.Value >= self.count)
		{
			finished.Value = true;
		}
    }

    public static void SpawnEnemy(int waveNumber, int spawnIndex, Vector3 position, int id, int coins)
    {
	    var spawn = EnemySpawner.instance.waves[waveNumber].spawns[spawnIndex];
	    var spawnedUnits = Traverse.Create(spawn).Field<int>("spawnedUnits");
	    var finished = Traverse.Create(spawn).Field<bool>("finished");
	    
	    SpawnEnemy(spawn, position, id, coins);
	    spawnedUnits.Value++;
	    if (spawnedUnits.Value >= spawn.count)
	    {
		    finished.Value = true;
	    }
    }

    private static GameObject SpawnEnemy(Spawn self, Vector3 position, int id, int coins)
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

		var singleHp = gameObject.GetComponentInChildren<Hp>();
		singleHp.coinCount = coins;
		
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