using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TheThoriumChallenge.SkillDefs;

namespace TheThoriumChallenge
{
    public static class SpeciesDefs
    {
        public static List<EntityDef> GetDefs()
        {
            // Base defs

            EntityDef BaseCreature = new EntityDef()
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

            // Final defs

            return new List<EntityDef>()
            {
                new EntityDef(BaseCreature)
                {
                    DefName = "Squishgrub",
                    Label = "squishgrub",
                    Description = "A small, squishy larva with sharp mandibles.",
                    Dimensions = new Vector3Int(1, 1, 1),
                    Components = new List<CompProperties>()
                    {
                        new CompProperties_Movement(),
                        new CompProperties_Skills()
                        {
                            InitialSkillLevels = new Dictionary<string, int>()
                            {
                                { HEALTH, 3 },
                                { MOVING, 5 },
                                { VISION, 5 },
                                { BITING, 8 },
                                { PUNCHING, 0 },
                                { KICKING, 2 },
                            }
                        },
                        new CompProperties_Stats()
                        {
                            StatBases = new Dictionary<StatDef, float>()
                            {
                                { StatDefOf.XpPerLevel, 10f },
                            }
                        },
                        new CompProperties_Creature()
                        {
                            Classes = new List<CreatureClassDef>()
                            {
                                CreatureClassDefOf.Squishy,
                                CreatureClassDefOf.Insect
                            },

                            InternalizedAbilities = new List<AbilityDef>()
                            {
                                AbilityDefOf.Move,
                                AbilityDefOf.Bite,
                            }
                        }
                    }
                },

                new EntityDef(BaseCreature)
                {
                    DefName = "Snapper",
                    Label = "snapper",
                    Description = "A small, heavily armored reptilian with a powerful bite and slow movement. Prefers to clamp onto prey and hold on.",
                    Dimensions = new Vector3Int(1, 1, 1),
                    Components = new List<CompProperties>()
                    {
                        new CompProperties_Movement(),
                        new CompProperties_Skills()
                        {
                            InitialSkillLevels = new Dictionary<string, int>()
                            {
                                { HEALTH, 10 },
                                { MOVING, 5 },
                                { VISION, 6 },
                                { BITING, 10 },
                                { PUNCHING, 1 },
                                { KICKING, 1 },
                            }
                        },
                        new CompProperties_Stats()
                        {
                            StatBases = new Dictionary<StatDef, float>()
                            {
                                { StatDefOf.XpPerLevel, 10f },
                            }
                        },
                        new CompProperties_Creature()
                        {
                            Classes = new List<CreatureClassDef>()
                            {
                                CreatureClassDefOf.Armored
                            },

                            InternalizedAbilities = new List<AbilityDef>()
                            {
                                AbilityDefOf.Move,
                                AbilityDefOf.Bite,
                            }
                        }
                    }
                }
            };
        }
    }
}
