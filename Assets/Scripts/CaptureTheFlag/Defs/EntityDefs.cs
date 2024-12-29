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
            Impassable = false,
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
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),

                        Speed = 10,
                        Vision = 8,
                        MaxStamina = 60,
                        StaminaRegeneration = 5,
                        ClimbingSpeedModifier = 1,
                        SwimmingSpeedModifier = 1,
                        Jumping = 2,
                        Dropping = 4,
                        Height = 3,
                        CanInteractWithDoors = true,
                    },
                    new CompProperties_Movement()
                    {
                        ClimbingSkill = ClimbingCategory.Intermediate,
                    }
                },
        };
        private static EntityDef DogBase = new EntityDef()
        {
            EntityClass = typeof(CtfCharacter),
            UiPreviewSprite = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
            Impassable = false,
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
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),

                        Speed = 15,
                        Vision = 4,
                        MaxStamina = 60,
                        StaminaRegeneration = 4,
                        ClimbingSpeedModifier = 0,
                        SwimmingSpeedModifier = 1.2f,
                        Jumping = 1,
                        Dropping = 1,
                        Height = 1,
                        CanInteractWithDoors = false,
                    },
                    new CompProperties_Movement() { }
                },
        };


        public static List<EntityDef> CharacterDefs
        {
            get
            {
                EntityDef alberto = new EntityDef(HumanBase)
                {
                    DefName = "Alberto",
                    Label = "alberto",
                    Description = "Very good and fast climber."
                };
                alberto.GetCompProperties<CompProperties_CtfCharacter>().ClimbingSpeedModifier = 2f;

                EntityDef usain = new EntityDef(HumanBase)
                {
                    DefName = "Usain",
                    Label = "usain",
                    Description = "Tall guy who is extremely fast but low stamina, meaning he needs to rest often."
                };
                usain.RenderProperties.ModelScale = new Vector3(1f, 1.3f, 1f);
                usain.GetCompProperties<CompProperties_CtfCharacter>().Speed = 16;
                usain.GetCompProperties<CompProperties_CtfCharacter>().MaxStamina = 40;
                usain.GetCompProperties<CompProperties_CtfCharacter>().Height = 4;

                EntityDef eluid = new EntityDef(HumanBase)
                {
                    DefName = "Eluid",
                    Label = "eluid",
                    Description = "Quite fast and very high stamina and regeneration, meaning he will almost never need rest."
                };
                eluid.GetCompProperties<CompProperties_CtfCharacter>().Speed = 12;
                eluid.GetCompProperties<CompProperties_CtfCharacter>().MaxStamina = 100;
                eluid.GetCompProperties<CompProperties_CtfCharacter>().StaminaRegeneration = 9;

                EntityDef veronica = new EntityDef(HumanBase)
                {
                    DefName = "Veronica",
                    Label = "veronica",
                    Description = "Extremely good vision, making her a great scout."
                };
                veronica.GetCompProperties<CompProperties_CtfCharacter>().Vision = 13;

                EntityDef katie = new EntityDef(HumanBase)
                {
                    DefName = "Katie",
                    Label = "katie",
                    Description = "Very fast swimmer with above-average stamina."
                };
                katie.GetCompProperties<CompProperties_CtfCharacter>().MaxStamina = 70;
                katie.GetCompProperties<CompProperties_CtfCharacter>().SwimmingSpeedModifier = 2.5f;

                EntityDef yaroslava = new EntityDef(HumanBase)
                {
                    DefName = "Yaroslava",
                    Label = "yaroslava",
                    Description = "Very skilled at jumping over high obstacles and onto high platforms. She can also absorb higher drops."
                };
                yaroslava.GetCompProperties<CompProperties_CtfCharacter>().Jumping = 6;
                yaroslava.GetCompProperties<CompProperties_CtfCharacter>().Dropping = 6;

                EntityDef blotto = new EntityDef(DogBase)
                {
                    DefName = "Blotto",
                    Label = "blotto",
                    Description = "Bigger dog that can run fast, but low vision and needs to rest for longer periods of time."
                };
                blotto.RenderProperties.ModelScale = new Vector3(1f, 1.8f, 1f);
                blotto.GetCompProperties<CompProperties_CtfCharacter>().Height = 2;
                blotto.GetCompProperties<CompProperties_CtfCharacter>().Jumping = 2;
                blotto.GetCompProperties<CompProperties_CtfCharacter>().Dropping = 2;

                EntityDef pierette = new EntityDef(DogBase)
                {
                    DefName = "Pierette",
                    Label = "pierette",
                    Description = "Smaller dog with high stamina, but once that's gone she will need to rest long."
                };
                blotto.GetCompProperties<CompProperties_CtfCharacter>().MaxStamina = 80;
                blotto.GetCompProperties<CompProperties_CtfCharacter>().Speed = 12;

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
