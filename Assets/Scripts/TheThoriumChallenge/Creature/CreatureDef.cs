using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class CreatureDef : EntityDef
    {
        public float HpPerLevel { get; init; }
        public float MovementSpeedModifier { get; init; }
        public int CreatureHeight { get; init; }


        public CreatureDef() { }
        public CreatureDef(CreatureDef orig) : base(orig)
        {
            HpPerLevel = orig.HpPerLevel;
            MovementSpeedModifier = orig.MovementSpeedModifier;
        }
    }
}
