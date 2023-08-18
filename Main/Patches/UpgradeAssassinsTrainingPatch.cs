namespace ThronefallMP.Patches;

public static class UpgradeAssassinsTrainingPatch
{
    public static void Apply()
    {
        On.UpgradeAssassinsTraining.OnEnable += OnEnable;
    }

    private static void OnEnable(On.UpgradeAssassinsTraining.orig_OnEnable original, UpgradeAssassinsTraining self)
    {
        UpgradeAssassinsTraining.instance = self;
        PlayerUpgradeManager.instance.assassinsTraining = self;
        foreach (var player in Plugin.Instance.PlayerManager.GetAllPlayers())
        {
            if (player.Object == null)
            {
                continue;
            }
            
            foreach (var manualAttack in player.Object.GetComponentsInChildren<ManualAttack>(true))
            {
                if (manualAttack.autoAttack)
                {
                    manualAttack.cooldownTime *= self.autoAttackCooldownDurationMulti;
                }
                else
                {
                    manualAttack.cooldownTime *= self.manualAttackCooldownDurationMulti;
                }
            }
        }
    }
}