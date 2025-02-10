using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    /// <summary>
    /// Each creature has a CreatureStat for all existing CreatureStatDefs.
    /// <br/>CreatureStats are the attributes that are relevant for the simulation and combat, and can be affected by the species, traits, status effects, items, etc.
    /// <br/>The base value of each CreatureStat comes from the SpeciesStatDef of the creature's species with the same DefName.
    /// </summary>
    public class CreatureStatDef : StatDef
    {
        /// <summary>
        /// Short form of label used in creature info.
        /// </summary>
        public string LabelShort { get; init; }

        /// <summary>
        /// Flag if this stat is shown in the creature info.
        /// </summary>
        public bool ShowInCreatureInfo { get; init; } = true;

        /// <summary>
        /// Flag if the stat naturally increases with the creatures level.
        /// <br/>If true, the base value is calculated with the formula: BaseValue = (10*SpeciesStatValue) + ((Level-1)*SpeciesStatValue).
        /// </summary>
        public bool ScalesWithLevel { get; init; }
    }
}
