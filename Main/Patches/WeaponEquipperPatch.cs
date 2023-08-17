using ThronefallMP.Components;
using UnityEngine;

namespace ThronefallMP.Patches;

public static class WeaponEquipperPatch
{
    public static void Apply()
    {
        On.WeaponEquipper.Start += Start;
    }

    private static void Start(On.WeaponEquipper.orig_Start original, WeaponEquipper self)
    {
        var equipment = Equip.Convert(self.requiredWeapon);
        switch (equipment)
        {
            case Equipment.LongBow:
            case Equipment.LightSpear:
            case Equipment.HeavySword:
                var id = self.GetComponentInParent<Identifier>();
                if (id == null || id.Type != IdentifierType.Player)
                {
                    break;
                }

                var weapon = Plugin.Instance.PlayerManager.Get(id.Id)?.Weapon;
                if (weapon != equipment)
                {
                    break;
                }

                self.GetComponentInParent<PlayerInteraction>().EquipWeapon(self.activeWeapon);
                self.GetComponentInParent<PlayerUpgradeManager>();
                self.GetComponentInParent<PlayerAttack>().AssignManualAttack(self.activeWeapon);
                self.gameObject.AddComponent<PlayerWeaponVisuals>().Init(self.visuals, self.passiveWeapon);
                self.facer.AssignAttack(self.passiveWeapon);
                Object.Destroy(self);
                return;
            default:
                original(self);
                return;
        }

        Object.Destroy(self.gameObject);
    }
}