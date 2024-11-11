using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all SurfaceDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalSurfaceDefs
    {
        private static string SurfaceTextureBasePath = "BlockmapFramework/Textures/Surface/";

        public static List<SurfaceDef> Defs = new List<SurfaceDef>()
        {
            new SurfaceDef()
            {
                DefName = "Grass",
                Label = "grass",
                Description = "Short grass",
                UiPreviewSprite = HelperFunctions.GetTextureAsSprite(SurfaceTextureBasePath + "Soil"),
                SurfacePropertyDefName = "Grass",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.FlatBlendableSurface,
                    DoBlend = true,
                    SurfaceColor = new Color(0.32f, 0.36f, 0.21f),
                    SurfaceTexture = Resources.Load<Texture2D>(SurfaceTextureBasePath + "Soil"),
                    SurfaceTextureScale = 5f,
                },
            },

            new SurfaceDef()
            {
                DefName = "Sand",
                Label = "sand",
                Description = "Soft sand",
                UiPreviewSprite = HelperFunctions.GetTextureAsSprite(SurfaceTextureBasePath + "HotSpringSand"),
                SurfacePropertyDefName = "Sand",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.FlatBlendableSurface,
                    DoBlend = true,
                    SurfaceColor = new Color(0.76f, 0.62f, 0.50f),
                    SurfaceTexture = Resources.Load<Texture2D>(SurfaceTextureBasePath + "HotSpringSand"),
                    SurfaceTextureScale = 5f,
                },
            },
        };
    }
}
