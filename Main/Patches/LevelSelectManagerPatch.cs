using HarmonyLib;

namespace ThronefallMP;

public static class LevelSelectManagerPatch
{
    static void Apply()
    {
        On.LevelSelectManager.MovePlayerToTheLevelYouCameFrom += MovePlayerToTheLevelYouCameFrom;
    }
    
    static void MovePlayerToTheLevelYouCameFrom(On.LevelSelectManager.orig_MovePlayerToTheLevelYouCameFrom original, LevelSelectManager self)
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
                    playerMovement.TeleportTo(levelInteractors.Value[i].transform.position + self.spawnOnLevelOffsetPositon);
                }
                return;
            }
        }
    }
}