using System.Collections.Generic;
using HarmonyLib;

namespace ThronefallMP.Patches;

public static class GateOpenerPatch
{
    public static void Apply()
    {
        On.GateOpener.Update += Update;
    }

    private static void Update(On.GateOpener.orig_Update original, GateOpener self)
    {
        Traverse.Create(self).Field<IReadOnlyList<TaggedObject>>("players").Value = TagManager.instance.Players;
        Traverse.Create(self).Field<IReadOnlyList<TaggedObject>>("playerUnits").Value = TagManager.instance.PlayerUnits;
        original(self);
    }
}