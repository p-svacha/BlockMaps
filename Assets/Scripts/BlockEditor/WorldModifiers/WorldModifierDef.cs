using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class WorldModifierDef : Def
    {
        /// <summary>
        /// The function that contains the logic for the modifier.
        /// </summary>
        public System.Action<World> ModifierAction { get; init; } = null;
    }
}
