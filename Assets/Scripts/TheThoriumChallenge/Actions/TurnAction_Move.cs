using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TurnAction_Move : TurnAction
    {
        private BlockmapNode Target;
        private Direction Direction;

        public TurnAction_Move(Creature e, BlockmapNode target, Direction dir) : base(e)
        {
            Target = target;
            Direction = dir;
        }

        public override int Cost => 60;

        protected override void OnPerformAction()
        {
            if (Entity.IsVisibleBy(Game.Instance.LocalPlayer))
            {
                Entity.MovementComp.OnTargetReached += OnMovementDone;
                Entity.MovementComp.MoveTo(Target);
            }
            else
            {
                Entity.Teleport(Target, Direction);
                EndAction();
            }
        }

        private void OnMovementDone()
        {
            Entity.MovementComp.OnTargetReached -= OnMovementDone;
            EndAction();
        }
    }
}
