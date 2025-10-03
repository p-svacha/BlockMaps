using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// WallMaterialDefs for simple textures extracted from old games.
    /// </summary>
    public static class RetroWallMaterialDefs
    {
        public static List<WallMaterialDef> Defs => new List<WallMaterialDef>()
        {
            new WallMaterialDef()
            {
                DefName = "fy_pool_day_TileWhite",
                Label = "tile (white)",
                Description = "Default white tile from fy_pool_day",
                MaterialName = "fy_pool_day/tile",
            },

            new WallMaterialDef()
            {
                DefName = "fy_pool_day_TileRed",
                Label = "tile (red)",
                Description = "Default red tile from fy_pool_day",
                MaterialName = "fy_pool_day/tile_red",
            },

            new WallMaterialDef()
            {
                DefName = "fy_pool_day_TileBlue",
                Label = "tile (blue)",
                Description = "Default blue tile from fy_pool_day",
                MaterialName = "fy_pool_day/tile_blue",
            },

            new WallMaterialDef()
            {
                DefName = "fy_pool_day_TileMiniRed",
                Label = "mini tile (red)",
                Description = "Mini red tile wall from fy_pool_day",
                MaterialName = "fy_pool_day/tile_red_mini",
            },
            new WallMaterialDef()
            {
                DefName = "fy_pool_day_TileMiniBlue",
                Label = "mini tile (blue)",
                Description = "Mini blue tile wall from fy_pool_day",
                MaterialName = "fy_pool_day/tile_blue_mini",
            },
        };
    }
}