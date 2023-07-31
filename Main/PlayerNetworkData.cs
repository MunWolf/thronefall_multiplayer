using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

namespace ThronefallMP;

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
                && InteractButton == b.Value.InteractButton;
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
    public bool PlayerMovementSprintToggle { get; set; }
    public bool PlayerSceptInteract { get; set; }
    public bool TeleportNext { get; set; }

    private void Update()
    {
        if (IsLocal)
        {
            var input = ReInput.players.GetPlayer(0);
            SharedData.MoveHorizontal = input.GetAxis("Move Horizontal");
            SharedData.MoveVertical = input.GetAxis("Move Vertical");
            SharedData.SprintToggleButton = input.GetButton("Sprint Toggle");
            SharedData.SprintButton = input.GetButton("Sprint");
            SharedData.InteractButton = input.GetButton("Interact");
        }
    }
}