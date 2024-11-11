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

        public AIJob_Flee(Character c) : base(c) { }

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
            List<Character> relevantOpponents = GetRelevantOpponents();
            if (relevantOpponents.Count == 0) return null;

            // Get node that is the furthest away from all opponent characters
            BlockmapNode targetNode = null;
            float highestCost = float.MinValue;
            foreach(BlockmapNode movementTarget in Character.PossibleMoves.Keys)
            {
                float lowestCost = float.MaxValue;
                foreach(Character opp in relevantOpponents)
                {
                    float cost = Pathfinder.GetPathCost(opp.Entity, opp.Node, movementTarget);
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

        private List<Character> GetRelevantOpponents()
        {
            List<Character> relevantOpponents = new List<Character>();
            foreach (Character opponentCharacter in Opponent.Characters)
            {
                if (opponentCharacter.Entity.IsInRange(Character.Node, 40)) relevantOpponents.Add(opponentCharacter);
            }
            return relevantOpponents;
        }
    }
}
