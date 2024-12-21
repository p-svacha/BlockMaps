using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Action_GoToJail : SpecialAction
    {
        private const float ACTION_COST = 0f;

        public override string Name => "Go to Jail";
        public override Sprite Icon => Resources.Load<Sprite>("CaptureTheFlag/Icons/Jail");

        public Action_GoToJail(CTFGame game, CTFCharacter c) : base(game, c, ACTION_COST) { }

        protected override void OnStartPerform()
        {
            Game.SendToJail(Character);
            EndAction();
        }

        public override void DoPause() { } // unused because action is instant
        public override void DoUnpause() { } // unused because action is instant


    }
}
