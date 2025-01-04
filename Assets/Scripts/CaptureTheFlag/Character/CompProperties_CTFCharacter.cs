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
        public bool CanUseDoors { get; init; } = true;

        public override CompProperties Clone()
        {
            return new CompProperties_CtfCharacter()
            {
                CompClass = this.CompClass,
                Avatar = this.Avatar,
                MaxActionPoints = this.MaxActionPoints,
                CanUseDoors = this.CanUseDoors,
            };
        }
    }
}
