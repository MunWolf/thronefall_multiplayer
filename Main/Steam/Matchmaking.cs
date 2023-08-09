using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace ThronefallMP.Steam;

public class Matchmaking : MonoBehaviour
{
    private CSteamID _lobby;

    public delegate void LobbiesChanged(IEnumerable<CSteamID> lobbies);
    public event LobbiesChanged OnLobbiesChanged;
    
    public void Awake()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }
        
		_favoritesListChanged = Callback<FavoritesListChanged_t>.Create(OnFavoritesListChanged);
		_lobbyInvite = Callback<LobbyInvite_t>.Create(OnLobbyInvite);
		_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
		_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
		_lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
		_lobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
		_lobbyGameCreated = Callback<LobbyGameCreated_t>.Create(OnLobbyGameCreated);
		_lobbyKicked = Callback<LobbyKicked_t>.Create(OnLobbyKicked);
		_favoritesListAccountsUpdated = Callback<FavoritesListAccountsUpdated_t>.Create(OnFavoritesListAccountsUpdated);
		_searchForGameProgressCallback = Callback<SearchForGameProgressCallback_t>.Create(OnSearchForGameProgressCallback);
		_searchForGameResultCallback = Callback<SearchForGameResultCallback_t>.Create(OnSearchForGameResultCallback);
		_requestPlayersForGameProgressCallback = Callback<RequestPlayersForGameProgressCallback_t>.Create(OnRequestPlayersForGameProgressCallback);
		_requestPlayersForGameResultCallback = Callback<RequestPlayersForGameResultCallback_t>.Create(OnRequestPlayersForGameResultCallback);
		_requestPlayersForGameFinalResultCallback = Callback<RequestPlayersForGameFinalResultCallback_t>.Create(OnRequestPlayersForGameFinalResultCallback);
		_submitPlayerResultResultCallback = Callback<SubmitPlayerResultResultCallback_t>.Create(OnSubmitPlayerResultResultCallback);
		_endGameResultCallback = Callback<EndGameResultCallback_t>.Create(OnEndGameResultCallback);
		_joinPartyCallback = Callback<JoinPartyCallback_t>.Create(OnJoinPartyCallback);
		_createBeaconCallback = Callback<CreateBeaconCallback_t>.Create(OnCreateBeaconCallback);
		_reservationNotificationCallback = Callback<ReservationNotificationCallback_t>.Create(OnReservationNotificationCallback);
		_changeNumOpenSlotsCallback = Callback<ChangeNumOpenSlotsCallback_t>.Create(OnChangeNumOpenSlotsCallback);
		_availableBeaconLocationsUpdated = Callback<AvailableBeaconLocationsUpdated_t>.Create(OnAvailableBeaconLocationsUpdated);
		_activeBeaconsUpdated = Callback<ActiveBeaconsUpdated_t>.Create(OnActiveBeaconsUpdated);

		_onLobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
		_onLobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
		_onLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

		//_onLobbyCreatedCallResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, 2));
    }

    public void RefreshLobbies()
    {
	    SteamMatchmaking.AddRequestLobbyListResultCountFilter(30);
	    var list = SteamMatchmaking.RequestLobbyList();
		_onLobbyMatchListCallResult.Set(list);
		Plugin.Log.LogInfo($"Requesting lobby list [{list}]");
    }

    // Below here are steam callbacks.
    
    private Callback<FavoritesListChanged_t> _favoritesListChanged;
    private Callback<LobbyInvite_t> _lobbyInvite;
    private Callback<LobbyEnter_t> _lobbyEnter;
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<LobbyChatMsg_t> _lobbyChatMsg;
    private Callback<LobbyGameCreated_t> _lobbyGameCreated;
    private Callback<LobbyKicked_t> _lobbyKicked;
    private Callback<FavoritesListAccountsUpdated_t> _favoritesListAccountsUpdated;
    private Callback<SearchForGameProgressCallback_t> _searchForGameProgressCallback;
    private Callback<SearchForGameResultCallback_t> _searchForGameResultCallback;
    private Callback<RequestPlayersForGameProgressCallback_t> _requestPlayersForGameProgressCallback;
    private Callback<RequestPlayersForGameResultCallback_t> _requestPlayersForGameResultCallback;
    private Callback<RequestPlayersForGameFinalResultCallback_t> _requestPlayersForGameFinalResultCallback;
    private Callback<SubmitPlayerResultResultCallback_t> _submitPlayerResultResultCallback;
    private Callback<EndGameResultCallback_t> _endGameResultCallback;
    private Callback<JoinPartyCallback_t> _joinPartyCallback;
    private Callback<CreateBeaconCallback_t> _createBeaconCallback;
    private Callback<ReservationNotificationCallback_t> _reservationNotificationCallback;
    private Callback<ChangeNumOpenSlotsCallback_t> _changeNumOpenSlotsCallback;
    private Callback<AvailableBeaconLocationsUpdated_t> _availableBeaconLocationsUpdated;
    private Callback<ActiveBeaconsUpdated_t> _activeBeaconsUpdated;

    private CallResult<LobbyEnter_t> _onLobbyEnterCallResult;
    private CallResult<LobbyMatchList_t> _onLobbyMatchListCallResult;
    private CallResult<LobbyCreated_t> _onLobbyCreatedCallResult;

    private void OnFavoritesListChanged(FavoritesListChanged_t pCallback) {
		Plugin.Log.LogInfo("[" + FavoritesListChanged_t.k_iCallback + " - FavoritesListChanged] - " + pCallback.m_nIP + " -- " + pCallback.m_nQueryPort + " -- " + pCallback.m_nConnPort + " -- " + pCallback.m_nAppID + " -- " + pCallback.m_nFlags + " -- " + pCallback.m_bAdd + " -- " + pCallback.m_unAccountId);
	}

	private void OnLobbyInvite(LobbyInvite_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyInvite_t.k_iCallback + " - LobbyInvite] - " + pCallback.m_ulSteamIDUser + " -- " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulGameID);
	}

	private void OnLobbyEnter(LobbyEnter_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyEnter_t.k_iCallback + " - LobbyEnter] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_rgfChatPermissions + " -- " + pCallback.m_bLocked + " -- " + pCallback.m_EChatRoomEnterResponse);

		_lobby = (CSteamID)pCallback.m_ulSteamIDLobby;
	}

	private void OnLobbyEnter(LobbyEnter_t pCallback, bool bIOFailure) {
		Plugin.Log.LogInfo("[" + LobbyEnter_t.k_iCallback + " - LobbyEnter] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_rgfChatPermissions + " -- " + pCallback.m_bLocked + " -- " + pCallback.m_EChatRoomEnterResponse);

		_lobby = (CSteamID)pCallback.m_ulSteamIDLobby;
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyDataUpdate_t.k_iCallback + " - LobbyDataUpdate] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDMember + " -- " + pCallback.m_bSuccess);
	}

	private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyChatUpdate_t.k_iCallback + " - LobbyChatUpdate] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDUserChanged + " -- " + pCallback.m_ulSteamIDMakingChange + " -- " + pCallback.m_rgfChatMemberStateChange);
	}

	private void OnLobbyChatMsg(LobbyChatMsg_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyChatMsg_t.k_iCallback + " - LobbyChatMsg] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDUser + " -- " + pCallback.m_eChatEntryType + " -- " + pCallback.m_iChatID);

		CSteamID SteamIDUser;
		byte[] Data = new byte[4096];
		EChatEntryType ChatEntryType;
		int ret = SteamMatchmaking.GetLobbyChatEntry((CSteamID)pCallback.m_ulSteamIDLobby, (int)pCallback.m_iChatID, out SteamIDUser, Data, Data.Length, out ChatEntryType);
		Plugin.Log.LogInfo("GetLobbyChatEntry(" + (CSteamID)pCallback.m_ulSteamIDLobby + ", " + (int)pCallback.m_iChatID + ", out SteamIDUser, Data, Data.Length, out ChatEntryType) : " + ret + " -- " + SteamIDUser + " -- " + System.Text.Encoding.UTF8.GetString(Data) + " -- " + ChatEntryType);
	}

	private void OnLobbyGameCreated(LobbyGameCreated_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyGameCreated_t.k_iCallback + " - LobbyGameCreated] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDGameServer + " -- " + pCallback.m_unIP + " -- " + pCallback.m_usPort);
	}

	private void OnLobbyMatchList(LobbyMatchList_t pCallback, bool bIOFailure) {
		Plugin.Log.LogInfo("[" + LobbyMatchList_t.k_iCallback + " - LobbyMatchList] - " + pCallback.m_nLobbiesMatching);
		for (var i = 0; i < pCallback.m_nLobbiesMatching; ++i)
		{
			var lobby = SteamMatchmaking.GetLobbyByIndex(i);
			
		}
	}

	private void OnLobbyKicked(LobbyKicked_t pCallback) {
		Plugin.Log.LogInfo("[" + LobbyKicked_t.k_iCallback + " - LobbyKicked] - " + pCallback.m_ulSteamIDLobby + " -- " + pCallback.m_ulSteamIDAdmin + " -- " + pCallback.m_bKickedDueToDisconnect);
	}

	private void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure) {
		Plugin.Log.LogInfo("[" + LobbyCreated_t.k_iCallback + " - LobbyCreated] - " + pCallback.m_eResult + " -- " + pCallback.m_ulSteamIDLobby);

		_lobby = (CSteamID)pCallback.m_ulSteamIDLobby;
	}

	private void OnFavoritesListAccountsUpdated(FavoritesListAccountsUpdated_t pCallback) {
		Plugin.Log.LogInfo("[" + FavoritesListAccountsUpdated_t.k_iCallback + " - FavoritesListAccountsUpdated] - " + pCallback.m_eResult);
	}

	private void OnSearchForGameProgressCallback(SearchForGameProgressCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + SearchForGameProgressCallback_t.k_iCallback + " - SearchForGameProgressCallback] - " + pCallback.m_ullSearchID + " -- " + pCallback.m_eResult + " -- " + pCallback.m_lobbyID + " -- " + pCallback.m_steamIDEndedSearch + " -- " + pCallback.m_nSecondsRemainingEstimate + " -- " + pCallback.m_cPlayersSearching);
	}

	private void OnSearchForGameResultCallback(SearchForGameResultCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + SearchForGameResultCallback_t.k_iCallback + " - SearchForGameResultCallback] - " + pCallback.m_ullSearchID + " -- " + pCallback.m_eResult + " -- " + pCallback.m_nCountPlayersInGame + " -- " + pCallback.m_nCountAcceptedGame + " -- " + pCallback.m_steamIDHost + " -- " + pCallback.m_bFinalCallback);
	}

	private void OnRequestPlayersForGameProgressCallback(RequestPlayersForGameProgressCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + RequestPlayersForGameProgressCallback_t.k_iCallback + " - RequestPlayersForGameProgressCallback] - " + pCallback.m_eResult + " -- " + pCallback.m_ullSearchID);
	}

	private void OnRequestPlayersForGameResultCallback(RequestPlayersForGameResultCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + RequestPlayersForGameResultCallback_t.k_iCallback + " - RequestPlayersForGameResultCallback] - " + pCallback.m_eResult + " -- " + pCallback.m_ullSearchID + " -- " + pCallback.m_SteamIDPlayerFound + " -- " + pCallback.m_SteamIDLobby + " -- " + pCallback.m_ePlayerAcceptState + " -- " + pCallback.m_nPlayerIndex + " -- " + pCallback.m_nTotalPlayersFound + " -- " + pCallback.m_nTotalPlayersAcceptedGame + " -- " + pCallback.m_nSuggestedTeamIndex + " -- " + pCallback.m_ullUniqueGameID);
	}

	private void OnRequestPlayersForGameFinalResultCallback(RequestPlayersForGameFinalResultCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + RequestPlayersForGameFinalResultCallback_t.k_iCallback + " - RequestPlayersForGameFinalResultCallback] - " + pCallback.m_eResult + " -- " + pCallback.m_ullSearchID + " -- " + pCallback.m_ullUniqueGameID);
	}

	private void OnSubmitPlayerResultResultCallback(SubmitPlayerResultResultCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + SubmitPlayerResultResultCallback_t.k_iCallback + " - SubmitPlayerResultResultCallback] - " + pCallback.m_eResult + " -- " + pCallback.ullUniqueGameID + " -- " + pCallback.steamIDPlayer);
	}

	private void OnEndGameResultCallback(EndGameResultCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + EndGameResultCallback_t.k_iCallback + " - EndGameResultCallback] - " + pCallback.m_eResult + " -- " + pCallback.ullUniqueGameID);
	}

	private void OnJoinPartyCallback(JoinPartyCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + JoinPartyCallback_t.k_iCallback + " - JoinPartyCallback] - " + pCallback.m_eResult + " -- " + pCallback.m_ulBeaconID + " -- " + pCallback.m_SteamIDBeaconOwner + " -- " + pCallback.m_rgchConnectString);
	}

	private void OnCreateBeaconCallback(CreateBeaconCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + CreateBeaconCallback_t.k_iCallback + " - CreateBeaconCallback] - " + pCallback.m_eResult + " -- " + pCallback.m_ulBeaconID);
	}

	private void OnReservationNotificationCallback(ReservationNotificationCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + ReservationNotificationCallback_t.k_iCallback + " - ReservationNotificationCallback] - " + pCallback.m_ulBeaconID + " -- " + pCallback.m_steamIDJoiner);
	}

	private void OnChangeNumOpenSlotsCallback(ChangeNumOpenSlotsCallback_t pCallback) {
		Plugin.Log.LogInfo("[" + ChangeNumOpenSlotsCallback_t.k_iCallback + " - ChangeNumOpenSlotsCallback] - " + pCallback.m_eResult);
	}

	private void OnAvailableBeaconLocationsUpdated(AvailableBeaconLocationsUpdated_t pCallback) {
		Plugin.Log.LogInfo("[" + AvailableBeaconLocationsUpdated_t.k_iCallback + " - AvailableBeaconLocationsUpdated]");
	}

	private void OnActiveBeaconsUpdated(ActiveBeaconsUpdated_t pCallback) {
		Plugin.Log.LogInfo("[" + ActiveBeaconsUpdated_t.k_iCallback + " - ActiveBeaconsUpdated]");
	}
}