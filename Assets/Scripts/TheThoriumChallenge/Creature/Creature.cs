using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class Creature : MovingEntity
    {
        // Attributes for simulation
        public TimeStamp NextActionTime;
        public bool IsPlayerControlled;
        public bool IsInTurn;
        public bool IsInAction;
        private List<TurnAction> PossibleActions;
        public Dictionary<Direction, TurnAction_Move> MoveActions;

        // Creature Stats
        public abstract float BaseHpPerLevel { get; }
        public abstract float BaseMovementSpeedModifier { get; }

        // Creature Looks
        protected abstract GameObject Model { get; }
        public abstract int CreatureHeight { get; }

        // Helpers
        protected const string ModelPath = "TheThoriumChallenge/CreatureModels/";
        private GameObject _RenderModel;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _RenderModel = Model;

            NextActionTime = new TimeStamp();
        }

        public void RefreshPossibleActions()
        {
            PossibleActions = new List<TurnAction>();
            MoveActions = new Dictionary<Direction, TurnAction_Move>();

            // Move
            foreach (Direction dir in HelperFunctions.GetSides())
            {
                if (OriginNode.WalkTransitions.TryGetValue(dir, out Transition t) && t.CanPass(this))
                {
                    TurnAction_Move moveAction = new TurnAction_Move(this, t.To, dir);
                    PossibleActions.Add(moveAction);
                    MoveActions.Add(dir, moveAction);
                }
            }

            if (PossibleActions.Count == 0) Debug.LogWarning("No possible actions");
        }

        public void PerformNextAction()
        {
            IsInTurn = true;

            if (IsPlayerControlled)
            {
                World.EnablePerspectiveVisionCutoff(this);
                World.CameraPanToFocusEntity(this, duration: 0.6f, followAfterPan: false);
            }

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

        protected override GameObject RenderModel => _RenderModel;
        public override Vector3Int Dimensions => new Vector3Int(1, CreatureHeight, 1);
        public override float VisionRange => 15;
        public override float MovementSpeed => 5;

        #endregion
    }
}
