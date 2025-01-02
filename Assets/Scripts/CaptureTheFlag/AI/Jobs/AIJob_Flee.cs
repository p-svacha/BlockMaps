using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_Flee : AICharacterJob
    {
        private bool StopFleeing;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.Flee;
        public override string DevmodeDisplayText => $"Fleeing from {FleeingFrom.Count} to {FleeingTo}";

        private List<CtfCharacter> FleeingFrom;
        private BlockmapNode FleeingTo;

        public AIJob_Flee(CtfCharacter c) : base(c)
        {
            FleeingFrom = GetOpponentsToFleeFrom();
            FleeingTo = GetNodeToFleeTo();
        }

        public override void OnCharacterStartsTurn()
        {
            FleeingFrom = GetOpponentsToFleeFrom();
            if (FleeingFrom.Count > 0) FleeingTo = GetNodeToFleeTo();

            // We decide only at the start if we should stop fleeing. Never stop fleeing during a turn
            if (!ShouldFlee()) StopFleeing = true;
        }

        public override void OnNextActionRequested()
        {
            // Update where we flee to
            FleeingFrom = GetOpponentsToFleeFrom();
            if (FleeingFrom.Count > 0) FleeingTo = GetNodeToFleeTo();
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we can tag an opponent directly this turn, do that
            if (CanTagCharacterDirectly(out CtfCharacter target0))
            {
                Log($"Switching from {Id} to ChaseToTagOpponent because we can reach {target0.LabelCap} directly this turn.");
                return new AIJob_ChaseToTagOpponent(Character, target0);
            }

            // If we should stop fleeing, find a new job based on game state
            if (StopFleeing)
            {
                Log($"Switching from {Id} to a general non-urgent job because we no longer need to flee.");
                return GetNewNonUrgentJob();
            }

            return this;
        }

        public override CharacterAction GetNextAction()
        {
            // Stop moving further if no longer in opponent territory
            if (!Character.IsInOpponentTerritory) return null;

            // Else keep fleeing to target
            return GetSingleNodeMovementTo(FleeingTo);
        }

        private BlockmapNode GetNodeToFleeTo()
        {
            BlockmapNode targetNode = null;
            float highestCost = float.MinValue;

            // If there are nodes that are outside of the opponent territory, go to random one of those
            List<BlockmapNode> candidateSafeNodes = Character.PossibleMoves.Keys.Where(n => !Opponent.Territory.ContainsNode(n)).ToList();
            if (candidateSafeNodes.Count > 0)
            {
                BlockmapNode fleeTarget = candidateSafeNodes.RandomElement();
                Log($"Fleeing towards {fleeTarget} because it is outside the opponent territory.");
                return fleeTarget;
            }

            // Get node we can move to this turn that is the furthest away from all opponent characters we are fleeing from
            foreach (BlockmapNode movementTarget in Character.PossibleMoves.Keys)
            {
                if (VisibleOpponentCharactersNotInJail.Any(c => c.Node == movementTarget)) continue; // We will never want to flee onto an opponent

                // Get the fastest way ANY relevant opponent could move there
                float opponentCostToGetThere = float.MaxValue;
                foreach (CtfCharacter opp in FleeingFrom)
                {
                    float costForOpponent = Pathfinder.GetPathCost(opp, opp.Node, movementTarget);
                    if (costForOpponent < opponentCostToGetThere) opponentCostToGetThere = costForOpponent;
                }

                // Check if that node is the most expensive for the opponent so far
                if (opponentCostToGetThere > highestCost)
                {
                    highestCost = opponentCostToGetThere;
                    targetNode = movementTarget;
                }
            }

            Log($"Fleeing towards {targetNode} because it is the furthest away from all relevant opponents with cost {highestCost}");

            return targetNode;
        }
    }
}
