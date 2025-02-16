using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TurnAction_Move : TurnAction
    {
        private Transition Transition;
        private BlockmapNode Target;
        private Direction Direction;

        public TurnAction_Move(Creature e, Transition t, Direction dir) : base(e)
        {
            Transition = t;
            Target = t.To;
            Direction = dir;
        }

        public override int GetCost()
        {
            float baseCost = 60f;

            return (int)(baseCost * Transition.GetMovementCost(Creature));
        }

        protected override void OnPerformAction()
        {
            if (Creature.IsVisibleBy(Game.Instance.CurrentStage.LocalPlayer))
            {
                Creature.MovementComp.OnTargetReached += OnMovementDone;
                Creature.MovementComp.MoveTo(Target);
            }
            else
            {
                Creature.Teleport(Target, Direction);
                EndAction();
            }
        }

        private void OnMovementDone()
        {
            Creature.MovementComp.OnTargetReached -= OnMovementDone;
            EndAction();
        }
    }
}
