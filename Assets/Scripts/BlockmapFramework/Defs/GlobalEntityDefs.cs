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
                    DefName = "Log_01_Small",
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
                    DefName = "Saguaro_01_Big",
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
                    DefName = "Saguaro_01_Medium",
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
                    DefName = "Saguaro_01_Small",
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
            };

            Defs.AddRange(RockEntityDefs.Defs);
            Defs.AddRange(ShrubEntityDefs.Defs);
            Defs.AddRange(TreeEntityDefs.Defs);

            return Defs;
        }
    }
}