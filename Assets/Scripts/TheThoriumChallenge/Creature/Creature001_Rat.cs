using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Creature001_Rat : Creature
    {
        // Stats
        public override string Label => "Rat";
        public override float BaseHpPerLevel => 3;
        public override float BaseMovementSpeedModifier => 1.2f;

        // Looks
        protected override GameObject Model => Resources.Load<GameObject>(ModelPath + "Rat");
        public override int CreatureHeight => 1;
    }
}
