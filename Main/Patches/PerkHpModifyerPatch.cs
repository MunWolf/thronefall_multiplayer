namespace ThronefallMP.Patches;

public static class PerkHpModifyerPatch
{
    public static void Apply()
    {
        On.PerkHpModifyer.Start += Start;
    }

    private static void Start(On.PerkHpModifyer.orig_Start original, PerkHpModifyer self)
    {
        HpPatch.AllowHealthChangeOnClient = true;
        original(self);
        HpPatch.AllowHealthChangeOnClient = false;
    }
}