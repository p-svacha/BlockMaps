using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class StatPart_CreatureLevel : StatPart
    {
        public override void TransformValue(Entity entity, Stat stat, ref float value)
        {
            int level = ((Creature)entity).Level;
            value *= level;
        }

        public override string ExplanationString(Entity entity, Stat stat)
        {
            int level = ((Creature)entity).Level;
            return $"Multiplier from creature level: x{level * 100}%";
        }

        
    }
}
