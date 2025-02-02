using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha
{
    public class EoaEntity : MovingEntity
    {
        public EoaTime NextActionTime;
        public bool IsPlayerControlled;
        public bool IsInTurn;
        public bool IsInAction;
        private List<EoaAction> PossibleActions;
        public Dictionary<Direction, EoaAction_Move> MoveActions;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            NextActionTime = new EoaTime();
        }

        public void RefreshPossibleActions()
        {
            PossibleActions = new List<EoaAction>();
            MoveActions = new Dictionary<Direction, EoaAction_Move>();

            // Move
            foreach (Direction dir in HelperFunctions.GetSides())
            {
                if (OriginNode.WalkTransitions.TryGetValue(dir, out Transition t) && t.CanPass(this))
                {
                    EoaAction_Move moveAction = new EoaAction_Move(this, t.To, dir);
                    PossibleActions.Add(moveAction);
                    MoveActions.Add(dir, moveAction);
                }
            }

            if (PossibleActions.Count == 0) Debug.LogWarning("No possible actions");
        }

        public void PerformNextAction()
        {
            IsInTurn = true;

            if (!IsPlayerControlled)
            {
                PossibleActions.RandomElement().Perform();
            }
        }

        public void EndTurn()
        {
            IsInTurn = false;
        }

        #region Getters

        public override float VisionRange => 15;
        public override float MovementSpeed => 10;

        #endregion
    }
}
