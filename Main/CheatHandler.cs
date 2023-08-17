using System.Collections.Generic;
using System.Linq;
using Steamworks;

namespace ThronefallMP;

public class CheatHandler
{
    private static bool CheatsEnabled => !Plugin.Instance.Network.Online || (
        SteamManager.Initialized && SteamMatchmaking.GetLobbyData(Plugin.Instance.Network.Lobby, "cheats_enabled") == "yes");

    private delegate bool CheatMessageHandler(string user, string[] parts);

    private Dictionary<string, CheatMessageHandler> _handlers = new();
    
    public CheatHandler()
    {
        _handlers.Add("add_coins", AddCoins);
        Plugin.Instance.Network.AddChatMessageHandler(100, OnMessageReceived);
    }
    
    private bool OnMessageReceived(string user, string message)
    {
        if (!CheatsEnabled || !Plugin.Instance.Network.Server || !message.StartsWith("/"))
        {
            return false;
        }

        message = message.Remove(0, 1);
        Plugin.Log.LogInfo($"Parsing cheat '{message}'");
        var parts = message.Split(' ');
        Plugin.Log.LogInfo($"Command '{parts[0]}'");
        if (_handlers.TryGetValue(parts[0], out var handler))
        {
            return handler(user, parts.Skip(1).ToArray());
        }
        
        Plugin.Log.LogInfo($"Not found");
        return false;
    }

    private bool AddCoins(string user, string[] parts)
    {
        if (parts.Length != 1)
        {
            Plugin.Log.LogInfo($"Too many arguments");
            return false;
        }
        
        if (!int.TryParse(parts[0], out var coins))
        {
            Plugin.Log.LogInfo($"Failed to parse coins");
            return false;
        }

        GlobalData.Balance += coins;
        Plugin.Instance.Network.SendServerMessage($"{user} Cheated in {coins} coins.");
        return true;
    }
}