using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// Special actions are actions that can be selected through the action button list at the bottom of the screen.
    /// </summary>
    public abstract class SpecialAction : CharacterAction
    {
        public SpecialAction(CTFGame game, Character c, float cost) : base(game, c, cost)
        {

        }
    }
}
