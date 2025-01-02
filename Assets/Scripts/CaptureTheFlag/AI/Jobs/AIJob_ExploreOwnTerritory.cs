using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    /// <summary>
    /// Non-urgent defender job that explores the own territory
    /// </summary>
    public class AIJob_ExploreOwnTerritory : AICharacterJob
    {
        private float MAX_CHASE_DISTANCE = 25;

        private BlockmapNode TargetNode;
        private NavigationPath TargetPath;
        private bool WeAreStuck;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.ExploreOwnTerritory;
        public override string DevmodeDisplayText => $"Exploring own territory (going to {TargetNode})";

        public AIJob_ExploreOwnTerritory(CtfCharacter c) : base(c)
        {
            SetNewTargetNode();
        }

        /// <summary>
        /// Sets the target node and target path to random reachable unexplored node in own territory
        /// </summary>
        private void SetNewTargetNode()
        {
            // Find a target node in own territoty
            TargetNode = null;
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Player.Territory.Nodes.Where(x => !x.IsExploredBy(Player.Actor) && x.IsPassable(Character)).ToList();
                BlockmapNode targetNode = candidates.RandomElement();
                NavigationPath targetPath = GetPath(targetNode);

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
            if (ShouldChaseOrSearchOpponent(MAX_CHASE_DISTANCE, out CtfCharacter target, out AICharacterJobId jobId, out float costToTarget))
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

            // Small chance that we switch to an attacker
            if (Random.value < AIPlayer.CHANCE_THAT_DEFENDER_SWITCHES_TO_ATTACKER_EACH_ACTION)
            {
                Log($"Switching from {Id} to a new general non-urgent job because we are switching role to attacker.");
                Player.Roles[Character] = AIPlayer.AICharacterRole.Attacker;
                return GetNewNonUrgentJob();
            }

            // If no of the above criteria apply, continue this job
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
