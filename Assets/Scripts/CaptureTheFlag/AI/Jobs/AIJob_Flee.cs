using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_Flee : AICharacterJob
    {
        private const int FLEE_DISTANCE = 15; // If cost from opponent towards this character is less than this, flee
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
            FleeingTo = GetNodeToFleeTo();

            // We decide only at the start if we should stop fleeing. Never stop fleeing during a turn
            if (ShouldStopFleeing()) StopFleeing = true;
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
                if (IsEnemyFlagExplored)
                {
                    Log($"Switching from {Id} to CaptureOpponentFlag because we no longer need to flee and we know where enemy flag is.");
                    return new AIJob_CaptureOpponentFlag(Character);
                }

                // Else chose a random unexplored node in enemy territory to go to
                else
                {
                    Log($"Switching from {Id} to SearchForOpponentFlag because we no longer need to flee and we don't know where enemy flag is.");
                    return new AIJob_SearchOpponentFlag(Character);
                }
            }

            return this;
        }



        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(FleeingTo);
        }

        // Helpers
        private bool ShouldStopFleeing()
        {
            return FleeingFrom.Count == 0;
        }
        private List<CtfCharacter> GetOpponentsToFleeFrom()
        {
            List<CtfCharacter> relevantOpponents = new List<CtfCharacter>();
            foreach (CtfCharacter opponentCharacter in VisibleOpponentCharactersNotInJail)
            {
                if (opponentCharacter.GetComponent<Comp_Movement>().IsInRange(Character.Node, FLEE_DISTANCE, out float cost))
                {
                    Log($"Fleeing from {opponentCharacter.LabelCap} because distance is {cost}.");
                    relevantOpponents.Add(opponentCharacter);
                }
            }
            return relevantOpponents;
        }
        private BlockmapNode GetNodeToFleeTo()
        {
            BlockmapNode targetNode = null;
            float highestCost = float.MinValue;

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
