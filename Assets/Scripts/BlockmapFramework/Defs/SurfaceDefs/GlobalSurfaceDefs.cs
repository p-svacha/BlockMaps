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
        private static string SurfaceTextureBasePath = "Textures/Surface/";
        private static string SurfaceMaterialBasePath = "Textures/MaterialTextures/";

        public static List<SurfaceDef> Defs => new List<SurfaceDef>()
        {
            new SurfaceDef()
            {
                DefName = "Grass",
                Label = "grass",
                Description = "Short grass",
                MovementSpeedModifier = 0.65f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "Grass",
                },
            },

            new SurfaceDef()
            {
                DefName = "Sand",
                Label = "sand",
                Description = "Sand",
                MovementSpeedModifier = 0.5f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "Sand",
                },
            },

            new SurfaceDef()
            {
                DefName = "SandSoft",
                Label = "soft sand",
                Description = "Soft sand",
                MovementSpeedModifier = 0.4f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "SandSoft",
                },
            },

            new SurfaceDef()
            {
                DefName = "Concrete",
                Label = "concrete",
                Description = "Simple flat concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "ConcreteLight",
                    DrawSlopeAsStairs = true,
                },
            },

            new SurfaceDef()
            {
                DefName = "Sidewalk",
                Label = "sidewalk",
                Description = "Raised concrete sidewalk with a border",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "Materials/NodeMaterials/ConcreteDark", "Materials/NodeMaterials/Concrete2", 0.1f, 0.1f, 0.1f),
                },
                UiSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Concrete"),
            },

            new SurfaceDef()
            {
                DefName = "Street",
                Label = "street",
                Description = "Street asphalt with a cobblestone border",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "Materials/NodeMaterials/Asphalt", "Materials/NodeMaterials/Cobblestone", 0.05f, 0.05f, 0.2f),
                },
                UiSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "CrackedConcrete"),
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
            },

            new SurfaceDef()
            {
                DefName = "DirtPath",
                Label = "dith path",
                Description = "A foresty dirt path",
                MovementSpeedModifier = 0.85f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "DirtPath",
                },
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
                UiSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "WaterChestDeepRamp"),
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
                UiSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Void"),
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
            },

            new SurfaceDef()
            {
                DefName = "Sandstone",
                Label = "sandstone",
                Description = "",
                MovementSpeedModifier = 0.9f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_Blend,
                    MaterialName = "Sandstone",
                },
            },

            new SurfaceDef()
            {
                DefName = "MetalPlates",
                Label = "metal plates",
                Description = "",
                MovementSpeedModifier = 1f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "MetalPlates",
                },
            },

            new SurfaceDef()
            {
                DefName = "DiamondPlate",
                Label = "metal",
                Description = "",
                MovementSpeedModifier = 1f,
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "DiamondPlate",
                },
            },
        };
    }
}
