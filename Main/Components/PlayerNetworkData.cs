using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.Serialization;
using UniverseLib;

namespace ThronefallMP.Components;

public class PlayerNetworkData : MonoBehaviour
{
    public class Shared
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
            var isANull = a.ReferenceEqual(null);
            var isBNull = b.ReferenceEqual(null);
            if (isANull && isBNull)
            {
                return true;
            }
            
            var output = a?.Equals(b);
            return output.HasValue && output.Value;
        }

        public static bool operator !=(Shared a, Shared b)
        {
            var isANull = a.ReferenceEqual(null);
            var isBNull = b.ReferenceEqual(null);
            if (isANull && isBNull)
            {
                return false;
            }
            
            var output = a?.Equals(b);
            return !(output.HasValue && output.Value);
        }

        public void Set(Shared a)
        {
            Position = a.Position;
            MoveHorizontal = a.MoveHorizontal;
            MoveVertical = a.MoveVertical;
            SprintToggleButton = a.SprintToggleButton;
            SprintButton = a.SprintButton;
            InteractButton = a.InteractButton;
            CallNightButton = a.CallNightButton;
            CallNightFill = a.CallNightFill;
            CommandUnitsButton = a.CommandUnitsButton;
        }

        public override bool Equals(object obj)
        {
            var b = obj as Shared;
            if (b.ReferenceEqual(null))
            {
                return false;
            }
            
            return Position == b.Position
                && Math.Abs(MoveHorizontal - b.MoveHorizontal) < 0.01f
                && Math.Abs(MoveVertical - b.MoveVertical) < 0.01f
                && SprintToggleButton == b.SprintToggleButton
                && SprintButton == b.SprintButton
                && InteractButton == b.InteractButton
                && CallNightButton == b.CallNightButton
                && Math.Abs(CallNightFill - b.CallNightFill) < 0.01f
                && CommandUnitsButton == b.CommandUnitsButton;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hashCode = 648391;
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
    public Network.PlayerManager.Player Player;
    
    public bool IsLocal => Plugin.Instance.PlayerManager.LocalId == id;

    // Shared variables
    public Shared SharedData = new();
    
    // Local variables
    public bool PlayerMovementSprintLast { get; set; }
    public bool PlayerScepterInteractLast { get; set; }
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