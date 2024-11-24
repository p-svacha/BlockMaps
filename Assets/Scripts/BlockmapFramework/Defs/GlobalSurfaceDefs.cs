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
        private static string SurfaceMaterialBasePath = "BlockmapFramework/Textures/MaterialTextures/";

        public static List<SurfaceDef> Defs = new List<SurfaceDef>()
        {
            new SurfaceDef()
            {
                DefName = "Grass",
                Label = "grass",
                Description = "Short grass",
                MovementSpeedModifier = 0.6f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "Grass",
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Soil"),
            },

            new SurfaceDef()
            {
                DefName = "Sand",
                Label = "sand",
                Description = "Soft sand",
                MovementSpeedModifier = 0.35f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "Sand",
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "HotSpringSand"),
            },

            new SurfaceDef()
            {
                DefName = "Concrete",
                Label = "concrete",
                Description = "Concrete with a border",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "ConcreteDark", "Concrete2", 0.1f, 0.1f, 0.1f),
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Concrete"),
            },

            new SurfaceDef()
            {
                DefName = "Street",
                Label = "street",
                Description = "Street asphalt",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "Asphalt", "Cobblestone", 0.05f, 0.05f, 0.2f),
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "CrackedConcrete"),
            },

            new SurfaceDef()
            {
                DefName = "RoofingTiles",
                Label = "roofing tiles",
                Description = "Shingles of a roof",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "RoofingTiles",
                    UseLongEdges = true,
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "RoofingTiles012B/RoofingTiles012B_1K-JPG_Color"),
            },

            new SurfaceDef()
            {
                DefName = "WoodParquet",
                Label = "wooden parquet",
                Description = "A nice and shiny wooden parquet",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "WoodParquet",
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "WoodFloor051/WoodFloor051_1K-JPG_Color"),
            },

            new SurfaceDef()
            {
                DefName = "Tiles",
                Label = "tiles",
                Description = "White big bathroom tiles",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "TilesWhite",
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "Tiles132A/Tiles133A_1K-JPG_Color"),
            },

            new SurfaceDef()
            {
                DefName = "DirtPath",
                Label = "dith path",
                Description = "A foresty dirt path",
                MovementSpeedModifier = 0.8f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "DirtPath",
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "Ground072/Ground072_1K-JPG_Color"),
            },

            new SurfaceDef()
            {
                DefName = "Water",
                Label = "water",
                Description = "Water",
                MovementSpeedModifier = 0.2f,
                Paintable = false,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.NoRender,
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "WaterChestDeepRamp"),
            },

            new SurfaceDef()
            {
                DefName = "Void",
                Label = "void",
                Description = "Surface used for terrain outside of the playable world.",
                Impassable = true,
                Paintable = false,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.NoRender,
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Void"),
            },

            new SurfaceDef()
            {
                DefName = "CorrugatedSteel",
                Label = "corrugated steel",
                Description = "Shiny corrugated steel.",
                MovementSpeedModifier = 0.7f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "CorrugatedSteel",
                    Height = 0.1f,
                },
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "CorrugatedSteel005/CorrugatedSteel005_1K-JPG_Color"),
            }
        };
    }
}
