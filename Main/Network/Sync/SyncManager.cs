using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Network.Packets;

namespace ThronefallMP.Network.Sync;

public static class SyncManager
{
    private static readonly List<BaseSync> RegisteredSyncs = new();
    private static readonly List<BaseTargetSync> RegisteredTargetSyncs = new();
    
    public static void RegisterSync(BaseSync sync)
    {
        RegisteredSyncs.Add(sync);
    }
    
    public static void RegisterSync(BaseTargetSync sync)
    {
        RegisteredTargetSyncs.Add(sync);
    }
    
    public static void UpdateSyncs()
    {
        foreach (var sync in RegisteredSyncs)
       {
           sync.Update();
       }
        
        foreach (var sync in RegisteredTargetSyncs)
        {
           sync.Update();
        }
    }

    public static void ResetSyncs()
    {
        foreach (var sync in RegisteredSyncs)
        {
            sync.Reset();
        }
        
        foreach (var sync in RegisteredTargetSyncs)
        {
            sync.Reset();
        }
    }

    public static void OnConnected(CSteamID id)
    {
        foreach (var sync in RegisteredSyncs)
        {
            sync.OnConnected(id);
        }
        
        foreach (var sync in RegisteredTargetSyncs)
        {
            sync.OnConnected(id);
        }
    }

    public static bool HandlePacket(SteamNetworkingIdentity sender, BasePacket packet)
    {
        foreach (var sync in RegisteredSyncs)
        {
            if (!sync.CanHandle(packet))
            {
                continue;
            }
            
            sync.Handle(sender.GetSteamID(), packet);
            return true;
        }
        
        foreach (var sync in RegisteredTargetSyncs)
        {
            if (!sync.CanHandle(packet))
            {
                continue;
            }
            
            sync.Handle(sender.GetSteamID(), packet);
            return true;
        }
        
        return false;
    }
}