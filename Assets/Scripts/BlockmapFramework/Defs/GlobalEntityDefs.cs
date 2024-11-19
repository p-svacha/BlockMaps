using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    public static class GlobalEntityDefs
    {
        private static string EntityModelBasePath = "Entities/Models/";
        private static string BlenderImportBasePath = "Entities/Models/BlenderImport/";
        public static string ThumbnailBasePath = "Editor/Thumbnails/";

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef()
            {
                DefName = "Human",
                Label = "human",
                Description = "Regular human",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "human/human_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(1, 3, 1),
                VisionRange = 10,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "human/human_fbx"),
                    PlayerColorMaterialIndex = 0,
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Movement()
                    {
                        MovementSpeed = 2f,
                        CanSwim = true,
                        ClimbingSkill = ClimbingCategory.Intermediate,
                    },
                },
            },

            new EntityDef()
            {
                DefName = "Dog",
                Label = "dog",
                Description = "good boi",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "dog/dog_2_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(1, 1, 1),
                VisionRange = 3,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "dog/dog_2_fbx"),
                    PlayerColorMaterialIndex = 1,
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Movement()
                    {
                        MovementSpeed = 6f,
                        CanSwim = true,
                        ClimbingSkill = ClimbingCategory.None,
                    },
                },
            },
            new EntityDef()
            {
                DefName = "ProcHedge",
                Label = "hedge",
                Description = "A solid hedge",
                UiPreviewSprite = HelperFunctions.TextureToSprite(ThumbnailBasePath + "ProceduralEntities/Hedge"),
                EntityClass = typeof(Entity),
                VariableHeight = true,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.Batch,
                    BatchRenderFunction = HedgeMeshGenerator.BuildHedgeMesh,
                }
            },

            new EntityDef()
            {
                DefName = "Door",
                Label = "door",
                Description = "A simple door that can be opened and closed.",
                EntityClass = typeof(Door),
                VariableHeight = true,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneGenerated,
                    StandaloneRenderFunction = Door.GenerateDoorMesh,
                    GetWorldPositionFunction = Door.GetWorldPosition,
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    VisionColliderType = VisionColliderType.MeshCollider
                }
            },

            new EntityDef()
            {
                DefName = "Ladder",
                Label = "ladder",
                Description = "A climbable ladder.",
                EntityClass = typeof(Ladder),
                VariableHeight = true,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneGenerated,
                    StandaloneRenderFunction = LadderMeshGenerator.GenerateLadderMesh,
                    GetWorldPositionFunction = Ladder.GetLadderWorldPosition,
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    BlocksVision = false,
                },
            },

            new EntityDef()
            {
                DefName = "PineSmall",
                Label = "pine tree (1x1)",
                Description = "A small pine tree",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelBasePath + "Trees/Fir_Tree"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(1, 3, 1),
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelBasePath + "Trees/Fir_Tree"),
                    ModelScale = 0.25f,
                }
            },

            new EntityDef()
            {
                DefName = "PineMedium",
                Label = "pine tree (2x2)",
                Description = "A medium pine tree",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelBasePath + "Trees/Fir_Tree"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(2, 7, 2),
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelBasePath + "Trees/Fir_Tree"),
                    ModelScale = 0.6f,
                }
            },

            new EntityDef()
            {
                DefName = "PineBig",
                Label = "pine tree (3x3)",
                Description = "A big pine tree",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelBasePath + "Trees/Fir_Tree"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(3, 10, 3),
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelBasePath + "Trees/Fir_Tree"),
                    ModelScale = 0.9f,
                }
            },

            new EntityDef()
            {
                DefName = "Car01",
                Label = "car 1",
                Description = "A car.",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "car/car1_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(4, 3, 2),
                RequiresFlatTerrain = true,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "car/car1_fbx"),
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    VisionColliderType = VisionColliderType.BlockPerNode,
                    VisionBlockHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 2 },
                        { new Vector2Int(0, 1), 2 },
                    },
                },
            },

            new EntityDef()
            {
                DefName = "Car02",
                Label = "car 2",
                Description = "A car.",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "car/car2_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(3, 3, 2),
                RequiresFlatTerrain = true,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "car/car2_fbx"),
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    VisionColliderType = VisionColliderType.BlockPerNode,
                    VisionBlockHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 2 },
                        { new Vector2Int(0, 1), 2 },
                    },
                },
            },

            new EntityDef()
            {
                DefName = "Car03",
                Label = "car 3",
                Description = "A car.",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "car/car3_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(4, 4, 2),
                RequiresFlatTerrain = true,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "car/car3_fbx"),
                },
                VisionImpact = new EntityVisionImpactProperties()
                {
                    VisionColliderType = VisionColliderType.BlockPerNode,
                    VisionBlockHeights = new Dictionary<Vector2Int, int>()
                    {
                        { new Vector2Int(0, 0), 3 },
                        { new Vector2Int(0, 1), 3 },
                    },
                },
            },

            new EntityDef()
            {
                DefName = "LogSmall",
                Label = "small log",
                Description = "A small log.",
                UiPreviewSprite = HelperFunctions.GetAssetPreviewSprite(BlenderImportBasePath + "log_2x1/log_2x1_fbx"),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(2, 1, 1),
                RequiresFlatTerrain = true,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(BlenderImportBasePath + "log_2x1/log_2x1_fbx"),
                },
            },
        };
    }
}
