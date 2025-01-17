using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace BlockmapFramework.Defs
{
    public static class TreeEntityDefs
    {
        private static EntityDef TreeBase = new EntityDef()
        {
            Label = "tree",
            Impassable = true,
            BlocksVision = true,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
            }
        };
        private static EntityDef PineBase = new EntityDef(TreeBase)
        {
            Label = "pine tree",
            UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/pine_tree_01_fbx"),
            RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
            {
                Model = Resources.Load<GameObject>(EntityModelPath + "trees/pine_tree_01_fbx"),
            },
        };

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef(PineBase)
            {
                DefName = "Pine_01_Tiny",
                Dimensions = new Vector3Int(1, 3, 1),
                RenderProperties = new EntityRenderProperties(PineBase.RenderProperties)
                {
                    ModelScale = new Vector3(0.25f, 0.25f, 0.25f),
                }
            },

            new EntityDef(PineBase)
            {
                DefName = "Pine_01_Small",
                Dimensions = new Vector3Int(2, 7, 2),
                RenderProperties = new EntityRenderProperties(PineBase.RenderProperties)
                {
                    ModelScale = new Vector3(0.6f, 0.6f, 0.6f),
                }
            },

            new EntityDef(PineBase)
            {
                DefName = "Pine_01_Medium",
                Dimensions = new Vector3Int(3, 10, 3),
                RenderProperties = new EntityRenderProperties(PineBase.RenderProperties)
                {
                    ModelScale = new Vector3(0.9f, 0.9f, 0.9f),
                }
            },

            new EntityDef(PineBase)
            {
                DefName = "Pine_01_Big",
                Dimensions = new Vector3Int(4, 13, 4),
                RenderProperties = new EntityRenderProperties(PineBase.RenderProperties)
                {
                    ModelScale = new Vector3(1.2f, 1.2f, 1.2f),
                }
            },

            new EntityDef(TreeBase)
            {
                DefName = "Palm_Tree_01",
                Label = "palm tree",
                Dimensions = new Vector3Int(1, 10, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/palm_tree_01_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/palm_tree_01_fbx"),
                },
            },

            new EntityDef(TreeBase)
            {
                DefName = "Palm_Tree_02",
                Label = "palm tree",
                Dimensions = new Vector3Int(1, 10, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/palm_tree_02_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/palm_tree_02_fbx"),
                    ModelScale = new Vector3(0.2f, 0.2f, 0.2f),
                },
            },

            new EntityDef(TreeBase)
            {
                DefName = "Dead_Tree_01",
                Label = "dead tree",
                Dimensions = new Vector3Int(1, 8, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/dead_tree_01_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/dead_tree_01_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(TreeBase)
            {
                DefName = "Dead_Tree_02",
                Label = "dead tree",
                Dimensions = new Vector3Int(1, 10, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/dead_tree_02_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/dead_tree_02_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(TreeBase)
            {
                DefName = "Dead_Tree_03",
                Label = "dead tree",
                Dimensions = new Vector3Int(1, 10, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/dead_tree_03_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/dead_tree_03_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(TreeBase)
            {
                DefName = "Dead_Tree_04",
                Label = "dead tree",
                Dimensions = new Vector3Int(1, 6, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/dead_tree_04_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/dead_tree_04_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
            new EntityDef(TreeBase)
            {
                DefName = "Dead_Tree_05",
                Label = "dead tree",
                Dimensions = new Vector3Int(1, 10, 1),
                UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "trees/dead_tree_05_fbx"),
                RenderProperties = new EntityRenderProperties(TreeBase.RenderProperties)
                {
                    Model = Resources.Load<GameObject>(EntityModelPath + "trees/dead_tree_05_fbx"),
                    ModelScale = new Vector3(1f, 1f, 1f),
                },
            },
        };
    }
}
