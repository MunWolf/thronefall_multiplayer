using HarmonyLib;

namespace ThronefallMP.Patches;

public static class LevelSelectManagerPatch
{
    public static void Apply()
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
        foreach (var level in levelInteractors.Value)
        {
            if (level.sceneName == sceneTransitionManager.ComingFromGameplayScene)
            {
                foreach (var data in Plugin.Instance.PlayerManager.GetAllPlayerData())
                {
                    var playerMovement = data.GetComponent<PlayerMovement>();
                    var spawnLocation = level.transform.position + self.spawnOnLevelOffsetPositon;
                    spawnLocation = Utils.GetSpawnLocation(spawnLocation, data.id);
                    playerMovement.TeleportTo(spawnLocation);
                }
                return;
            }
        }
    }
}