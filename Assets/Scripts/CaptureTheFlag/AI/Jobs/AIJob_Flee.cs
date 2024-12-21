using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_Flee : AICharacterJob
    {
        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.Flee;
        public override string DevmodeDisplayText => "Fleeing";

        public AIJob_Flee(CTFCharacter c) : base(c) { }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            // Check if we should still be fleeing
            if (!Player.ShouldFlee(Character)) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            // Get opponent characters that are relevant
            List<CTFCharacter> relevantOpponents = GetRelevantOpponents();
            if (relevantOpponents.Count == 0) return null;

            // Get node that is the furthest away from all opponent characters
            BlockmapNode targetNode = null;
            float highestCost = float.MinValue;
            foreach(BlockmapNode movementTarget in Character.PossibleMoves.Keys)
            {
                float lowestCost = float.MaxValue;
                foreach(CTFCharacter opp in relevantOpponents)
                {
                    float cost = Pathfinder.GetPathCost(opp, opp.Node, movementTarget);
                    if (cost < lowestCost) lowestCost = cost;
                }
                if (lowestCost > highestCost)
                {
                    highestCost = lowestCost;
                    targetNode = movementTarget;
                }
            }

            // Move one node towards there
            return GetSingleNodeMovementTo(targetNode);
        }

        private List<CTFCharacter> GetRelevantOpponents()
        {
            List<CTFCharacter> relevantOpponents = new List<CTFCharacter>();
            foreach (CTFCharacter opponentCharacter in Opponent.Characters)
            {
                if (opponentCharacter.GetComponent<Comp_Movement>().IsInRange(Character.Node, 40)) relevantOpponents.Add(opponentCharacter);
            }
            return relevantOpponents;
        }
    }
}
