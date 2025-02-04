using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class CreatureDefs
    {
        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef() { DefName="Rat", EntityClass = typeof(Creature001_Rat) }
        };
    }
}
