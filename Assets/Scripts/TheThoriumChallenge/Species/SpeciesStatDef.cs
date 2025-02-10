using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    /// <summary>
    /// Species stats are fixed, immutable values that each species has for every SpeciesStatDef.
    /// <br/>They act as base value for CreatureStatDefs with the same DefName.
    /// </summary>
    public class SpeciesStatDef : Def { }
}
