using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    public static class GlobalEntityDefs
    {
        private static string EntityModelBasePath = "Entities/Models/";
        public static string ThumbnailBasePath = "Editor/Thumbnails/";

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef()
            {
                DefName = "PineSmall",
                Label = "pine tree (1x1)",
                Description = "A small pine tree",
                UiPreviewSprite = HelperFunctions.TextureToSprite(AssetPreview.GetMiniThumbnail(Resources.Load(EntityModelBasePath + "Trees/Fir_Tree"))),
                EntityClass = typeof(Entity),
                Dimensions = new Vector3Int(1, 3, 1),
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<Mesh>(EntityModelBasePath + "Trees/Fir_Tree"),
                    ModelScale = 0.25f,
                }
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
                DefName = "Human",
                Label = "human",
                Description = "Regular human",
                UiPreviewSprite = HelperFunctions.TextureToSprite(AssetPreview.GetMiniThumbnail(Resources.Load(EntityModelBasePath + "BlenderImport/human/human_fbx"))),
                EntityClass = typeof(Entity),
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<Mesh>(EntityModelBasePath + "BlenderImport/human/human_fbx"),
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
        };
    }
}
