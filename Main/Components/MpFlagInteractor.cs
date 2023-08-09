using System;
using Rewired;
using ThronefallMP.Network;
using ThronefallMP.UI;
using UnityEngine;

namespace ThronefallMP.Components;

public class MpFlagInteractor : MonoBehaviour
{
    public GameObject Indicator { get; set; }
    public float InteractionDistance { get; set; }

    private Player _input;
    
    public void Start()
    {
        _input = ReInput.players.GetPlayer(0);
    }
    
    public void Update()
    {
        var data = Plugin.Instance.Network.LocalPlayerData;
        if (data == null)
        {
            return;
        }

        var position = data.transform.position;
        var inside = (position - transform.position).sqrMagnitude <= InteractionDistance * InteractionDistance;
        Indicator.SetActive(inside);
        if (inside && !LocalGamestate.Instance.PlayerFrozen && _input.GetButtonDown("Interact"))
        {
            //UIManager.OpenNetworkPanel();
        }
    }
}