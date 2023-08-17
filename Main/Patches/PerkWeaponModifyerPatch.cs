using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class PerkWeaponModifyerPatch
{
    public static void Apply()
    {
        On.PerkWeaponModifyer.Start += Start;
    }

    private static void Start(On.PerkWeaponModifyer.orig_Start original, PerkWeaponModifyer self)
    {
        var equipment = Equip.Convert(self.requiredPerk);
        switch (equipment)
        {
            case Equipment.LongBow:
            case Equipment.LightSpear:
            case Equipment.HeavySword:
                var id = self.GetComponent<Identifier>();
                if (id == null || id.Type != IdentifierType.Player)
                {
                    original(self);
                    return;
                }

                var weapon = Plugin.Instance.PlayerManager.Get(id.Id).Weapon;
                if (weapon != equipment)
                {
                    break;
                }

                self.autoAttack.weapon = self.weaponToInsert;
                break;
            default:
                original(self);
                return;
        }

        Object.Destroy(self);
    }
}