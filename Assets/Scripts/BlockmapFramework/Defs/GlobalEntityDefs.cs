using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    public static class GlobalEntityDefs
    {
        public static string EntityModelPath = "Models/EntityModels/";
        public static string ThumbnailPath = "BlockEditor/Thumbnails/";

        public static List<EntityDef> GetAllGlobalEntityDefs()
        {
            List<EntityDef> Defs = new List<EntityDef>()
            {
                new EntityDef()
                {
                    DefName = "ProcHedge",
                    Label = "hedge",
                    Description = "A solid hedge",
                    UiPreviewSprite = HelperFunctions.TextureToSprite(ThumbnailPath + "ProceduralEntities/Hedge"),
                    VariableHeight = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.Batch,
                        BatchRenderFunction = HedgeMeshGenerator.BuildHedgeMesh,
                    },
                },

                new EntityDef()
                {
                    DefName = "Door",
                    Label = "door",
                    Description = "A simple door that can be opened and closed.",
                    EntityClass = typeof(Door),
                    VariableHeight = true,
                    Impassable = false,
                    VisionColliderType = VisionColliderType.CustomImplementation,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneGenerated,
                        StandaloneRenderFunction = Door.GenerateDoorMesh,
                        GetWorldPositionFunction = Door.GetWorldPosition,
                    },
                },

                new EntityDef()
                {
                    DefName = "Ladder",
                    Label = "ladder",
                    Description = "A climbable ladder.",
                    EntityClass = typeof(Ladder),
                    VariableHeight = true,
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneGenerated,
                        StandaloneRenderFunction = LadderMeshGenerator.GenerateLadderMesh,
                        GetWorldPositionFunction = Ladder.GetLadderWorldPosition,
                    },
                },

                new EntityDef()
                {
                    DefName = "PineSmall",
                    Label = "pine tree (1x1)",
                    Description = "A small pine tree",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/pine_tree_01_fbx"),
                    Dimensions = new Vector3Int(1, 3, 1),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "trees/pine_tree_01_fbx"),
                        ModelScale = new Vector3(0.25f, 0.25f, 0.25f),
                    }
                },

                new EntityDef()
                {
                    DefName = "PineMedium",
                    Label = "pine tree (2x2)",
                    Description = "A medium pine tree",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/pine_tree_01_fbx"),
                    Dimensions = new Vector3Int(2, 7, 2),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "trees/pine_tree_01_fbx"),
                        ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                    }
                },

                new EntityDef()
                {
                    DefName = "PineBig",
                    Label = "pine tree (3x3)",
                    Description = "A big pine tree",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/pine_tree_01_fbx"),
                    Dimensions = new Vector3Int(3, 10, 3),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "trees/pine_tree_01_fbx"),
                        ModelScale = new Vector3(0.9f, 0.9f, 0.9f),
                    }
                },

                new EntityDef()
                {
                    DefName = "Car01",
                    Label = "car 1",
                    Description = "A car.",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "car/car1_fbx"),
                    Dimensions = new Vector3Int(4, 3, 2),
                    OverrideHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 2 },
                        { new Vector2Int(0, 1), 2 },
                    },
                    RequiresFlatTerrain = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "car/car1_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "Car02",
                    Label = "car 2",
                    Description = "A car.",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "car/car2_fbx"),
                    Dimensions = new Vector3Int(3, 3, 2),
                    OverrideHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 2 },
                        { new Vector2Int(0, 1), 2 },
                    },
                    RequiresFlatTerrain = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "car/car2_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "Car03",
                    Label = "car 3",
                    Description = "A car.",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "car/car3_fbx"),
                    Dimensions = new Vector3Int(4, 4, 2),
                    OverrideHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 3 },
                        { new Vector2Int(0, 1), 3 },
                    },
                    RequiresFlatTerrain = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "car/car3_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "LogSmall",
                    Label = "small log",
                    Description = "A small log.",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "log_2x1/log_2x1_fbx"),
                    Dimensions = new Vector3Int(2, 1, 1),
                    RequiresFlatTerrain = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "log_2x1/log_2x1_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "Crate",
                    Label = "crate",
                    Description = "A crate.",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "crate/crate01_fbx"),
                    Dimensions = new Vector3Int(2, 2, 2),
                    RequiresFlatTerrain = true,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "crate/crate01_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "Saguaro01_Big",
                    Label = "saguaro cactus",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "cactus/saguaro01_fbx"),
                    Dimensions = new Vector3Int(1, 12, 1),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "cactus/saguaro01_fbx"),
                    },
                },

                new EntityDef()
                {
                    DefName = "Saguaro01_Medium",
                    Label = "saguaro cactus",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "cactus/saguaro01_fbx"),
                    Dimensions = new Vector3Int(1, 9, 1),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "cactus/saguaro01_fbx"),
                        ModelScale = new Vector3(0.75f, 0.75f, 0.75f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Saguaro01_Small",
                    Label = "saguaro cactus",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "cactus/saguaro01_fbx"),
                    Dimensions = new Vector3Int(1, 6, 1),
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "cactus/saguaro01_fbx"),
                        ModelScale = new Vector3(0.5f, 0.5f, 0.5f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Desert_Shrub_01",
                    Label = "desert shrub",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_01_fbx"),
                    Dimensions = new Vector3Int(1, 1, 1),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_01_fbx"),
                        ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Desert_Shrub_02_Small",
                    Label = "desert shrub",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                    Dimensions = new Vector3Int(1, 1, 1),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                        ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Desert_Shrub_02_Medium",
                    Label = "desert shrub",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                    Dimensions = new Vector3Int(2, 2, 2),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_grass_02_fbx"),
                        ModelScale = new Vector3(1.2f, 1.2f, 1.2f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Shrub_Tall_01",
                    Label = "tall shrub",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_tall_01_fbx"),
                    Dimensions = new Vector3Int(1, 3, 1),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_tall_01_fbx"),
                        ModelScale = new Vector3(1f, 1f, 1f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Shrub_Wide_01",
                    Label = "wide shrub",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/shrub_wide_01_fbx"),
                    Dimensions = new Vector3Int(2, 1, 2),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/shrub_wide_01_fbx"),
                        ModelScale = new Vector3(1f, 1f, 1f),
                    },
                },

                new EntityDef()
                {
                    DefName = "Grass_01",
                    Label = "grass",
                    Description = "",
                    UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "shrubs/grass_01_fbx"),
                    Dimensions = new Vector3Int(2, 1, 2),
                    Impassable = false,
                    BlocksVision = false,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "shrubs/grass_01_fbx"),
                        ModelScale = new Vector3(0.5f, 0.5f, 0.5f),
                    },
                },
            };

            Defs.AddRange(RockEntityDefs.Defs);

            return Defs;
        }
    }
}