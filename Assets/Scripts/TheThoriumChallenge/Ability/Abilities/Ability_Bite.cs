using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Ability_Bite : Ability
    {
        public override string Label => "Bite";
        public override string Description => "Deal 100% of Bite Strength as Pierce Damage to an adjacent creature.";
        public override int BaseCost => 60;

        public override HashSet<BlockmapNode> GetPossibleTargets()
        {
            return AdjacentNodesWithEnemies;
        }
        public override HashSet<BlockmapNode> GetImpactedNodes(BlockmapNode target)
        {
            return new HashSet<BlockmapNode>() { target };
        }
        public override int GetCost(BlockmapNode target)
        {
            return BaseCost;
        }
        public override void OnPerform(BlockmapNode target)
        {
            Creature targetCreature = target.GetCreature();
            float damage = 1.0f * Creature.GetStat(StatDefOf.BiteStrength);

            targetCreature.ApplyDamage(new DamageInfo(Creature, targetCreature, DamageTypeDefOf.Pierce, damage), onHealthChangeDoneCallback: Finish);
        }
    }
}
