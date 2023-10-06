using System;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;

namespace ThronefallMP.Patches;

public static class HpPatch
{
    public static void Apply()
    {
        On.Hp.TakeDamage += TakeDamage;
        On.Hp.ScaleHp += ScaleHp;
        On.Hp.Heal += Heal;
        On.Hp.SetHpToMaxHp += SetHpToMaxHp;
    }

    public static bool AllowHealthChangeOnClient;
    
    private static bool TakeDamage(
        On.Hp.orig_TakeDamage original,
        Hp self,
        float amount,
        TaggedObject damageComingFrom,
        bool causedByPlayer,
        bool invokeFeedbackEvents)
    {
        // Only block damage to targets we can actually identify.
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            return original(self, amount, damageComingFrom, causedByPlayer, invokeFeedbackEvents);
        }
        
        if (Plugin.Instance.Network.Server)
        {
            var packet = new DamageFeedbackPacket
            {
                Target = new IdentifierData(identifier),
                CausedByPlayer = causedByPlayer,
            };
            
            Plugin.Instance.Network.Send(packet);
        }
        else if (!AllowHealthChangeOnClient)
        {
            return false;
        }

        return original(self, amount, damageComingFrom, causedByPlayer, invokeFeedbackEvents);
    }

    private static void ScaleHp(On.Hp.orig_ScaleHp original, Hp self, float multiply)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self, multiply);
            return;
        }
        
        if (Plugin.Instance.Network.Server || AllowHealthChangeOnClient)
        {
            original(self, multiply);
        }
    }

    private static void Heal(On.Hp.orig_Heal original, Hp self, float amount)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self, amount);
            return;
        }

        if (Plugin.Instance.Network.Server || AllowHealthChangeOnClient)
        {
            original(self, amount);
        }
    }

    private static void SetHpToMaxHp(On.Hp.orig_SetHpToMaxHp original, Hp self)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self);
            return;
        }

        if (Plugin.Instance.Network.Server || AllowHealthChangeOnClient)
        {
            original(self);
        }
    }
}