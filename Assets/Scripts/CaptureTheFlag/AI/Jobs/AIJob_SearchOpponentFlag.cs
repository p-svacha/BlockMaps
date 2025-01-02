using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_SearchOpponentFlag : AICharacterJob
    {
        private const float OPPONENT_RANGE_TO_LINGER_IN_NEUTRAL = 13;

        private BlockmapNode TargetNode;
        private NavigationPath TargetPath;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.SearchOpponentFlag;
        public override string DevmodeDisplayText => $"Searching Flag (going to {TargetNode})";

        public AIJob_SearchOpponentFlag(CtfCharacter c) : base(c)
        {
            SetNewTargetNode();
        }

        /// <summary>
        /// Sets the target node and target path to random reachable unexplored node in opponent territory
        /// </summary>
        private void SetNewTargetNode()
        {
            // Find a target node
            TargetNode = null;
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Opponent.Territory.Nodes.Where(x => !x.IsExploredBy(Player.Actor) && x.IsPassable(Character)).ToList();
                BlockmapNode targetNode = candidates.RandomElement();
                NavigationPath targetPath = GetPath(targetNode);

                if (targetPath != null) // valid target that we can reach
                {
                    TargetNode = targetNode;
                    TargetPath = targetPath;
                }
            }

            if (attempts >= maxAttempts) Log($"Couldn't find valid target node after {attempts} attempts.", isWarning: true);
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

            // Check we should linger in neutral territory because opponent is nearby
            if (!Character.IsInOpponentTerritory)
            {
                foreach (CtfCharacter opponentCharacter in VisibleOpponentCharactersNotInJail)
                {
                    if (opponentCharacter.IsInRange(Character.Node, OPPONENT_RANGE_TO_LINGER_IN_NEUTRAL, out float cost))
                    {
                        Log($"Switching from {Id} to LingerInNeutral  because {opponentCharacter.LabelCap} is nearby (distance = {cost})");
                        return new AIJob_LingerInNeutral(Character);
                    }
                }
            }

            // Check if we should flee
            if (ShouldFlee())
            {
                Log($"Switching from {Id} to Flee because we need to flee.");
                return new AIJob_Flee(Character);
            }

            // If flag is explored 
            if (IsEnemyFlagExplored)
            {
                Log($"Switching from {Id} to CaptureOpponentFlag because enemy flag is explored.");
                return new AIJob_CaptureOpponentFlag(Character);
            }

            // Continue if none other apply
            return this;
        }


        public override CharacterAction GetNextAction()
        {
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
