using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TurnAction_DoNothing : TurnAction
    {
        public TurnAction_DoNothing(Creature creature) : base(creature) { }

        public override int GetCost()
        {
            return 60;
        }

        protected override void OnPerformAction()
        {
            Debug.Log($"{Creature} is skipping its turn.");
            EndAction();
        }
    }
}
