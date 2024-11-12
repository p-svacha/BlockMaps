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
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Soil"),
                SurfacePropertyDefName = "Grass",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.FlatBlendableSurface,
                    SurfaceReferenceMaterial = MaterialManager.LoadMaterial("BlendSurfaceReferenceMaterials/Grass"),
                },
            },

            new SurfaceDef()
            {
                DefName = "Sand",
                Label = "sand",
                Description = "Soft sand",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "HotSpringSand"),
                SurfacePropertyDefName = "Sand",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.FlatBlendableSurface,
                    SurfaceReferenceMaterial = MaterialManager.LoadMaterial("BlendSurfaceReferenceMaterials/Sand"),
                },
            },

            new SurfaceDef()
            {
                DefName = "Concrete",
                Label = "concrete",
                Description = "Concrete with a border",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "Concrete"),
                SurfacePropertyDefName = "Concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "ConcreteDark", "Concrete2", 0.1f, 0.1f, 0.1f),
                },
            },

            new SurfaceDef()
            {
                DefName = "Street",
                Label = "street",
                Description = "Street asphalt",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "CrackedConcrete"),
                SurfacePropertyDefName = "Concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => NodeMeshGenerator.BuildBorderedNodeSurface(node, meshBuilder, "Asphalt", "Cobblestone", 0.05f, 0.05f, 0.2f),
                },
            },

            new SurfaceDef()
            {
                DefName = "RoofingTiles",
                Label = "roofing tiles",
                Description = "Shingles of a room",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "RoofingTiles012B/RoofingTiles012B_1K-JPG_Color"),
                SurfacePropertyDefName = "Concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    UseLongEdges = true,
                    CustomRenderFunction = (node, meshBuilder) => meshBuilder.DrawShapePlane(node, "RoofingTiles", height: 0f, 0f, 1f, 0f, 1f),
                },
            },

            new SurfaceDef()
            {
                DefName = "WoodParquet",
                Label = "wooden parquet",
                Description = "A nice and shiny wooden parquet",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "WoodFloor051/WoodFloor051_1K-JPG_Color"),
                SurfacePropertyDefName = "Concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => meshBuilder.DrawShapePlane(node, "WoodParquet", height: 0f, 0f, 1f, 0f, 1f),
                },
            },

            new SurfaceDef()
            {
                DefName = "Tiles",
                Label = "tiles",
                Description = "White big bathroom tiles",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "Tiles132A/Tiles133A_1K-JPG_Color"),
                SurfacePropertyDefName = "Concrete",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.CustomMeshGeneration,
                    CustomRenderFunction = (node, meshBuilder) => meshBuilder.DrawShapePlane(node, "TilesWhite", height: 0f, 0f, 1f, 0f, 1f),
                },
            },

            new SurfaceDef()
            {
                DefName = "DirtPath",
                Label = "dith path",
                Description = "A foresty dirt path",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceMaterialBasePath + "Ground072/Ground072_1K-JPG_Color"),
                SurfacePropertyDefName = "Dirt",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.FlatBlendableSurface,
                    SurfaceReferenceMaterial = MaterialManager.LoadMaterial("BlendSurfaceReferenceMaterials/DirtPath"),
                },
            },

            new SurfaceDef()
            {
                DefName = "Water",
                Label = "water",
                Description = "Water",
                UiPreviewSprite = HelperFunctions.TextureToSprite(SurfaceTextureBasePath + "WaterChestDeepRamp"),
                SurfacePropertyDefName = "Water",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.NoRender,
                },
            },
        };
    }
}
