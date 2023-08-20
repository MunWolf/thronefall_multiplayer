using ThronefallMP.Network.Packets.Game;

namespace ThronefallMP.Patches;

public static class InGameResignUIHelperPatch
{
    public static void Apply()
    {
        On.InGameResignUIHelper.Resign += Resign;
    }

    private static void Resign(On.InGameResignUIHelper.orig_Resign original, InGameResignUIHelper self)
    {
        var packet = new ResignPacket();
        Plugin.Instance.Network.Send(packet, true);
    }
}