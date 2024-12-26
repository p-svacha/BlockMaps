using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CompProperties_CTFCharacter : CompProperties
    {
        public CompProperties_CTFCharacter()
        {
            CompClass = typeof(Comp_CTFCharacter);
        }

        public Sprite Avatar { get; init; } = null;

        public float MaxActionPoints { get; init; } = 0;
        public float MaxStamina { get; init; } = 0;
        public float StaminaRegeneration { get; init; } = 0;
        public float MovementSkill { get; init; } = 0;
        public bool CanInteractWithDoors { get; init; } = false;
    }
}
