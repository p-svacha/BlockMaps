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
        /// <summary>
        /// The amount of action points and stamina that get reduced when performing this action.
        /// </summary>
        public float Cost { get; protected set; }

        /// <summary>
        /// Start performing this action for a specific character.
        /// </summary>
        public abstract void Perform(Character c);
    }
}
