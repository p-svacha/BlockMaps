using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// A character action represents the process of something a character does, like moving, using items or interacting with objects.
    /// </summary>
    public abstract class CharacterAction
    {
        public CTFGame Game { get; private set; }

        /// <summary>
        /// The actor who will perform this action.
        /// </summary>
        public Character Character { get; private set; }

        /// <summary>
        /// The amount of action points and stamina that get reduced when performing this action.
        /// </summary>
        public float Cost { get; protected set; }

        /// <summary>
        /// The performing state of this action.
        /// </summary>
        public CharacterActionState State { get; private set; }
        public bool IsPending => State == CharacterActionState.Pending;
        public bool IsDone => State == CharacterActionState.Done;
        public bool IsPaused => State == CharacterActionState.Paused;

        public CharacterAction(CTFGame game, Character c, float cost)
        {
            Game = game;
            Character = c;
            Cost = cost;
            State = CharacterActionState.Pending;
        }

        /// <summary>
        /// Start performing this action.
        /// </summary>
        public void Perform()
        {
            State = CharacterActionState.Performing;
            Character.SetCurrentAction(this);
            OnStartPerform();
        }
        protected abstract void OnStartPerform();

        public void PauseAction()
        {
            if (State != CharacterActionState.Performing) throw new System.Exception("Can only pause an action that is currently being performed.");
            DoPause();
            State = CharacterActionState.Paused;
        }
        public  void UnpauseAction()
        {
            if (State != CharacterActionState.Paused) throw new System.Exception("Can only unpause an action that is currently paused.");
            DoUnpause();
            State = CharacterActionState.Performing;
        }
        public abstract void DoPause();
        public abstract void DoUnpause();

        protected void EndAction()
        {
            Character.SetCurrentAction(null);
            State = CharacterActionState.Done;

            Game.OnActionDone(this);
            if(Game.State != GameState.GameFinished) Character.Owner.OnActionDone(this);
        }


    }
}
