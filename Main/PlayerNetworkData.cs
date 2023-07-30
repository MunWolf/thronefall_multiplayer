using UnityEngine;

namespace ThronefallMP;

public class PlayerNetworkData : MonoBehaviour
{
    public int id;
    
    public bool IsLocal
    {
        get
        {
            var network = Plugin.Instance.Network;
            return !network.Online || network.LocalPlayer == id;
        }
    }
}