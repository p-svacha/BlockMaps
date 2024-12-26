using BlockmapFramework;
using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Action_InteractWithDoor : SpecialCharacterAction
    {
        private const float ACTION_COST = 0.5f;

        public override string Name => "Interact with Door";
        public override Sprite Icon => Resources.Load<Sprite>("CaptureTheFlag/ActionIcons/Door");

        public Door TargetDoor { get; private set; }

        public Action_InteractWithDoor(CtfMatch game, CtfCharacter c, Door target) : base(game, c, ACTION_COST)
        {
            TargetDoor = target;
        }

        public override bool CanPerformNow()
        {
            if (!Character.CanInteractWithDoors) return false;

            return base.CanPerformNow();
        }

        protected override void OnStartPerform()
        {
            TargetDoor.Toggle(callback: DoorToggleDone);
        }

        private void DoorToggleDone()
        {
            EndAction();
        }

        public override void DoPause() { } // unused because action is instant
        public override void DoUnpause() { } // unused because action is instant

        public override NetworkMessage_CharacterAction GetNetworkAction()
        {
            return new NetworkMessage_CharacterAction("CharacterAction_InteractWithDoor", Character.Id, TargetDoor.Id);
        }
    }
}
