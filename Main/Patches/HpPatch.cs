using System;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;
using UniverseLib.Utility;

namespace ThronefallMP.Patches;

public static class HpPatch
{
    public static void Apply()
    {
        On.Hp.TakeDamage += TakeDamage;
        On.Hp.ScaleHp += ScaleHp;
        On.Hp.Heal += Heal;
    }

    private static bool _allowHealthChangeOnClient;
    
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
            var packet = new DamagePacket
            {
                Target = new IdentifierData(identifier),
                Source = new IdentifierData(damageComingFrom == null ? null : damageComingFrom.GetComponent<Identifier>()),
                Damage = amount,
                CausedByPlayer = causedByPlayer,
                InvokeFeedbackEvents = invokeFeedbackEvents
            };
            
            Plugin.Instance.Network.Send(packet);
        }
        else if (!_allowHealthChangeOnClient)
        {
            return false;
        }

        return original(self, amount, damageComingFrom, causedByPlayer, invokeFeedbackEvents);
    }

    public static void InflictDamage(
        IdentifierData targetId,
        IdentifierData sourceId,
        float amount,
        bool causedByPlayer,
        bool invokeFeedbackEvents)
    {
        var target = targetId.Get();
        if (target == null)
        {
            Plugin.Log.LogWarning($"Failed to inflict damage, target {targetId.Type}:{targetId.Id} did not exist");
            return;
        }
        
        var source = sourceId.Get();
        var component = source == null ? null : source.GetComponent<TaggedObject>();
        _allowHealthChangeOnClient = true;
        target.GetComponent<Hp>().TakeDamage(
            amount, component, causedByPlayer, invokeFeedbackEvents);
        _allowHealthChangeOnClient = false;
    }

    private static void ScaleHp(On.Hp.orig_ScaleHp original, Hp self, float multiply)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self, multiply);
            return;
        }
        
        if (Plugin.Instance.Network.Server)
        {
            var packet = new ScaleHpPacket
            {
                Target = new IdentifierData(identifier),
                Multiplier = multiply
            };
            
            Plugin.Instance.Network.Send(packet);
        }
        
        if (Plugin.Instance.Network.Server || _allowHealthChangeOnClient)
        {
            original(self, multiply);
        }
    }

    public static void ScaleHp(IdentifierData targetId, float multiply)
    {
        var target = targetId.Get();
        if (target == null)
        {
            Plugin.Log.LogWarning($"Failed to scale hp, target {targetId.Type}:{targetId.Id} did not exist");
            return;
        }
        
        _allowHealthChangeOnClient = true;
        target.GetComponent<Hp>().ScaleHp(multiply);
        _allowHealthChangeOnClient = false;
    }

    private static void Heal(On.Hp.orig_Heal original, Hp self, float amount)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self, amount);
            return;
        }

        if (Math.Abs(self.HpValue - self.maxHp) < 0.01f)
        {
            return;
        }
        
        if (Plugin.Instance.Network.Server)
        {
            var packet = new HealPacket
            {
                Target = new IdentifierData(identifier),
                Amount = amount
            };

            Plugin.Instance.Network.Send(packet);
        }
        
        if (Plugin.Instance.Network.Server || _allowHealthChangeOnClient)
        {
            original(self, amount);
        }
    }

    public static void Heal(IdentifierData targetId, float amount)
    {
        var target = targetId.Get();
        if (target == null)
        {
            return;
        }
        
        _allowHealthChangeOnClient = true;
        target.GetComponent<Hp>().Heal(amount);
        _allowHealthChangeOnClient = false;
    }
}