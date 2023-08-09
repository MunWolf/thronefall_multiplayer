using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace ThronefallMP.Steam;

public class Network : MonoBehaviour
{
    private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequestCallback;
    private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;
    
    private List<SteamNetworkingIdentity> _peers = new();

    public int MaxPlayers { get; set; }
    public bool Authority => Server || !Online;
    public bool Server { get; private set; }
    public bool Online { get; private set; }
    
    public void Awake()
    {
        _sessionRequestCallback = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        _sessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        if (_peers.Count < MaxPlayers ||
            request.m_identityRemote.m_eType != ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID)
        {
            return;
        }

        //SteamNetworkingMessages.SendMessageToUser(ref request.m_identityRemote, );
    }

    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t failed)
    {
        _peers.Remove(failed.m_info.m_identityRemote);
        if (!Server)
        {
            // TODO: Show an error
            Online = _peers.Count != 0;
        }
        else
        {
            // TODO: Show notification that client left.
        }
    }
    
    public void Host()
    {
        
    }

    public void Connect(CSteamID host)
    {
        
    }

    public void Send(CSteamID except = new CSteamID())
    {
        
    }
}