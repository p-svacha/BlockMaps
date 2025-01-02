using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_CaptureOpponentFlag : AICharacterJob
    {
        private const float OPPONENT_RANGE_TO_LINGER_IN_NEUTRAL = 13;
        private const float CHANCE_TO_TAKE_DETOUR = 0.3f;
        private const float MIN_DISTANCE_TO_FLAG_FOR_DETOUR = 70; // If a character is closer to the flag than this distance, never take a detour

        private BlockmapNode ViaNode;
        private BlockmapNode TargetNode => ViaNode != null ? ViaNode : Opponent.Flag.OriginNode;
        private NavigationPath TargetPath;
        private bool WeAreStuck;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.CaptureOpponentFlag;
        public override string DevmodeDisplayText => "Capturing Flag" + (ViaNode == null ? "" : $" via {ViaNode}");

        public AIJob_CaptureOpponentFlag(CtfCharacter c) : base(c)
        {
            SetNewTargetPath();
        }

        public override void OnNextActionRequested()
        {
            // If we are taking a detour and arrived at via node, switch target to flag
            if (ViaNode != null && IsOnOrNear(ViaNode))
            {
                ViaNode = null;
                TargetPath = GetPath(TargetNode);
            }

            // Recalculate target path if no longer valid
            bool isTargetPathStillValid = (TargetPath != null && TargetPath.CanPass(Character));
            if (!isTargetPathStillValid) TargetPath = GetPath(TargetNode);
        }
        
        private void SetNewTargetPath()
        {
            ViaNode = null;

            float pathCostToFlag = GetPathCost(Opponent.Flag.OriginNode);
            bool forbidDetour = (pathCostToFlag < MIN_DISTANCE_TO_FLAG_FOR_DETOUR);
            if (forbidDetour) Log($"We will never make a detour because our distance to the flag is small (distance = {pathCostToFlag})");

            if (Random.value < CHANCE_TO_TAKE_DETOUR && !forbidDetour)
            {
                // Set any node as the via node
                int attempts = 0;
                int maxAttempts = 15;
                while (ViaNode == null && attempts < maxAttempts)
                {
                    attempts++;
                    BlockmapNode targetViaNode = Player.World.GetAllNodes().RandomElement();
                    NavigationPath targetViaPath = GetPath(targetViaNode);

                    if (targetViaPath != null) // valid target that we can reach
                    {
                        ViaNode = targetViaNode;
                        TargetPath = targetViaPath;
                    }
                }
            }

            else
            {
                TargetPath = GetPath(TargetNode);
            }

            if(TargetPath == null)
            {
                Log($"Couldn't find valid path to target. We are stuck!", isWarning: true);
                WeAreStuck = true;
            }
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we can capture the flag directly, do that no matter what
            if (Character.PossibleMoves.Values.Any(m => m.Target == Opponent.Flag.OriginNode))
            {
                Log("We can move directly onto flag, so we keep doing this job no matter what.");
                return this;
            }

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

            // Keep capturing
            return this;
        }

        public override CharacterAction GetNextAction()
        {
            // If we can capture the flag directly, do that no matter what
            Action_Movement moveToFlag = Character.PossibleMoves.Values.FirstOrDefault(m => m.Target == Opponent.Flag.OriginNode);
            if (moveToFlag != null && moveToFlag.CanPerformNow())
            {
                Log("We can move directly onto flag, so we do that no matter what.");
                return moveToFlag;
            }

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