using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// SurfaceDefs forsimple textures from old games.
    /// </summary>
    public static class RetroSurfaceDefs
    {
        public static List<SurfaceDef> Defs => new List<SurfaceDef>()
        {
            new SurfaceDef()
            {
                DefName = "fy_pool_day_Tile",
                Label = "Tile",
                Description = "White tiles from fy_pool_day",
                RenderProperties = new SurfaceRenderProperties()
                {
                    Type = SurfaceRenderType.Default_NoBlend,
                    MaterialName = "fy_pool_day_Tiles",
                },
            }
        };
    }
}

