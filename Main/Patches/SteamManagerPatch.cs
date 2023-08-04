namespace ThronefallMP.Patches;

public static class SteamManagerPatch
{
    public static void Apply()
    {
        On.SteamManager.UploadHighscore += UploadHighscore;
    }

    private static void UploadHighscore(On.SteamManager.orig_UploadHighscore orig, SteamManager self, int _score, string _leaderboardname)
    {
        // We don't want to upload scores we get while playing multiplayer.
    }
}