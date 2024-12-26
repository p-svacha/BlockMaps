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
        private NavigationPath TargetPath;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.PatrolDefendFlag;
        public override string DevmodeDisplayText => "Patrolling Flag --> " + TargetNode;

        public AIJob_PatrolDefendFlag(CtfCharacter c) : base(c)
        {
            if (Player.DefendPerimeterNodes.Count == 0) return;

            // Find a target node near own flag
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                BlockmapNode targetNode = Player.DefendPerimeterNodes[Random.Range(0, Player.DefendPerimeterNodes.Count)];
                NavigationPath targetPath = GetPath(targetNode);
                if (targetPath != null) // valid target that we can reach
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

            // If we see a nearby opponent, chase them
            if (Player.ShouldChaseCharacterToTag(Character, out CtfCharacter target))
            {
                forcedNewJob = new AIJob_TagOpponent(Character, target);
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
