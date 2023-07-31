namespace ThronefallMP;

public static class PlayerInteractionPatch
{
    public static void Apply()
    {
        On.PlayerInteraction.Update += Update;
    }

    static void Update(On.PlayerInteraction.orig_Update original, PlayerInteraction self)
    {
        var data = self.GetComponent<PlayerNetworkData>();
        if (data.IsLocal)
        {
            original(self);
        }
    }
}