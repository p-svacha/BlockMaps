using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace BlockmapFramework.Defs
{
    public static class RockEntityDefs
    {
        private static EntityDef RockBase = new EntityDef()
        {
            Label = "rock",
            Impassable = true,
            BlocksVision = true,
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
                            { 0, MaterialManager.LoadMaterial(EntityModelPath + "rocks/TemperateRockMaterial") },
                        }
                    },
                    new EntityVariant()
                    {
                        VariantName = "Desert",
                        OverwrittenMaterials = new Dictionary<int, Material>()
                        {
                            { 0, MaterialManager.LoadMaterial(EntityModelPath + "rocks/DesertRockMaterial") },
                        }
                    },
                }
            }
        };

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef(RockBase)
            {
                DefName = "DesertRock01_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_01_fbx"),
                Dimensions = new Vector3Int(1, 2, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_01_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock01_Medium",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_01_fbx"),
                Dimensions = new Vector3Int(2, 4, 2),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_01_fbx"),
                    ModelScale = new Vector3(2f, 2f, 2f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock01_Big",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_01_fbx"),
                Dimensions = new Vector3Int(3, 6, 3),
                OverrideHeights = new Dictionary<Vector2Int, int>()
                {
                    { new Vector2Int(0, 0), 4 },
                    { new Vector2Int(0, 1), 4 },
                    { new Vector2Int(0, 2), 4 },
                    { new Vector2Int(1, 0), 5 },
                    { new Vector2Int(2, 0), 5 },
                    { new Vector2Int(2, 2), 5 },
                },
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_01_fbx"),
                    ModelScale = new Vector3(3f, 3f, 3f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock01_Large",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_01_fbx"),
                Dimensions = new Vector3Int(4, 8, 4),
                OverrideHeights = new Dictionary<Vector2Int, int>()
                {
                    { new Vector2Int(0, 0), 0 },
                    { new Vector2Int(2, 0), 0 },
                    { new Vector2Int(3, 0), 0 },
                    { new Vector2Int(0, 3), 0 },
                    { new Vector2Int(0, 1), 5 },
                    { new Vector2Int(0, 2), 5 },
                    { new Vector2Int(3, 3), 6 },
                    { new Vector2Int(3, 1), 7 },
                    { new Vector2Int(3, 2), 7 },
                },
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_01_fbx"),
                    ModelScale = new Vector3(4f, 4f, 4f),
                },
            },

            new EntityDef(RockBase)
            {
                DefName = "DesertRock02_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_02_fbx"),
                Dimensions = new Vector3Int(1, 2, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_02_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock03_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_03_fbx"),
                Dimensions = new Vector3Int(1, 2, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_03_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock04_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_04_fbx"),
                Dimensions = new Vector3Int(1, 2, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_04_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock05_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_05_fbx"),
                Dimensions = new Vector3Int(1, 1, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_05_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(RockBase)
            {
                DefName = "DesertRock06_Small",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "rocks/desert_rock_06_fbx"),
                Dimensions = new Vector3Int(1, 3, 1),
                RenderProperties = new EntityRenderProperties(RockBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "rocks/desert_rock_06_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
        };
    }
}
