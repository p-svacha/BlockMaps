using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class SpeciesDefs
    {
        private static SpeciesDef BaseCreature = new SpeciesDef()
        {
            EntityClass = typeof(Creature),
            Impassable = false,
            BlocksVision = false,
            VisionColliderType = VisionColliderType.NodeBased,
            Components = new List<CompProperties>()
            {
                new CompProperties_Movement() { },
            },
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                PositionType = PositionType.CenterPoint,
            }
        };

        public static List<SpeciesDef> Defs = new List<SpeciesDef>()
        {
            new SpeciesDef(BaseCreature)
            {
                DefName = "Needlegrub",
                Label = "needlegrub",
                Description = "A small, burrowing larva with sharp mandibles.",
                CreatureHeight = 1,
                VisionRange = 5,
                MaxHpPerLevel = 3f,
                MovementSpeedModifier = 0.7f,
                BiteStrengthPerLevel = 0.6f,
            }
        };
    }
}
