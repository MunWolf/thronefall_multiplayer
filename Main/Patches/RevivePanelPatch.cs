using Microsoft.Win32;

namespace ThronefallMP.Patches;

public static class RevivePanelPatch
{
    public static void Apply()
    {
        On.RevivePanel.Update += Update;
    }

    private static void Update(On.RevivePanel.orig_Update original, RevivePanel self)
    {
        var data = Plugin.Instance.Network.LocalPlayerData;
        if (data != null)
        {
            self.playerReviveComponent = data.GetComponent<AutoRevive>();
        }
        
        original(self);
    }
}