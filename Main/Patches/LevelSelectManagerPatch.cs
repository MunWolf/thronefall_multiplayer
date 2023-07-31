using HarmonyLib;

namespace ThronefallMP.Patches;

public static class LevelSelectManagerPatch
{
    static void Apply()
    {
        On.LevelSelectManager.MovePlayerToTheLevelYouCameFrom += MovePlayerToTheLevelYouCameFrom;
    }
    
    private static void MovePlayerToTheLevelYouCameFrom(On.LevelSelectManager.orig_MovePlayerToTheLevelYouCameFrom original, LevelSelectManager self)
    {
        SceneTransitionManager sceneTransitionManager = SceneTransitionManager.instance;
        if (!sceneTransitionManager)
        {
            return;
        }
        if (sceneTransitionManager.ComingFromGameplayScene == "")
        {
            return;
        }

        var levelInteractors = Traverse.Create(self).Field<LevelInteractor[]>("levelInteractors");
        for (int i = 0; i < levelInteractors.Value.Length; i++)
        {
            if (levelInteractors.Value[i].sceneName == sceneTransitionManager.ComingFromGameplayScene)
            {
                foreach (var data in Plugin.Instance.Network.GetAllPlayerData())
                {
                    var playerMovement = data.GetComponent<PlayerMovement>();
                    var spawnLocation = levelInteractors.Value[i].transform.position + self.spawnOnLevelOffsetPositon;
                    spawnLocation = Utils.GetSpawnLocation(spawnLocation, data.id);
                    playerMovement.TeleportTo(spawnLocation);
                }
                return;
            }
        }
    }
}