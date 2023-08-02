using ThronefallMP.NetworkPackets;

namespace ThronefallMP.Patches;

public static class SceneTransitionManagerPatch
{
    public static bool DisableTransitionHook = false;
    
    public static void Apply()
    {
        On.SceneTransitionManager.TransitionFromLevelSelectToLevel += TransitionFromLevelSelectToLevel;
    }
    
    private static void TransitionFromLevelSelectToLevel(On.SceneTransitionManager.orig_TransitionFromLevelSelectToLevel original, SceneTransitionManager self, string levelName)
    {
        if (!DisableTransitionHook)
        {
            var packet = new TransitionToScenePacket
            {
                Type = TransitionToScenePacket.TransitionType.LevelSelectToLevel,
                Level = levelName,
            };
            
            foreach (var item in PerkManager.instance.CurrentlyEquipped)
            {
                packet.Perks.Add(item.name);
            }
            
            Plugin.Instance.Network.Send(packet);
        }

        original(self, levelName);
    }
}