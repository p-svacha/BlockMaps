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

        /// <summary>
        /// Gets called every frame on opponent actions that are at least partly visible to the player.
        /// </summary>
        public abstract void UpdateVisibleOpponentAction();

        protected void EndAction()
        {
            Character.SetCurrentAction(null);
            Game.OnActionDone(this);
            State = CharacterActionState.Done;
        }

        /// <summary>
        /// Returns if the given player can see any part of this action happening.
        /// </summary>
        public abstract bool IsVisibleBy(Player p);

        public CharacterAction(CTFGame game, Character c)
        {
            Game = game;
            Character = c;
            State = CharacterActionState.Pending;
        }
    }
}
