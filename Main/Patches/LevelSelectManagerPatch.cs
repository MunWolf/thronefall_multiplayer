using HarmonyLib;
using ThronefallMP.Utils;

namespace ThronefallMP.Patches;

public static class LevelSelectManagerPatch
{
    public static void Apply()
    {
        On.LevelSelectManager.MovePlayerToTheLevelYouCameFrom += MovePlayerToTheLevelYouCameFrom;
    }
    
    private static void MovePlayerToTheLevelYouCameFrom(On.LevelSelectManager.orig_MovePlayerToTheLevelYouCameFrom original, LevelSelectManager self)
    {
        var sceneTransitionManager = SceneTransitionManager.instance;
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
            if (level != null) { }
            if (level.levelInfo.sceneName != sceneTransitionManager.ComingFromGameplayScene)
            {
                continue;
            }
            
            foreach (var data in Plugin.Instance.PlayerManager.GetAllPlayerData())
            {
                var playerMovement = data.GetComponent<PlayerMovement>();
                var spawnLocation = level.transform.position + self.spawnOnLevelOffsetPositon;
                spawnLocation = Helpers.GetSpawnLocation(spawnLocation, data.id);
                playerMovement.TeleportTo(spawnLocation);
            }
            
            break;
        }
    }
}