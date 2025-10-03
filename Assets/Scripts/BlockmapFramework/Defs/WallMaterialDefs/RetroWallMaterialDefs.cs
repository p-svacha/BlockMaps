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
                DefName = "fy_pool_day_tilemini_blue",
                Label = "tile mini blue",
                Description = "Mini blue tiles from fy_pool_day",
                Material = MaterialManager.LoadMaterial("Materials/NodeMaterials/fy_pool_day/tilemini_blue"),
            },

            new WallMaterialDef()
            {
                DefName = "fy_pool_day_tile",
                Label = "tile",
                Description = "Default tile from fy_pool_day",
                Material = MaterialManager.LoadMaterial("Materials/NodeMaterials/fy_pool_day/tile"),
            },
        };
    }
}