using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CompProperties_CtfCharacter : CompProperties
    {
        public CompProperties_CtfCharacter()
        {
            CompClass = typeof(Comp_CtfCharacter);
        }

        public Sprite Avatar { get; init; } = null;
        public float MaxActionPoints { get; init; } = 10;

        public float Speed { get; set; } = 0;
        public float Vision { get; set; } = 0;
        public float MaxStamina { get; set; } = 0;
        public float StaminaRegeneration { get; set; } = 0;

        public float ClimbingSpeedModifier { get; set; } = 0;
        public float SwimmingSpeedModifier { get; set; } = 0;
        public int Jumping { get; set; } = 0;
        public int Dropping { get; set; } = 0;

        public int Height { get; set; } = 0;
        public bool CanInteractWithDoors { get; set; } = false;

        public override CompProperties Clone()
        {
            return new CompProperties_CtfCharacter()
            {
                CompClass = this.CompClass,
                Avatar = this.Avatar,
                MaxActionPoints = this.MaxActionPoints,

                Speed = this.Speed,
                Vision = this.Vision,
                MaxStamina = this.MaxStamina,
                StaminaRegeneration = this.StaminaRegeneration,
                ClimbingSpeedModifier = this.ClimbingSpeedModifier,
                SwimmingSpeedModifier = this.SwimmingSpeedModifier,
                Jumping = this.Jumping,
                Dropping = this.Dropping,
                Height = this.Height,
                CanInteractWithDoors = this.CanInteractWithDoors,
            };
        }
    }
}
