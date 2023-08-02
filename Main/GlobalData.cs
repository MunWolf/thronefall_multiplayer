using ThronefallMP.NetworkPackets;
using UnityEngine.PlayerLoop;

namespace ThronefallMP;

public struct InternalGlobalData
{
    public int Balance;
    public int Networth;
}

public static class GlobalData
{
    public static InternalGlobalData Internal;
    
    // TODO: Add a set from Balance -> Player.balance in
    //       TutorialManager.Update
    //       DebugCoinDisplay.Update
    //       LocalGamestate.WaitThenTriggerEndOfMatchScreen
    
    public static int Balance
    {
        get => Internal.Balance;
        set
        {
            var delta = value - Internal.Balance;
            var packet = new BalancePacket { Delta = delta };
            Plugin.Instance.Network.Send(packet, true);
        }
    }
    
    public static int Networth => Internal.Networth;
}