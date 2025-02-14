using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    /// <summary>
    /// Stores information about the current state of a creature.
    /// </summary>
    public class CreatureInfo
    {
        public EntityDef SpeciesDef { get; init; }
        public int Level { get; init; }
    }
}
