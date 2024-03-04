using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIPlayer : Player
    {
        public bool TurnFinished { get; private set; }

        public AIPlayer(Actor actor, Zone jailZone, Zone flagZone) : base(actor, jailZone, flagZone) { }

        public void StartTurn()
        {
            TurnFinished = false;

            // Move each character
            List<BlockmapNode> targetNodes = new List<BlockmapNode>();
            foreach(Character c in Characters)
            {
                if (c.PossibleMoves.Count == 0) continue;

                // Move to random reachable node, with heigher weights for nodes that are further west
                Dictionary<Movement, float> movementProbabilities = new Dictionary<Movement, float>();
                int maxX = c.PossibleMoves.Max(x => x.Value.Target.WorldCoordinates.x);
                foreach (var possibleMove in c.PossibleMoves)
                {
                    if (targetNodes.Contains(possibleMove.Value.Target)) continue; // Can't go on a node that another character is going to already
                    movementProbabilities.Add(possibleMove.Value, maxX - possibleMove.Value.Target.WorldCoordinates.x + 1);
                }

                Movement randomMove = HelperFunctions.GetWeightedRandomElement(movementProbabilities);
                targetNodes.Add(randomMove.Target);

                c.PerformAction(randomMove);
            }
        }

        public void UpdateTurn()
        {
            if (Characters.All(x => !x.IsInAction)) TurnFinished = true;
        }
    }
}
