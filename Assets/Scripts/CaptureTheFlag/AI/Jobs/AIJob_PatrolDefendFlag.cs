using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_PatrolDefendFlag : AICharacterJob
    {
        private BlockmapNode TargetNode;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.PatrolDefendFlag;
        public override string DevmodeDisplayText => "Patrolling Flag --> " + TargetNode.ToStringShort();

        public AIJob_PatrolDefendFlag(Character c) : base(c)
        {
            // Find a target node near own flag
            int numAttempts = 0;
            int maxAttempts = 50;
            do
            {
                TargetNode = Player.DefendPerimeterNodes[Random.Range(0, Player.DefendPerimeterNodes.Count)];
            }
            while (Pathfinder.GetPath(Character.Entity, Character.Node, TargetNode, forbiddenNodes: Player.FlagZone.Nodes) == null && numAttempts++ < maxAttempts);

            if (numAttempts >= maxAttempts && Game.DevMode) Debug.LogError("No valid node found within defend perimeter for " + Character.Name + " after " + numAttempts + " attempts.");
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

            // If we see a nearby opponent, chase them
            if (Player.ShouldChaseCharacterToTag(Character, out Character target))
            {
                forcedNewJob = new AIJob_TagOpponent(Character, target);
                return true;
            }

            // If we are on or close to our target node, look for new job
            if (Character.Entity.OriginNode == TargetNode || Character.Entity.OriginNode.TransitionsByTarget.ContainsKey(TargetNode)) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(TargetNode);
        }
    }
}
