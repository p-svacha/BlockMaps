using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class TurnAction
    {
        public Creature Creature;
        private int DefinitiveCost;

        public TurnAction(Creature c)
        {
            Creature = c;
        }

        public abstract int GetCost();
        protected abstract void OnPerformAction();

        public void StartPerform()
        {
            DefinitiveCost = GetCost();
            Creature.IsInAction = true;
            OnPerformAction();
        }


        protected void EndAction()
        {
            Creature.IsInAction = false;
            Creature.NextActionTime.IncreaseTime(DefinitiveCost);
            Creature.EndTurn();
        }


        public System.Action OnDoneCallback;
    }
}
