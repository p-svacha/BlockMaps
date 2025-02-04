using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Ability001_Bite : Ability
    {
        public override string Label => "Bite";
        public override string Description => "Deal 60% of Bite Strength as Pierce Damage to an adjacent creature.";
        public override int Cost => 60;
    }
}
