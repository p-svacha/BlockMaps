using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha
{
    public abstract class EoaAction
    {
        public EoaEntity Entity;

        public EoaAction(EoaEntity e)
        {
            Entity = e;
        }

        public abstract int Cost { get; }
        protected abstract void OnPerformAction();

        public void Perform()
        {
            Entity.IsInAction = true;
            OnPerformAction();
        }

        protected void EndAction()
        {
            Entity.IsInAction = false;
            Entity.NextActionTime.IncreaseTime(Cost);
            Entity.EndTurn();
        }


        public System.Action OnDoneCallback;
    }
}
