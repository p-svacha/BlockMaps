using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Comp_CTFCharacter : EntityComp
    {
        public CompProperties_CTFCharacter Props => (CompProperties_CTFCharacter)props;

        #region Getters

        public Sprite Avatar => Props.Avatar;
        public float MaxActionPoints => Props.MaxActionPoints;
        public float MaxStamina => Props.MaxStamina;
        public float StaminaRegeneration => Props.StaminaRegeneration;
        public float MovementSkill => Props.MovementSkill;

        #endregion
    }
}
