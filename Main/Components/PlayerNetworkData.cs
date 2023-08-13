using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UniverseLib;

namespace ThronefallMP.Components;

public class PlayerNetworkData : MonoBehaviour
{
    public class Shared
    {
        public float MoveHorizontal;
        public float MoveVertical;
        public bool SprintToggleButton;
        public bool SprintButton;
        public bool InteractButton;
        public bool CallNightButton;
        public float CallNightFill;
        public bool CommandUnitsButton;
        
        public void Set(Shared a)
        {
            MoveHorizontal = a.MoveHorizontal;
            MoveVertical = a.MoveVertical;
            SprintToggleButton = a.SprintToggleButton;
            SprintButton = a.SprintButton;
            InteractButton = a.InteractButton;
            CallNightButton = a.CallNightButton;
            CallNightFill = a.CallNightFill;
            CommandUnitsButton = a.CommandUnitsButton;
        }

        public bool Compare(Shared b)
        {
            return Math.Abs(MoveHorizontal - b.MoveHorizontal) < Helpers.Epsilon
                && Math.Abs(MoveVertical - b.MoveVertical) < Helpers.Epsilon
                && SprintToggleButton == b.SprintToggleButton
                && SprintButton == b.SprintButton
                && InteractButton == b.InteractButton
                && CallNightButton == b.CallNightButton
                && Math.Abs(CallNightFill - b.CallNightFill) < Helpers.Epsilon
                && CommandUnitsButton == b.CommandUnitsButton;
        }
    }
    
    public int id;
    public Network.PlayerManager.Player Player;
    
    public bool IsLocal => Plugin.Instance.PlayerManager.LocalId == id;

    // Shared variables
    public Shared SharedData = new();
    
    // Local variables
    public bool PlayerMovementSprintLast { get; set; }
    public bool PlayerScepterInteractLast { get; set; }
    public bool CallNightLast { get; set; }
    public bool CommandUnitsButtonLast { get; set; }

    private void Update()
    {
        if (!IsLocal)
        {
            return;
        }

        var frozen = LocalGamestate.Instance.PlayerFrozen;
        var input = ReInput.players.GetPlayer(0);
        SharedData.MoveHorizontal = frozen ? 0f : input.GetAxis("Move Horizontal");
        SharedData.MoveVertical = frozen ? 0f : input.GetAxis("Move Vertical");
        SharedData.SprintToggleButton = !frozen && input.GetButton("Sprint Toggle");
        SharedData.SprintButton = !frozen && input.GetButton("Sprint");
        SharedData.InteractButton = !frozen && input.GetButton("Interact");
        SharedData.CallNightButton = !frozen && input.GetButton("Call Night");
        SharedData.CallNightFill = frozen ? 0f : Traverse.Create(NightCall.instance).Field<float>("currentFill").Value;
        SharedData.CommandUnitsButton = !frozen && input.GetButton("Command Units");
    }
}