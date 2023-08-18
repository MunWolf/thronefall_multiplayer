namespace ThronefallMP.Patches;

public static class UIFrameManagerPatch
{
    public static void Apply()
    {
        On.UIFrameManager.ProcessFrameChange += ProcessFrameChange;
    }

    private static void ProcessFrameChange(
        On.UIFrameManager.orig_ProcessFrameChange original,
        UIFrameManager self,
        UIFrame nextframe,
        bool writeoldframetostack,
        bool keepoldframegameobjectactive,
        ThronefallUIElement firstselected)
    {
        nextframe.freezeTime = false;
        original(
            self,
            nextframe,
            writeoldframetostack,
            keepoldframegameobjectactive,
            firstselected
        );
    }
}