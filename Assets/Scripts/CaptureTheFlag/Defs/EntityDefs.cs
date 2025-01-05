using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class EntityDefs
    {
        private static string BlenderImportBasePath = "Entities/Models/BlenderImport/";
        public static string ThumbnailBasePath = "Editor/Thumbnails/";

        private static EntityDef HumanBase = new EntityDef()
        {
            EntityClass = typeof(CtfCharacter),
            UiPreviewSprite = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),
            Dimensions = new Vector3Int(1, 3, 1),
            Impassable = false,
            WaterBehaviour = WaterBehaviour.HalfBelowWaterSurface,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                Model = Resources.Load<GameObject>(BlenderImportBasePath + "human/human_fbx"),
                PlayerColorMaterialIndex = 0,
            },
            VisionImpactProperties = new EntityVisionImpactProperties()
            {
                BlocksVision = false,
            },
            Components = new List<CompProperties>()
                {
                    new CompProperties_Skills()
                    {
                        InitialSkillLevels = new Dictionary<string, int>()
                        {
                            { "Running", 10 },
                            { "Vision", 8 },
                            { "Stamina", 14 },
                            { "Regeneration", 10 },
                            { "Climbing", 6 },
                            { "Swimming", 6 },
                            { "Vaulting", 8 },
                        },
                    },
                    new CompProperties_Movement()
                    {
                        ClimbingSkill = ClimbingCategory.Intermediate,
                    },
                    new CompProperties_Stats() { },
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),
                        CanUseDoors = true,
                    },
                },
        };
        private static EntityDef DogBase = new EntityDef()
        {
            EntityClass = typeof(CtfCharacter),
            UiPreviewSprite = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
            Impassable = false,
            WaterBehaviour = WaterBehaviour.HalfBelowWaterSurface,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                Model = Resources.Load<GameObject>(BlenderImportBasePath + "dog/dog_2_fbx"),
                PlayerColorMaterialIndex = 1,
            },
            VisionImpactProperties = new EntityVisionImpactProperties()
            {
                BlocksVision = false,
            },
            Components = new List<CompProperties>()
                {
                    new CompProperties_Skills()
                    {
                        InitialSkillLevels = new Dictionary<string, int>()
                        {
                            { "Running", 15 },
                            { "Vision", 5 },
                            { "Stamina", 14 },
                            { "Regeneration", 8 },
                            { "Climbing", 0 },
                            { "Swimming", 12 },
                            { "Vaulting", 5 },
                        },
                    },
                    new CompProperties_Movement() { },
                    new CompProperties_Stats() { },
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
                        CanUseDoors = false,
                    },
                },
        };
        public static List<EntityDef> CharacterDefs
        {
            get
            {
                EntityDef alberto = new EntityDef(HumanBase)
                {
                    DefName = "Human1",
                    Label = "alberto",
                    Description = "Very good and fast climber."
                };
                alberto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Climbing"] = 20;

                EntityDef usain = new EntityDef(HumanBase)
                {
                    DefName = "Human2",
                    Label = "usain",
                    Description = "Tall guy who is extremely fast but low stamina, meaning he needs to rest often.",
                    Dimensions = new Vector3Int(1, 4, 1),
                };
                usain.RenderProperties.ModelScale = new Vector3(1f, 1.3f, 1f);
                usain.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Running"] = 20;
                usain.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Stamina"] = 10;

                EntityDef eluid = new EntityDef(HumanBase)
                {
                    DefName = "Human3",
                    Label = "eluid",
                    Description = "Quite fast and very high stamina and regeneration, meaning he will almost never need rest."
                };
                eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Running"] = 12;
                eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Stamina"] = 20;
                eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Regeneration"] = 20;

                EntityDef veronica = new EntityDef(HumanBase)
                {
                    DefName = "Human4",
                    Label = "veronica",
                    Description = "Extremely good vision, making her a great scout."
                };
                veronica.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Vision"] = 14;

                EntityDef katie = new EntityDef(HumanBase)
                {
                    DefName = "Human5",
                    Label = "katie",
                    Description = "Very fast swimmer with above-average stamina."
                };
                katie.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Stamina"] = 16;
                katie.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Swimming"] = 20;

                EntityDef yaroslava = new EntityDef(HumanBase)
                {
                    DefName = "Human6",
                    Label = "yaroslava",
                    Description = "Very skilled at jumping over high obstacles and onto high platforms. She can also absorb higher drops."
                };
                yaroslava.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Vaulting"] = 20;

                EntityDef blotto = new EntityDef(DogBase)
                {
                    DefName = "Dog1",
                    Label = "chevap",
                    Description = "Bigger dog that can run fast, but low vision and needs to rest for longer periods of time.",
                    Dimensions = new Vector3Int(1, 4, 1),
                };
                blotto.RenderProperties.ModelScale = new Vector3(1f, 1.8f, 1f);
                blotto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Vaulting"] = 10;
                blotto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Regeneration"] = 6;

                EntityDef pierette = new EntityDef(DogBase)
                {
                    DefName = "Dog2",
                    Label = "cici",
                    Description = "Smaller dog with high stamina, but once that's gone she will need to rest long."
                };
                pierette.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Stamina"] = 18;
                pierette.GetCompProperties<CompProperties_Skills>().InitialSkillLevels["Running"] = 12;

                return new List<EntityDef>()
                {
                    alberto,
                    usain,
                    eluid,
                    veronica,
                    katie,
                    yaroslava,
                    blotto,
                    pierette
                };
            }
        }

        public static List<EntityDef> ObjectDefs = new List<EntityDef>()
        {
            new EntityDef()
            {
                DefName = "Flag",
                Label = "flag",
                Description = "The flag to protect or capture.",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "flag/flag_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(1, 2, 1),
                Impassable = false,
                WaterBehaviour = WaterBehaviour.AboveWater,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "flag/flag_fbx"),
                    PlayerColorMaterialIndex = 1,
                },
                VisionImpactProperties = new EntityVisionImpactProperties()
                {
                    BlocksVision = false,
                }
            },
        };
    }
}
