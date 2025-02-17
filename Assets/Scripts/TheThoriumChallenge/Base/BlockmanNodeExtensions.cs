using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class BlockmanNodeExtensions
    {
        public static Creature GetCreature(this BlockmapNode node)
        {
            Entity e = node.Entities.FirstOrDefault(e => e is Creature);
            if (e != null) return (Creature)e;
            else return null;
        }
    }
}
