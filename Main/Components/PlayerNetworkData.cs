using System;
using HarmonyLib;
using Rewired;
using UnityEngine;

namespace ThronefallMP.Components;

public class PlayerNetworkData : MonoBehaviour
{
    public struct Shared
    {
        public Vector3 Position { get; set; }
        public float MoveHorizontal { get; set; }
        public float MoveVertical { get; set; }
        public bool SprintToggleButton { get; set; }
        public bool SprintButton { get; set; }
        public bool InteractButton { get; set; }
        public bool CallNightButton { get; set; }
        public float CallNightFill { get; set; }
        public bool CommandUnitsButton { get; set; }
        
        public static bool operator ==(Shared a, Shared b) 
        {
            return a.Equals(b);
        }

        public static bool operator !=(Shared a, Shared b) 
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            var b = obj as Shared?;
            return b != null
                && Position == b.Value.Position
                && Math.Abs(MoveHorizontal - b.Value.MoveHorizontal) < 0.01f
                && Math.Abs(MoveVertical - b.Value.MoveVertical) < 0.01f
                && SprintToggleButton == b.Value.SprintToggleButton
                && SprintButton == b.Value.SprintButton
                && InteractButton == b.Value.InteractButton
                && CallNightButton == b.Value.CallNightButton
                && Math.Abs(CallNightFill - b.Value.CallNightFill) < 0.01f
                && CommandUnitsButton == b.Value.CommandUnitsButton;
        }

        public override int GetHashCode()
        {
            int hashCode = 648391;
            hashCode *= 37139213 ^ Position.GetHashCode();
            hashCode *= 174440041 ^ MoveHorizontal.GetHashCode();
            hashCode *= 17624813 ^ MoveVertical.GetHashCode();
            hashCode *= 9737333 ^ SprintToggleButton.GetHashCode();
            hashCode *= 7474967 ^ SprintButton.GetHashCode();
            hashCode *= 77557187 ^ InteractButton.GetHashCode();
            hashCode *= 1146581 ^ CallNightButton.GetHashCode();
            hashCode *= 840943 ^ CallNightFill.GetHashCode();
            hashCode *= 37027103 ^ CommandUnitsButton.GetHashCode();
            return hashCode;
        }
    }
    
    public int id;
    
    public bool IsLocal
    {
        get
        {
            var network = Plugin.Instance.Network;
            return !network.Online || network.LocalPlayer == id;
        }
    }

    // Shared variables
    public Shared SharedData;
    
    // Local variables
    public bool PlayerMovementSprintLast { get; set; }
    public bool PlayerSceptInteractLast { get; set; }
    public bool CallNightLast { get; set; }
    public bool CommandUnitsButtonLast { get; set; }
    public bool TeleportNext { get; set; }

    private void Update()
    {
        if (!IsLocal)
        {
            return;
        }
        
        var input = ReInput.players.GetPlayer(0);
        SharedData.MoveHorizontal = input.GetAxis("Move Horizontal");
        SharedData.MoveVertical = input.GetAxis("Move Vertical");
        SharedData.SprintToggleButton = input.GetButton("Sprint Toggle");
        SharedData.SprintButton = input.GetButton("Sprint");
        SharedData.InteractButton = input.GetButton("Interact");
        SharedData.CallNightButton = input.GetButton("Call Night");
        SharedData.CallNightFill = Traverse.Create(NightCall.instance).Field<float>("currentFill").Value;
        SharedData.CommandUnitsButton = input.GetButton("Command Units");
    }
}