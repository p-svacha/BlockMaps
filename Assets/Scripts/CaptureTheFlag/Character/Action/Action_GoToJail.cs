using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Action_GoToJail : SpecialCharacterAction
    {
        private const float ACTION_COST = 0f;

        public override string Name => "Go to Jail";
        public override Sprite Icon => Resources.Load<Sprite>("CaptureTheFlag/ActionIcons/Jail");

        public Action_GoToJail(CtfCharacter c) : base(c, ACTION_COST) { }

        public override bool CanPerformNow()
        {
            if (Character.IsInJail) return false;

            return base.CanPerformNow();
        }

        protected override void OnStartPerform()
        {
            Match.SendToJail(Character);
            EndAction();
        }

        public override void DoPause() { } // unused because action is instant
        public override void DoUnpause() { } // unused because action is instant

        public override NetworkMessage_CharacterAction GetNetworkAction()
        {
            return new NetworkMessage_CharacterAction("CharacterAction_GoToJail", Character.Id);
        }
    }
}
