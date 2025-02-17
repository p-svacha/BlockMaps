using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Ability_Move : Ability
    {
        public override string Label => "Move";
        public override string Description => "Move to an adjacent tile.";
        public override int BaseCost => 60;

        public override HashSet<BlockmapNode> GetPossibleTargets()
        {
            HashSet<BlockmapNode> targets = new HashSet<BlockmapNode>();
            foreach(Direction dir in HelperFunctions.GetSides())
            {
                if (OriginNode.WalkTransitions.TryGetValue(dir, out Transition t) && t.CanPass(Creature))
                {
                    if (t.To.Entities.Any(e => e is Creature)) continue;
                    targets.Add(t.To);
                }
            }

            return targets;
        }

        public override HashSet<BlockmapNode> GetImpactedNodes(BlockmapNode target)
        {
            return new HashSet<BlockmapNode>() { target };
        }

        public override int GetCost(BlockmapNode target)
        {
            float cost = BaseCost;
            cost *= OriginNode.WalkTransitions.First(t => t.Value.To == target).Value.GetMovementCost(Creature);
            cost /= Creature.GetStat(StatDefOf.MovementSpeed);

            return Mathf.RoundToInt(cost);
        }

        public override void OnPerform(BlockmapNode target)
        {
            if (Creature.IsVisibleBy(Game.Instance.CurrentStage.LocalPlayer))
            {
                Creature.MovementComp.OnTargetReached += OnMovementDone;
                Creature.MovementComp.MoveTo(target);
            }
            else
            {
                Creature.Teleport(target, newRotation: GetDirectionToTarget(target));
                Finish();
            }
        }

        private Direction GetDirectionToTarget(BlockmapNode target)
        {
            return OriginNode.WalkTransitions.First(t => t.Value.To == target).Key;
        }

        private void OnMovementDone()
        {
            Creature.MovementComp.OnTargetReached -= OnMovementDone;
            Finish();
        }
    }
}
