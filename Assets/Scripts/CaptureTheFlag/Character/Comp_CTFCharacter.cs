using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Comp_CtfCharacter : EntityComp
    {
        private CompProperties_CtfCharacter Props => (CompProperties_CtfCharacter)props;

        public Sprite Avatar => Props.Avatar;
        public float MaxActionPoints => Props.MaxActionPoints;
        public bool CanUseDoors => Props.CanUseDoors;
    }
}
