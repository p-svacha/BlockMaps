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

        public AIPlayer(Actor actor) : base(actor) { }

        public void StartTurn()
        {
            TurnFinished = false;

            foreach(Character c in Characters)
            {
                // Move to random reachable node
                Movement randomMove = c.PossibleMoves.Values.ToList()[Random.Range(0, c.PossibleMoves.Count)];
                c.PerformAction(randomMove);
            }
        }

        public void UpdateTurn()
        {
            if (Characters.All(x => !x.IsInAction)) TurnFinished = true;
        }
    }
}
