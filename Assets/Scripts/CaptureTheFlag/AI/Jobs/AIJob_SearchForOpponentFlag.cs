using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_SearchForOpponentFlag : AICharacterJob
    {
        private BlockmapNode TargetNode;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.SearchOpponentFlag;
        public override string DevmodeDisplayText => "Searching Flag (going to " + TargetNode.ToStringShort() + ")";

        public AIJob_SearchForOpponentFlag(Character c) : base(c)
        {
            // Find a target node
            List<BlockmapNode> candidates = Game.LocalPlayerZone.Nodes.Where(x => !x.IsExploredBy(Player.Actor) && x.IsPassable(Character.Entity)).ToList();
            BlockmapNode targetNode = candidates[Random.Range(0, candidates.Count)];
            TargetNode = targetNode;
        }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            // If we can tag an opponent this turn, do that
            if (Player.CanTagCharacterDirectly(Character, out Character target0))
            {
                forcedNewJob = new AIJob_TagOpponent(Character, target0);
                return true;
            }

            // If we should flee, do that
            if(Player.ShouldFlee(Character))
            {
                forcedNewJob = new AIJob_Flee(Character);
                return true;
            }

            // If we are on or close to our target node, look for new job
            if (Character.Entity.OriginNode == TargetNode || Character.Entity.OriginNode.Transitions.ContainsKey(TargetNode)) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(TargetNode);
        }
    }
}
