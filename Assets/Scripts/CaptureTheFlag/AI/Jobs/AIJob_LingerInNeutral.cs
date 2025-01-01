using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    /// <summary>
    /// A job where characters move around in the neutral area until it's either safe to cross into opponent territory, or see an opponent in the own territory.
    /// </summary>
    public class AIJob_LingerInNeutral : AICharacterJob
    {
        private float MAX_DEFEND_CHASE_DISTANCE = 25;

        private BlockmapNode TargetNode;
        private NavigationPath TargetPath;

        private AICharacterJob NextJob;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.LingerInNeutral;
        public override string DevmodeDisplayText => $"LingerInNeutral";

        public AIJob_LingerInNeutral(CtfCharacter c) : base(c)
        {
            SetNewTargetNode();
        }

        /// <summary>
        /// Sets the target node as a random reachable node in the neutral zone
        /// </summary>
        private void SetNewTargetNode()
        {
            // Find a target node
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Match.NeutralZone.Nodes.ToList();
                BlockmapNode targetNode = candidates[Random.Range(0, candidates.Count)];
                NavigationPath targetPath = GetPath(targetNode);
                if (targetPath != null) // valid target that we can reach
                {
                    TargetNode = targetNode;
                    TargetPath = targetPath;
                }
            }
        }

        public override void OnNextActionRequested()
        {
            // Set new target if we reached previous target
            if (IsOnOrNear(TargetNode)) SetNewTargetNode();

            // Set new target if path is no longer valid
            bool isTargetStillValid = (TargetPath != null && TargetPath.CanPass(Character));
            if (!isTargetStillValid) SetNewTargetNode();
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we can tag an opponent directly this turn, do that
            if (CanTagCharacterDirectly(out CtfCharacter target0))
            {
                Log($"Switching from {Id} to ChaseToTagOpponent because we can reach {target0.LabelCap} directly this turn.");
                return new AIJob_ChaseToTagOpponent(Character, target0);
            }

            // If there is an opponent or position to check nearby in our own territory, go to that
            if (ShouldChaseOrSearchOpponent(MAX_DEFEND_CHASE_DISTANCE, out CtfCharacter target, out AICharacterJobId jobId))
            {
                if (jobId == AICharacterJobId.CaptureOpponentFlag)
                {
                    Log($"Switching from {Id} to ChaseToTagOpponent because {target.LabelCap} is nearby.");
                    return new AIJob_ChaseToTagOpponent(Character, target);
                }
                else if (jobId == AICharacterJobId.SearchOpponentInOwnTerritory)
                {
                    Log($"Switching from {Id} to SearchOpponentInOwnTerritory because {target.LabelCap} was seen nearby.");
                    return new AIJob_SearchOpponentInOwnTerritory(Character, target);
                }
                else throw new System.Exception($"id {jobId} not handled.");
            }

            // If there is no opponent anywhere close, switch to attack mode
            if (!IsAnyOpponentNearby(maxCost: 25))
            {
                // If we know where enemy flag is => move directly
                if (IsEnemyFlagExplored)
                {
                    Log($"Switching from {Id} to CaptureOpponentFlag because no opponent is nearby and we know where enemy flag is.");
                    return new AIJob_CaptureOpponentFlag(Character);
                }

                // Else chose a random unexplored node in enemy territory to go to
                else
                {
                    Log($"Switching from {Id} to SearchForOpponentFlag because no opponent is nearby and we don't know where enemy flag is.");
                    return new AIJob_SearchOpponentFlag(Character);
                }

            }

            // If none of the above criteria apply, keep lingering in neutral
            return this;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(TargetNode, TargetPath);
        }
    }
}
