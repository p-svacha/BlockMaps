using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class CreatureDefs
    {
        private static CreatureDef BaseCreature = new CreatureDef()
        {
            EntityClass = typeof(Creature),
            Impassable = false,
            BlocksVision = false,
            VisionColliderType = VisionColliderType.NodeBased,
            Components = new List<CompProperties>()
            {
                new CompProperties_Movement() { }
            },
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                PositionType = PositionType.CenterPoint,
            }
        };

        public static List<CreatureDef> Defs = new List<CreatureDef>()
        {
            new CreatureDef(BaseCreature)
            {
                DefName = "Needlegrub",
                Label = "needlegrub",
                Description = "A small, burrowing larva with sharp mandibles.",
                CreatureHeight = 1,
                VisionRange = 5,
                HpPerLevel = 0.8f,
                MovementSpeedModifier = 0.7f,
            }
        };
    }
}
