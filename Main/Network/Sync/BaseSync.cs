using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Network.Packets;
using ThronefallMP.Network.Packets.Sync;
using UnityEngine;

namespace ThronefallMP.Network.Sync;

public abstract class BaseSync
{
    protected enum Mode
    {
        Auto,
        Request
    }
    
    private static readonly Dictionary<int, BaseSync> RegisteredSyncs = new();
    protected virtual float SyncCheckTimer => 0.5f;
    protected virtual Mode SyncMode => Mode.Auto;

    private readonly Dictionary<CSteamID, int> _lastSyncCheck = new();
    private readonly Dictionary<CSteamID, int> _lastHash = new();
    private readonly int _type;
    private int _nextOrder;
    private float _syncTimer;

    protected BaseSync(int type)
    {
        _type = type;
        RegisteredSyncs[type] = this;
    }

    protected abstract int Hash(CSteamID sender);
    protected abstract bool CanHandle(BasePacket packet);
    protected abstract void Handle(CSteamID sender, BasePacket packet);
    protected abstract BasePacket CreateSyncPacket(CSteamID sender);

    protected virtual IEnumerable<(CSteamID peer, BasePacket packet)> CreateSyncPackets(IEnumerable<CSteamID> ids)
    {
        var packet = CreateSyncPacket(CSteamID.Nil);
        foreach (var id in ids)
        {
            yield return (id, packet);
        }
    }

    private void HandleSyncCheck(SteamNetworkingIdentity sender, int order, int hash)
    {
        var steamId = sender.GetSteamID();
        if (_lastSyncCheck.TryGetValue(steamId, out var lastOrder))
        {
            lastOrder = -1;
        }
        
        if (lastOrder >= order)
        {
            return;
        }

        _lastSyncCheck[steamId] = order;
        if (Hash(steamId) == hash)
        {
            return;
        }
        
        Plugin.Instance.Network.SendSingle(CreateSyncPacket(steamId), sender);
    }
    
    private void Update()
    {
        if (SyncMode == Mode.Auto && Plugin.Instance.Network.Server)
        {
            var peersNeedingUpdate = new List<CSteamID>();
            foreach (var peer in Plugin.Instance.Network.Peers)
            {
                var current = Hash(peer);
                if (_lastHash.TryGetValue(peer, out var last) && current == last)
                {
                    return;
                }
        
                peersNeedingUpdate.Add(peer);
                _lastHash[peer] = current;
            }

            var id = new SteamNetworkingIdentity();
            foreach (var data in CreateSyncPackets(peersNeedingUpdate))
            {
                id.SetSteamID(data.peer);
                Plugin.Instance.Network.SendSingle(data.packet, id);
            }
        }
        else if (!Plugin.Instance.Network.Server)
        {
            _syncTimer += Time.deltaTime;
            if (_syncTimer < SyncCheckTimer)
            {
                return;
            }

            _syncTimer = 0.0f;
            var packet = new SyncCheckPacket()
            {
                Hash = Hash(Network.SteamId),
                Order = _nextOrder++,
                Type = _type
            };
            Plugin.Instance.Network.Send(packet);
        }
    }
    
    private void Reset()
    {
        _syncTimer = 0.0f;
        _nextOrder = 0;
        _lastSyncCheck.Clear();
    }

    public static bool HandlePacket(SteamNetworkingIdentity sender, BasePacket packet)
    {
        if (packet is SyncCheckPacket syncCheckPacket)
        {
            if (RegisteredSyncs.TryGetValue(syncCheckPacket.Type, out var sync))
            {
                sync.HandleSyncCheck(sender, syncCheckPacket.Order, syncCheckPacket.Hash);
            }
            
            return true;
        }
        
        foreach (var sync in RegisteredSyncs)
        {
            if (!sync.Value.CanHandle(packet))
            {
                continue;
            }
            
            sync.Value.Handle(sender.GetSteamID(), packet);
            return true;
        }
        
        return false;
    }
    
    public static void UpdateSyncs()
    {
        foreach (var sync in RegisteredSyncs)
        {
            sync.Value.Update();
        }
    }

    public static void ResetSyncs()
    {
        foreach (var sync in RegisteredSyncs)
        {
            sync.Value.Reset();
        }
    }
}