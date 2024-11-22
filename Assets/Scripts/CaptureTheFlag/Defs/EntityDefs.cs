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
        };
    }
}
