using ThronefallMP.Components;
using ThronefallMP.NetworkPackets;

namespace ThronefallMP.Patches;

public static class PathFinderMovementEnemyPatch
{
    public static void Apply()
    {
        On.PathfindMovementEnemy.Update += Update;
    }

    private static void Update(On.PathfindMovementEnemy.orig_Update original, PathfindMovementEnemy self)
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