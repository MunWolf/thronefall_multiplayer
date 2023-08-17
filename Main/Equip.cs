﻿using System.Collections.Generic;
using HarmonyLib;

namespace ThronefallMP;

public enum Equipment
{
    Invalid,
    PerkPoint,
        
    LongBow,
    LightSpear,
    HeavySword,
        
    RoyalMint,
    ArcaneTowers,
    HeavyArmor,
    CastleFortifications,
    RingOfResurrection,
    PumpkinFields,
    ArchitectsCouncil,
    GodsLotion,
    CastleBlueprints,
    GladiatorSchool,
    WarHorse,
    GlassCannon,
    BigHarbours,
    EliteWarriors,
    ArcherySkills,
    FasterResearch,
    TowerSupport,
    FortifiedHouses,
    CommanderMode,
    HealingSpirits,
    IceMagic,
    MeleeResistance,
    PowerTower,
    RangedResistance,
    TreasureHunter,
    IndestructibleMines,
    WarriorMode,
        
    AssassinsTraining,
    MagicArmor,
    GodlyCurse,
    CastleUp,
        
    MillScarecrow,
    MillWindSpirits,
        
    TowerHotOil,
        
    MeleeFlails,
    RangedHunters,
    MeleeBerserkers,
    RangedFireArchers,
        
    Turtle,
    Tiger,
    Rat,
    Falcon,
    Destruction,
    Wasp,
    Death
}

public static class Equip
{
    
    private static readonly Dictionary<string, Equipment> NameToEquip = new()
    {
        { "", Equipment.Invalid },
        { "Perk Point", Equipment.PerkPoint },
        
        // Weapons
        { "Long Bow", Equipment.LongBow },
        { "Light Spear", Equipment.LightSpear },
        { "Heavy Sword", Equipment.HeavySword },
        
        // Perks
        { "Universal Income", Equipment.RoyalMint },
        { "Arcane Towers", Equipment.ArcaneTowers },
        { "Heavy Armor", Equipment.HeavyArmor },
        { "Castle Fortifications", Equipment.CastleFortifications },
        { "Ring of Resurection", Equipment.RingOfResurrection },
        { "Pumpkin Fields", Equipment.PumpkinFields },
        { "Architect's Council", Equipment.ArchitectsCouncil },
        { "Gods Lotion", Equipment.GodsLotion },
        { "Castle Blueprints", Equipment.CastleBlueprints },
        { "Gladiator School", Equipment.GladiatorSchool },
        { "War Horse", Equipment.WarHorse },
        { "Glass Canon", Equipment.GlassCannon },
        { "Big Harbours", Equipment.BigHarbours },
        { "Ellite Warriors", Equipment.EliteWarriors },
        { "Archery Skills", Equipment.ArcherySkills },
        { "Faster Research", Equipment.FasterResearch },
        { "TowerSupport", Equipment.TowerSupport },
        { "Fortified Houses", Equipment.FortifiedHouses },
        { "Commander Mode", Equipment.CommanderMode },
        { "Healing Spirits", Equipment.HealingSpirits },
        { "Ice Magic", Equipment.IceMagic },
        { "Melee Resistence", Equipment.MeleeResistance },
        { "Power Tower", Equipment.PowerTower },
        { "Ranged Resistence", Equipment.RangedResistance },
        { "Treasure Hunter", Equipment.TreasureHunter },
        { "Indestructible Mines", Equipment.IndestructibleMines },
        { "Warrior Mode", Equipment.WarriorMode },
        
        // Castle Upgrades
        { "CCAssassinsTraining", Equipment.AssassinsTraining },
        { "CCMagicArmor", Equipment.MagicArmor },
        { "CCGodlyCurse", Equipment.GodlyCurse },
        { "CCCastleUp", Equipment.CastleUp },
        
        // Mill Upgrades
        { "MillScarecrow", Equipment.MillScarecrow },
        { "MillWindSpirits", Equipment.MillWindSpirits },
        
        // Tower Upgrades
        { "TowerHotOil", Equipment.TowerHotOil },
        
        // Units
        { "MeleeFlails", Equipment.MeleeFlails },
        { "RangedHunters", Equipment.RangedHunters },
        { "MeleeBerserks", Equipment.MeleeBerserkers },
        { "RangedFireArchers", Equipment.RangedFireArchers },
        
        // Mutators
        { "Taunt The Turtle God", Equipment.Turtle },
        { "Taunt The Tiger God", Equipment.Tiger },
        { "Taunt The Rat God", Equipment.Rat },
        { "Taunt The Falcon God", Equipment.Falcon },
        { "Taunt God of Destruction", Equipment.Destruction },
        { "Taunt The Cheese God", Equipment.Wasp },
        { "Taunt The Disease God", Equipment.Death },
    };
    
    private static readonly Dictionary<Equipment, Equippable> EquipmentToEquippable = new();
    private static readonly Dictionary<Equippable, Equipment> EquippableToEquipment = new();
    private static bool _initialized;

    private static void InitializeDictionaries()
    {
        _initialized = true;
        var metaLevels = Traverse.Create(PerkManager.instance).Field<List<MetaLevel>>("metaLevels");
        Plugin.Log.LogInfoFiltered("Equipment", "Initializing converter dictionary");
        Plugin.Log.LogInfoFiltered("Equipment", "Meta levels");
        foreach (var meta in metaLevels.Value)
        {
            var equipment = Convert(meta.reward.name);
            Plugin.Log.LogInfoFiltered("Equipment", $"- {equipment} = {meta.reward.name}");
            EquipmentToEquippable[equipment] = meta.reward;
            EquippableToEquipment[meta.reward] = equipment;
        }
        
        Plugin.Log.LogInfoFiltered("Equipment", "Currently Unlocked");
        foreach (var unlocked in PerkManager.instance.UnlockedEquippables)
        {
            var equipment = Convert(unlocked.name);
            Plugin.Log.LogInfoFiltered("Equipment", $"- {equipment} = {unlocked.name}");
            EquipmentToEquippable[equipment] = unlocked;
            EquippableToEquipment[unlocked] = equipment;
        }
    }

    public static void ClearEquipments()
    {
        Plugin.Log.LogInfoFiltered("Equipment", "Clearing equipment");
        PerkManager.instance.CurrentlyEquipped.Clear();
    }

    public static void EquipEquipment(Equipment equipment)
    {
        if (!_initialized)
        {
            InitializeDictionaries();
        }
        
        Plugin.Log.LogInfoFiltered("Equipment", $"Equipping {equipment} -> {EquipmentToEquippable[equipment].displayName}");
        PerkManager.instance.CurrentlyEquipped.Add(EquipmentToEquippable[equipment]);
    }
    
    public static Equipment Convert(Equippable equip)
    {
        if (!_initialized)
        {
            InitializeDictionaries();
        }

        return EquippableToEquipment.GetValueSafe(equip);
    }
    
    public static Equippable Convert(Equipment equip)
    {
        if (!_initialized)
        {
            InitializeDictionaries();
        }

        return EquipmentToEquippable.GetValueSafe(equip);
    }
    
    public static Equipment Convert(string name)
    {
        return NameToEquip.GetValueSafe(name);
    }
}