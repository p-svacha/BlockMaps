using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class SpeciesDefs
    {
        private static EntityDef BaseCreature = new EntityDef()
        {
            EntityClass = typeof(Creature),
            Impassable = false,
            BlocksVision = false,
            VisionColliderType = VisionColliderType.NodeBased,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                PositionType = PositionType.CenterPoint,
            }
        };

        public static List<EntityDef> GetDefs()
        {
            return new List<EntityDef>()
            {
                new EntityDef(BaseCreature)
                {
                    DefName = "Squishgrub",
                    Label = "squishgrub",
                    Description = "A small, squishy larva with sharp mandibles.",
                    Dimensions = new Vector3Int(1, 1, 1),
                    Components = new()
                    {
                        new CompProperties_Movement(),
                        new CompProperties_Skills()
                        {
                            InitialSkillLevels = new()
                            {
                                { SkillDefOf.Health, 3 },
                                { SkillDefOf.Moving, 5 },
                                { SkillDefOf.Vision, 5 },
                                { SkillDefOf.Biting, 8 },
                                { SkillDefOf.Punching, 0 },
                                { SkillDefOf.Kicking, 1 },
                            }
                        },
                        new CompProperties_Stats()
                        {
                            StatBases = new()
                            {
                                { StatDefOf.XpPerLevel, 10f },
                            }
                        },
                        new CompProperties_Abilities()
                        {
                            InternalizedAbilities = new()
                            {
                                AbilityDefOf.Move
                            }
                        }
                    }
                }
            };
        }
    }
}
