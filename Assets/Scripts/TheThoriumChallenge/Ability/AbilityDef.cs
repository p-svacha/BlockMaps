using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class AbilityDef : Def
    {
        /// <summary>
        /// The class that will be instantiated when assigning this ability to a creature.
        /// </summary>
        public Type AbilityClass { get; init; } = null;
    }
}
