using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class EntityDefs
    {
        private static string BlenderImportBasePath = "Entities/Models/BlenderImport/";
        public static string ThumbnailBasePath = "Editor/Thumbnails/";

        public static List<EntityDef> Defs = new List<EntityDef>()
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
                VisionImpact = new EntityVisionImpactProperties()
                {
                    BlocksVision = false,
                }
            },

            new EntityDef()
            {
                DefName = "Human",
                Label = "human",
                Description = "Regular human",
                EntityClass = typeof(CtfCharacter),
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "human/human_fbx"),
                Dimensions = new Vector3Int(1, 3, 1),
                VisionRange = 10,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "human/human_fbx"),
                    PlayerColorMaterialIndex = 0,
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    BlocksVision = false,
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Movement()
                    {
                        MovementSpeed = 2f,
                        CanSwim = true,
                        ClimbingSkill = ClimbingCategory.Intermediate,
                        MaxHopUpDistance = 2,
                        MaxHopDownDistance = 5,
                    },
                    new CompProperties_CTFCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),
                        MaxActionPoints = 10,
                        MaxStamina = 60,
                        StaminaRegeneration = 6,
                        MovementSkill = 10,
                    }
                },
            },

            new EntityDef()
            {
                DefName = "Dog",
                Label = "dog",
                Description = "good boi",
                EntityClass = typeof(CtfCharacter),
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "dog/dog_2_fbx"),
                Dimensions = new Vector3Int(1, 1, 1),
                VisionRange = 3,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "dog/dog_2_fbx"),
                    PlayerColorMaterialIndex = 1,
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    BlocksVision = false,
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Movement()
                    {
                        MovementSpeed = 6f,
                        CanSwim = true,
                        ClimbingSkill = ClimbingCategory.None,
                        MaxHopUpDistance = 1,
                        MaxHopDownDistance = 3,
                    },
                    new CompProperties_CTFCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
                        MaxActionPoints = 10,
                        MaxStamina = 40,
                        StaminaRegeneration = 5,
                        MovementSkill = 15,
                    }
                },
            },
        };
    }
}
