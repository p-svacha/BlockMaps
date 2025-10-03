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
                DefName = "fy_pool_day_TileWhite",
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

            new SurfaceDef()
            {
                DefName = "fy_pool_day_TileMiniBlue",
                Label = "tile mini (blue)",
                Description = "Mini blue tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day/tile_blue_mini",
                },
            },

            new SurfaceDef()
            {
                DefName = "fy_pool_day_TileMiniRed",
                Label = "tile mini (red)",
                Description = "Mini red tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day/tile_red_mini",
                },
            },
        };
    }
}

