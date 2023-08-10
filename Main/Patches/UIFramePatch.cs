namespace ThronefallMP.Patches;

public class UIFramePatch
{
    public static int DisableGameUIInputCount = 0;
    
    public static void Apply()
    {
        On.UIFrame.HandleButtonNavigation += HandleButtonNavigation;
        On.UIFrame.HandleMouseNavigation += HandleMouseNavigation;
    }

    private static void HandleButtonNavigation(On.UIFrame.orig_HandleButtonNavigation original, UIFrame self)
    {
        if (DisableGameUIInputCount == 0)
        {
            original(self);
        }
    }

    private static void HandleMouseNavigation(On.UIFrame.orig_HandleMouseNavigation original, UIFrame self)
    {
        if (DisableGameUIInputCount == 0)
        {
            original(self);
        }
    }
}