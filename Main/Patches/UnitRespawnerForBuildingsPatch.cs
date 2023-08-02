using ThronefallMP.NetworkPackets;
using UnityEngine;

namespace ThronefallMP.Patches;

public class UnitRespawnerForBuildingsPatch
{
    public static void Apply()
    {
        On.UnitRespawnerForBuildings.RespawnAKnockedOutUnit += RespawnAKnockedOutUnit;
    }

    private static void RespawnAKnockedOutUnit(On.UnitRespawnerForBuildings.orig_RespawnAKnockedOutUnit original, UnitRespawnerForBuildings self)
    {
        if (!Plugin.Instance.Network.Server)
        {
            return;
        }

        foreach (var hp in self.units)
        {
            if (!hp.gameObject.activeInHierarchy || !hp.KnockedOut)
            {
                continue;
            }
            
            var component = hp.GetComponent<PathfindMovementPlayerunit>();
            var position = self.taggedObject.colliderForBigOjectsToMeasureDistance.ClosestPoint(component.HopePositionOriginal);
            var identifier = self.GetComponent<Identifier>();
            if (identifier != null && identifier.Type != IdentifierType.Invalid)
            {
                var packet = new RespawnPacket
                {
                    Target = new IdentifierData(identifier),
                    Position = position
                };
                
                Plugin.Instance.Network.Send(packet);
            }
            
            RevivePlayerUnit(hp, position);
            break;
        }
    }

    public static void RevivePlayerUnit(Hp hp, Vector3 position)
    {
        hp.Revive(true, 1f);
        var component = hp.GetComponent<PathfindMovementPlayerunit>();
        hp.transform.position = position;
        component.SnapToNavmesh();
    }
}