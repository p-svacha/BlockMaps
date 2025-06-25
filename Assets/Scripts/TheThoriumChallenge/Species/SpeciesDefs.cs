using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                            InitialSkillLevels = new Dictionary<SkillDef, int>()
                            {
                                { SkillDefOf.Health, 3 },
                                { SkillDefOf.Moving, 5 },
                                { SkillDefOf.Vision, 5 },
                                { SkillDefOf.Biting, 8 },
                                { SkillDefOf.Punching, 0 },
                                { SkillDefOf.Kicking, 2 },
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
                            InitialSkillLevels = new Dictionary<SkillDef, int>()
                            {
                                { SkillDefOf.Health, 10 },
                                { SkillDefOf.Moving, 5 },
                                { SkillDefOf.Vision, 6 },
                                { SkillDefOf.Biting, 10 },
                                { SkillDefOf.Punching, 1 },
                                { SkillDefOf.Kicking, 1 },
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
