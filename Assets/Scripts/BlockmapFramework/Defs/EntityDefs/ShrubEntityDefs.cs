using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace BlockmapFramework.Defs
{
    public static class ShrubEntityDefs
    {
        private static EntityDef ShrubBase = new EntityDef()
        {
            Label = "shrub",
            Impassable = false,
            MovementSlowdown = 1f,
            BlocksVision = false,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                Variants = new List<EntityVariant>()
                {
                    new EntityVariant()
                    {
                        VariantName = "Temperate",
                        OverwrittenMaterials = new Dictionary<int, Material>()
                        {
                            { 0, MaterialManager.LoadMaterial(EntityModelPath + "shrubs/ShrubMaterial_Temperate") },
                        }
                    },
                    new EntityVariant()
                    {
                        VariantName = "Desert",
                        OverwrittenMaterials = new Dictionary<int, Material>()
                        {
                            { 0, MaterialManager.LoadMaterial(EntityModelPath + "shrubs/ShrubMaterial_Desert") },
                        }
                    },
                }
            }
        };

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef(ShrubBase)
                {
                    DefName = "Shrub_01",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_01_fbx"),
                    Dimensions = new Vector3Int(1, 1, 1),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_01_fbx"),
                        ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                    },
                },

                new EntityDef(ShrubBase)
                {
                    DefName = "Shrub_02_Small",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                    Dimensions = new Vector3Int(1, 1, 1),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                        ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                    },
                },

                new EntityDef(ShrubBase)
                {
                    DefName = "Shrub_02_Medium",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                    Dimensions = new Vector3Int(2, 2, 2),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                        ModelScale = new Vector3(1.2f, 1.2f, 1.2f),
                    },
                },

                new EntityDef(ShrubBase)
                {
                    DefName = "Shrub_Tall_01",
                    Label = "tall shrub",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_tall_01_fbx"),
                    Dimensions = new Vector3Int(1, 3, 1),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_tall_01_fbx"),
                        ModelScale = new Vector3(1f, 1f, 1f),
                    },
                },

                new EntityDef(ShrubBase)
                {
                    DefName = "Shrub_Wide_01",
                    Label = "wide shrub",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_wide_01_fbx"),
                    Dimensions = new Vector3Int(2, 1, 2),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_wide_01_fbx"),
                        ModelScale = new Vector3(1f, 1f, 1f),
                    },
                },

                new EntityDef(ShrubBase)
                {
                    DefName = "Grass_01",
                    Label = "grass",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/grass_01_fbx"),
                    Dimensions = new Vector3Int(2, 1, 2),
                    RenderProperties = new EntityRenderProperties(ShrubBase.RenderProperties)
                    {
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/grass_01_fbx"),
                        ModelScale = new Vector3(0.5f, 0.5f, 0.5f),
                    },
                },
        };
    }
}
