using ThronefallMP.NetworkPackets;

namespace ThronefallMP.Patches;

public static class PathfindMovementPlayerunitPatch
{
    public static void Apply()
    {
        On.PathfindMovementPlayerunit.Update += Update;
    }

    private static void Update(On.PathfindMovementPlayerunit.orig_Update original, PathfindMovementPlayerunit self)
    {
        var identifier = self.GetComponent<Identifier>();
        if (identifier == null || identifier.Type == IdentifierType.Invalid)
        {
            original(self);
            return;
        }
        
        if (!Plugin.Instance.Network.Server)
        {
            return;
        }
        
        original(self);
        var packet = new PositionPacket
        {
            Target = new IdentifierData(identifier),
            Position = self.transform.position
        };
        
        Plugin.Instance.Network.Send(packet);
    }
}