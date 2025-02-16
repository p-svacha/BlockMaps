using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Ability_Bite : Ability
    {
        public override string Label => "Bite";
        public override string Description => "Deal 60% of Bite Strength as Pierce Damage to an adjacent creature.";
        public override int BaseCost => 60;

        public override List<BlockmapNode> GetPossibleTargets()
        {
            throw new System.NotImplementedException();
        }
        public override int GetCost(BlockmapNode target)
        {
            throw new System.NotImplementedException();
        }
        public override void OnPerform(BlockmapNode target)
        {
            throw new System.NotImplementedException();
        }
    }
}
