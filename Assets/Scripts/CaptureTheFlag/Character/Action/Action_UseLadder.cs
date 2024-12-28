using BlockmapFramework;
using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Action_UseLadder : SpecialCharacterAction
    {
        public override string Name => "Use Ladder";
        public override Sprite Icon => Resources.Load<Sprite>("CaptureTheFlag/ActionIcons/Ladder");

        public Transition Transition { get; private set; }

        public Action_UseLadder(CtfCharacter c, Transition target) : base(c, target.GetMovementCost(c))
        {
            Transition = target;
        }

        public override bool CanPerformNow()
        {
            if (!Match.CanCharacterMoveOn(Character, Transition.To)) return false;

            // Check if character can use the transition
            if (!Transition.CanPass(Character)) return false;

            return base.CanPerformNow();
        }

        protected override void OnStartPerform()
        {
            // Subsribe to OnTargetReached so we know when character is done moving
            Character.MovementComp.OnTargetReached += OnTargetReached;

            // Start movement of character entity
            Character.MovementComp.MoveAlong(new NavigationPath(Transition));
        }

        private void OnTargetReached()
        {
            Character.MovementComp.OnTargetReached -= OnTargetReached;
            EndAction();
        }

        public override void DoPause()
        {
            Character.MovementComp.PauseMovement();
        }
        public override void DoUnpause()
        {
            Character.MovementComp.UnpauseMovement();
        }

        public override NetworkMessage_CharacterAction GetNetworkAction()
        {
            return new NetworkMessage_CharacterAction("CharacterAction_UseLadder", Character.Id, Transition.To.Id);
        }
    }
}
