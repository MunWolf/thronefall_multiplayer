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
            
            return Math.Abs(MoveHorizontal - b.MoveHorizontal) < 0.01f
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
            return (
                MoveHorizontal,
                MoveVertical,
                SprintToggleButton,
                SprintButton,
                InteractButton,
                CallNightButton,
                CallNightFill,
                CommandUnitsButton
            ).GetHashCode();
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