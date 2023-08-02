using ThronefallMP.NetworkPackets;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class SceneTransitionManagerPatch
{
    public static bool DisableTransitionHook = false;
    
    public static void Apply()
    {
        On.SceneTransitionManager.TransitionToScene += TransitionToScene;
    }

    private static void TransitionToScene(On.SceneTransitionManager.orig_TransitionToScene original, SceneTransitionManager self, string scene)
    {
        if (!DisableTransitionHook)
        {
            var packet = new TransitionToScenePacket
            {
                ComingFromGameplayScene = self.ComingFromGameplayScene,
                Level = scene,
            };
            
            foreach (var item in PerkManager.instance.CurrentlyEquipped)
            {
                packet.Perks.Add(item.name);
            }
            
            Plugin.Instance.Network.Send(packet);
        }
        
        foreach (var data in Plugin.Instance.Network.GetAllPlayerData())
        {
            PlayerManager.UnregisterPlayer(data.GetComponent<PlayerMovement>());
            Object.Destroy(data.gameObject);
        }
        
        original(self, scene);
    }
}