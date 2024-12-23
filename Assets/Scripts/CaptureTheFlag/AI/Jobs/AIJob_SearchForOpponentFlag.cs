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
        private NavigationPath TargetPath;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.SearchOpponentFlag;
        public override string DevmodeDisplayText => "Searching Flag (going to " + TargetNode + ")";

        public AIJob_SearchForOpponentFlag(CtfCharacter c) : base(c)
        {
            // Find a target node
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Game.LocalPlayerZone.Nodes.Where(x => !x.IsExploredBy(Player.Actor) && x.IsPassable(Character)).ToList();
                BlockmapNode targetNode = candidates[Random.Range(0, candidates.Count)];
                NavigationPath targetPath = GetPath(targetNode);
                if(targetPath != null) // valid target that we can reach
                {
                    TargetNode = targetNode;
                    TargetPath = targetPath;
                }
            }
        }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            // Check if our path is still valid
            if (TargetPath == null) return true;
            if (!TargetPath.CanPass(Character)) return true;

            // If we can tag an opponent this turn, do that
            if (Player.CanTagCharacterDirectly(Character, out CtfCharacter target0))
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
            if (Character.OriginNode == TargetNode || Character.OriginNode.TransitionsByTarget.ContainsKey(TargetNode)) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(TargetNode, TargetPath);
        }
    }
}
