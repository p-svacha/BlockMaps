using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_Flee : AICharacterJob
    {
        private const int FLEE_DISTANCE = 15; // If cost from opponent towards this character is less than this, flee

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.Flee;
        public override string DevmodeDisplayText => $"Fleeing from {FleeingFrom.Count} to {FleeingTo.ToString()}";

        private List<CtfCharacter> FleeingFrom;
        private BlockmapNode FleeingTo;

        public AIJob_Flee(CtfCharacter c) : base(c)
        {
            RefreshJob();
        }

        public override void OnCharacterStartsTurn()
        {
            RefreshJob();
        }

        private void RefreshJob()
        {
            FleeingFrom = GetOpponentsToFleeFrom();
            FleeingTo = GetNodeToFleeTo();
        }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            // Check if we should still be fleeing
            if (FleeingFrom.Count == 0) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            // todo: check if we still need to flee from same opponents as identified at start of turn
            // If not, refresh where we should flee to

            return GetSingleNodeMovementTo(FleeingTo);
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
                // Get the fastest way ANY relevant opponent could move there
                float opponentCostToGetThere = float.MaxValue;
                foreach (CtfCharacter opp in FleeingFrom)
                {
                    if (opp.Node == movementTarget) continue; // We will never want to flee onto an opponent

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
