using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha
{
    public class EoaAction_Move : EoaAction
    {
        private BlockmapNode Target;
        private Direction Direction;

        public EoaAction_Move(EoaEntity e, BlockmapNode target, Direction dir) : base(e)
        {
            Target = target;
            Direction = dir;
        }

        public override int Cost => 60;

        protected override void OnPerformAction()
        {
            if (Entity.IsVisibleBy(EoaGame.Instance.LocalPlayer))
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
