﻿using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Components;
using ThronefallMP.Network.Packets;
using UnityEngine;
using SteamNetworkingIdentity = Steamworks.SteamNetworkingIdentity;

namespace ThronefallMP.Network.Sync;

public abstract class BaseTargetSync
{
    private class State
    {
        public BasePacket LastPacket;
        public float ForceTimer;
        public float MinWaitTimer;
    }
    
    private readonly Dictionary<(CSteamID, IdentifierData), State> _states = new();
    
    protected BaseTargetSync()
    {
        SyncManager.RegisterSync(this);
    }

    protected virtual bool HandleDisabledTargets => false;
    protected virtual bool CaresAboutPeer => false;
    protected virtual float ForceUpdateTimer => float.MaxValue;
    protected virtual float MinWaitTimer => 0f;
    protected virtual bool ShouldUpdate => Plugin.Instance.Network.Server;
    protected abstract IEnumerable<(IdentifierData id, GameObject target)> Targets();
    protected abstract BasePacket CreateSyncPacket(CSteamID peer, IdentifierData id, GameObject target);
    protected abstract bool Compare(CSteamID peer, IdentifierData id, GameObject target, BasePacket current, BasePacket last);
    
    public abstract bool CanHandle(BasePacket packet);
    public abstract void Handle(CSteamID peer, BasePacket packet);

    // Used to filter targets for sending to specific ids.
    protected virtual bool Filter(CSteamID peer, IdentifierData id, GameObject target)
    {
        return false;
    }

    private BasePacket PacketToSend(State state, CSteamID peer, IdentifierData id, GameObject target)
    {
        state.ForceTimer += Time.deltaTime;
        state.MinWaitTimer += Time.deltaTime;
        if (state.MinWaitTimer < MinWaitTimer)
        {
            return null;
        }

        var current = CreateSyncPacket(peer, id, target);
        if (state.ForceTimer < ForceUpdateTimer &&
            state.LastPacket != null &&
            Compare(peer, id, target, current, state.LastPacket))
        {
            return null;
        }
        
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
            foreach (var data in Targets())
            {
                if (!HandleDisabledTargets && !data.target.activeInHierarchy)
                {
                    continue;
                }
                
                foreach (var peer in Plugin.Instance.Network.Peers)
                {
                    if (Filter(peer, data.id, data.target))
                    {
                        continue;
                    }
                    
                    if (!_states.TryGetValue((peer, data.id), out var last))
                    {
                        last = new State();
                        _states[(peer, data.id)] = last;
                    }

                    var packet = PacketToSend(last, peer, data.id, data.target);
                    if (packet != null)
                    {
                        id.SetSteamID(peer);
                        Plugin.Instance.Network.SendSingle(packet, id);
                    }
                }
            }
        }
        else
        {
            foreach (var data in Targets())
            {
                if (!HandleDisabledTargets && (data.target == null || !data.target.activeInHierarchy))
                {
                    continue;
                }
                
                if (!_states.TryGetValue((CSteamID.Nil, data.id), out var last))
                {
                    last = new State();
                    _states[(CSteamID.Nil, data.id)] = last;
                }

                var packet = PacketToSend(last, CSteamID.Nil, data.id, data.target);
                if (packet != null)
                {
                    var id = new SteamNetworkingIdentity();
                    foreach (var peer in Plugin.Instance.Network.Peers)
                    {
                        if (!Filter(peer, data.id, data.target))
                        {
                            id.SetSteamID(peer);
                            Plugin.Instance.Network.SendSingle(packet, id);
                        }
                    }
                }
            }
        }
    }
    
    public void Reset()
    {
        _states.Clear();
    }

    public void OnConnected(CSteamID id)
    {
        foreach (var data in _states)
        {
            if (CaresAboutPeer && data.Key.Item1 != id)
            {
                continue;
            }
            
            data.Value.ForceTimer = 0;
            data.Value.MinWaitTimer = 0;
            data.Value.LastPacket = null;
        }
    }
}