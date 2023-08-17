using Steamworks;

namespace ThronefallMP;

public class CheatHandler
{
    private static bool CheatsEnabled => !Plugin.Instance.Network.Online || (
        SteamManager.Initialized && SteamMatchmaking.GetLobbyData(Plugin.Instance.Network.Lobby, "cheats_enabled") == "yes");

    public CheatHandler()
    {
        Plugin.Instance.Network.OnReceivedChatMessage += OnHandleMessage;
    }
    
    private void OnHandleMessage(string user, string message)
    {
        if (!CheatsEnabled || !message.StartsWith("/"))
        {
            return;
        }

        message = message.Remove(0, 1);
        Plugin.Log.LogInfo($"Parsing cheat '{message}'");
        var parts = message.Split(' ');
        Plugin.Log.LogInfo($"Command '{parts[0]}'");
        switch (parts[0])
        {
            case "coins_add":
                if (parts.Length == 2 && Plugin.Instance.Network.Server && int.TryParse(parts[1], out var add))
                {
                    GlobalData.Balance += add;
                    Plugin.Instance.Network.SendChatMessage($"{user} Cheated in {add} coins.");
                }
                else
                {
                    Plugin.Log.LogInfo($"Failed to parse coins");
                }
                return;
        }
        Plugin.Log.LogInfo($"Not found");
    }
}