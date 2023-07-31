using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThronefallMP.Patches;

public static class BuildSlotPatch
{
    public class Identifier : MonoBehaviour
    {
        public int id;
    }
    
    public static Dictionary<int, BuildSlot> Buildings = new();
    
    public static void Apply()
    {
        On.BuildSlot.Start += Start;
    }

    private static void Start(On.BuildSlot.orig_Start original, BuildSlot self)
    {
        original(self);
        if (self.ActivatorBuilding == null)
        {
            var slots = new List<BuildSlot> { self };
            var id = 0;
            while (slots.Count > 0)
            {
                var current = slots[0];
                slots.Remove(current);
                slots.AddRange(current.BuiltSlotsThatRelyOnThisBuilding);
                AssignId(self, id++);
            }
        }
    }

    private static void AssignId(BuildSlot self, int id)
    {
        var identifier = self.gameObject.AddComponent<Identifier>();
        identifier.id = id;
        Buildings[id] = self;
        Plugin.Log.LogInfo("Building " + self.buildingName + " assigned id " + id);
    }
}