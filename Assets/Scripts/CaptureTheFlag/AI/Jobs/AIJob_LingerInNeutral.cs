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
        private bool WeAreStuck;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.LingerInNeutral;
        public override string DevmodeDisplayText => $"Lingering in Neutral (going to {TargetNode})";

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
            TargetNode = null;
            int attempts = 0;
            int maxAttempts = 20;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Match.NeutralZone.Nodes.ToList();
                BlockmapNode targetNode = candidates[Random.Range(0, candidates.Count)];
                NavigationPath targetPath = GetPath(targetNode);

                // Path is only valid if it doesn't go through opponent territory
                bool isPathValid = (targetPath != null) && targetPath.Nodes.Any(n => !Opponent.Territory.ContainsNode(n));

                if (targetPath != null) // valid target that we can reach
                {
                    TargetNode = targetNode;
                    TargetPath = targetPath;
                }
            }

            if (attempts >= maxAttempts)
            {
                Log($"Couldn't find valid target node after {attempts} attempts. We are stuck!", isWarning: true);
                WeAreStuck = true;
            }
        }

        public override void OnNextActionRequested()
        {
            // No need for target node checks if we are stuck
            if (WeAreStuck) return;

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
            if (ShouldChaseOrSearchOpponent(MAX_DEFEND_CHASE_DISTANCE, out CtfCharacter target, out AICharacterJobId jobId, out float costToTarget))
            {
                if (jobId == AICharacterJobId.ChaseAndTagOpponent)
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
                Log($"Switching from {Id} to a general non-urgent job because no opponent is nearby.");
                return GetNewNonUrgentJob();
            }

            // If none of the above criteria apply, keep lingering in neutral
            return this;
        }

        public override CharacterAction GetNextAction()
        {
            // If we are stuck, go to jail
            if (WeAreStuck)
            {
                Log($"Going to jail because we are stuck on {Character.OriginNode}.");
                return Character.PossibleSpecialActions.First(a => a is Action_GoToJail);
            }

            // Small chance to stop moving to rest
            if (Character.StaminaRatio < AIPlayer.MAX_STAMINA_FOR_REST_CHANCE && Random.value < AIPlayer.NON_URGENT_REST_CHANCE_PER_ACTION)
            {
                Log("Stop moving to rest this turn because stamina is low.");
                return null;
            }

            // Move closer to target node
            return GetSingleNodeMovementTo(TargetNode, TargetPath);
        }
    }
}
