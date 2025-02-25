using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// Special actions are actions that can be selected through the action button list at the bottom of the screen.
    /// </summary>
    public abstract class SpecialCharacterAction : CharacterAction
    {
        public abstract string Name { get; }
        public abstract Sprite Icon { get; }

        public SpecialCharacterAction(CtfCharacter c, float cost) : base(c, cost) { }
    }
}
