using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using ThronefallMP.Network.Packets;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public abstract class BaseSync
{
    private class State
    {
        public BasePacket LastPacket;
        public float ForceTimer;
        public float MinWaitTimer;
    }
    
    private readonly Dictionary<CSteamID, State> _states = new();
    private readonly State _state = new();
    
    protected BaseSync()
    {
        SyncManager.RegisterSync(this);
    }

    protected virtual bool CaresAboutPeer => false;
    protected virtual float ForceUpdateTimer => float.MaxValue;
    protected virtual float MinWaitTimer => 0f;
    protected virtual bool ShouldUpdate => Plugin.Instance.Network.Server;
    protected abstract BasePacket CreateSyncPacket(CSteamID peer);
    protected abstract bool Compare(CSteamID peer, BasePacket current, BasePacket last);
    protected abstract void HandlePacket(CSteamID peer, BasePacket packet);
    
    public abstract bool CanHandle(BasePacket packet);
    public void Handle(CSteamID peer, BasePacket packet)
    {
        HandlePacket(peer, packet);
        // If we received a packet from someone else, assume that update was sent to everyone.
        foreach (var state in _states)
        {
            state.Value.LastPacket = packet;
        }
    }

    private BasePacket PacketToSend(State state, CSteamID peer)
    {
        state.ForceTimer += Time.deltaTime;
        state.MinWaitTimer += Time.deltaTime;
        if (state.MinWaitTimer < MinWaitTimer)
        {
            return null;
        }

        var current = CreateSyncPacket(peer);
        if (state.ForceTimer < ForceUpdateTimer &&
            state.LastPacket != null &&
            Compare(peer, current, state.LastPacket))
        {
            state.LastPacket = current;
            return null;
        }
        
        state.LastPacket = current;
        state.LastPacket = current;
        state.ForceTimer = 0;
        state.MinWaitTimer = 0;
        return current;
    }
    
    public void Update()
    {
        if (!ShouldUpdate)
        {
            return;
        }

        if (CaresAboutPeer)
        {
            var id = new SteamNetworkingIdentity();
            foreach (var peer in Plugin.Instance.Network.Peers)
            {
                if (!_states.TryGetValue(peer, out var last))
                {
                    last = new State();
                    _states[peer] = last;
                }

                var packet = PacketToSend(last, peer);
                if (packet != null)
                {
                    id.SetSteamID(peer);
                    Plugin.Instance.Network.SendSingle(packet, id);
                }
            }
        }
        else
        {
            var packet = PacketToSend(_state, CSteamID.Nil);
            if (packet != null)
            {
                Plugin.Instance.Network.Send(packet);
            }
        }
    }
    
    public void Reset()
    {
        _states.Clear();
    }

    public void OnConnected(CSteamID id)
    {
        var state = CaresAboutPeer ? _states.GetValueSafe(id) : _state;
        if (state == null)
        {
            return;
        }
        
        state.ForceTimer = 0;
        state.MinWaitTimer = 0;
        state.LastPacket = null;
    }
}