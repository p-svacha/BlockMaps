using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// SurfaceDefs for simple textures extracted from old games.
    /// </summary>
    public static class RetroSurfaceDefs
    {
        public static List<SurfaceDef> Defs => new List<SurfaceDef>()
        {
            new SurfaceDef()
            {
                DefName = "fy_pool_day_Tile",
                Label = "tile (white)",
                Description = "White tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day/tile",
                },
            },

            new SurfaceDef()
            {
                DefName = "fy_pool_day_TileRed",
                Label = "tile (red)",
                Description = "Red tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day/tile_red",
                },
            },

            new SurfaceDef()
            {
                DefName = "fy_pool_day_TileBlue",
                Label = "tile (blue)",
                Description = "Blue tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day/tile_blue",
                },
            },
        };
    }
}

