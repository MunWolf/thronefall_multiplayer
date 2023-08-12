using ThronefallMP.Components;
using ThronefallMP.Network.Packets.Game;

namespace ThronefallMP.Patches;

public static class PathfindMovementPlayerunitPatch
{
    public static void Apply()
    {
        On.PathfindMovementPlayerunit.Update += Update;
    }

    private static void Update(On.PathfindMovementPlayerunit.orig_Update original, PathfindMovementPlayerunit self)
    {
        original(self);
        var identifier = self.GetComponent<Identifier>();
        if (identifier != null && identifier.Type != IdentifierType.Invalid && Plugin.Instance.Network.Server)
        {
            var packet = new PositionPacket
            {
                Target = new IdentifierData(identifier),
                Position = self.transform.position
            };
        
            Plugin.Instance.Network.Send(packet);
        }
    }
}