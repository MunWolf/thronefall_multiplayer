using System.Collections.Generic;
using HarmonyLib;

namespace ThronefallMP;

public static class EquippableConverters
{
    public enum Equip
    {
        Invalid,
        CheeseGod,
        CommanderMode,
        DestructionGod,
        FalconGod,
        GlassCannon,
        GodOfDeath,
        HealingSpirits,
        IceMagic,
        MeleeResistance,
        RangedResistance,
        RatGod,
        TigerGod,
        TreasureHunter,
        TurtleGod,
        WarriorMode
    }
    
    private static readonly Dictionary<string, Equippable> NameToEquippable = new();

    private static void InitializeDictionaries()
    {
        var metaLevels = Traverse.Create(PerkManager.instance).Field<List<MetaLevel>>("metaLevels");
        Plugin.Log.LogInfo("Initializing converter dictionary");
        Plugin.Log.LogInfo("Currently Unlocked");
        foreach (var unlocked in PerkManager.instance.UnlockedEquippables)
        {
            Plugin.Log.LogWarning($"- {unlocked.name}");
            NameToEquippable[unlocked.name] = unlocked;
        }
        Plugin.Log.LogInfo("Metalevels");
        foreach (var meta in metaLevels.Value)
        {
            Plugin.Log.LogInfo($"- {meta.reward.name}");
            NameToEquippable[meta.reward.name] = meta.reward;
        }
    }
    
    public static Equippable Convert(string name)
    {
        if (NameToEquippable.Count == 0)
        {
            InitializeDictionaries();
        }
        
        return NameToEquippable.GetValueSafe(name);
    }
}