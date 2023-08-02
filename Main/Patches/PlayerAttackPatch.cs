using HarmonyLib;

namespace ThronefallMP.Patches;

public static class PlayerAttackPatch
{
    public static void Apply()
    {
        On.PlayerAttack.Update += Update;
    }

    private static void Update(On.PlayerAttack.orig_Update orig, PlayerAttack self)
    {
        var attack = Traverse.Create(self).Field<ManualAttack>("attack");
        if (attack.Value != null)
        {
            attack.Value.Tick();
            var data = self.GetComponent<PlayerNetworkData>();
            if (data != null && data.IsLocal)
            {
                self.ui.SetCurrentCooldownPercentage(attack.Value.CooldownPercentage);
            }
        }
    }
}