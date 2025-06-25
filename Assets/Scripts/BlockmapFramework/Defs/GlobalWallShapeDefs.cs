using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all WallShapeDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalWallShapeDefs
    {
        private static string PreviewSpriteBasePath => "Editor/Thumbnails/WallShapes/";

        public static List<WallShapeDef> Defs = new List<WallShapeDef>()
        {
            new WallShapeDef()
            {
                DefName = "Solid",
                Label = "solid",
                Description = "A solid wall side piece",
                UiSprite = Resources.Load<Sprite>(PreviewSpriteBasePath + "solid"),
                RenderFunction = (world, meshBuilder, globalCellPosition, localCellPosition, side, material, isMirrored) => WallMeshGenerator.DrawSolidWall(meshBuilder, localCellPosition, side, material),
            },

            new WallShapeDef()
            {
                DefName = "Corner",
                Label = "corner",
                Description = "A solid wall corner piece",
                UiSprite = Resources.Load<Sprite>(PreviewSpriteBasePath + "corner"),
                IsClimbable = false,
                IsCornerShape = true,
                RenderFunction = (world, meshBuilder, globalCellPosition, localCellPosition, side, material, isMirrored) => WallMeshGenerator.DrawCornerWall(meshBuilder, localCellPosition, side, material),
            },

            new WallShapeDef()
            {
                DefName = "Slope",
                Label = "slope",
                Description = "A solid wall side piece with a sloped top",
                UiSprite = Resources.Load<Sprite>(PreviewSpriteBasePath + "slope"),
                IsClimbable = false,
                RenderFunction = (world, meshBuilder, globalCellPosition, localCellPosition, side, material, isMirrored) => WallMeshGenerator.DrawSlopeWall(meshBuilder, localCellPosition, side, material, isMirrored),
            },

            new WallShapeDef()
            {
                DefName = "Window",
                Label = "window",
                Description = "A wall side piece with a see-through glass window",
                UiSprite = Resources.Load<Sprite>(PreviewSpriteBasePath + "window"),
                BlocksVision = false,
                RenderFunction = (world, meshBuilder, globalCellPosition, localCellPosition, side, material, isMirrored) => WallMeshGenerator.DrawWindowWall(world, meshBuilder, globalCellPosition, localCellPosition, side, material, isMirrored),
            },
        };
    }
}
