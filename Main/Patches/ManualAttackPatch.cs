namespace ThronefallMP.Patches;

public static class ManualAttackPatch
{
    public static void Apply()
    {
        On.ManualAttack.Update += Update;
    }

    private static void Update(On.ManualAttack.orig_Update orig, ManualAttack self)
    {
        
    }
}