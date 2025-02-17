using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class DamageTypeDef : Def
    {
        /// <summary>
        /// How effective this type of damage is vs all creature classes.
        /// </summary>
        public Dictionary<CreatureClassDef, Effectiveness> Effectivess { get; init; }
    }
}
