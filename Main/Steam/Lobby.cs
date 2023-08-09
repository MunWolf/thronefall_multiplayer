using Steamworks;

namespace ThronefallMP.Steam;

public struct Lobby
{
    public CSteamID Id;
    public string Name;
    public bool HasPassword;
    public int PlayerCount;
    public int MaxPlayerCount;
}