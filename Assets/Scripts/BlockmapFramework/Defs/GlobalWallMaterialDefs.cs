using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all WallMaterialDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalWallMaterialDefs
    {
        public static List<WallMaterialDef> Defs = new List<WallMaterialDef>()
        {
            new WallMaterialDef()
            {
                DefName = "Brick",
                Label = "brick",
                Description = "Just red bricks",
                Material = MaterialManager.LoadMaterial("Brick"),
            },

            new WallMaterialDef()
            {
                DefName = "Plaster",
                Label = "plaster",
                Description = "White smooth plaster",
                Material = MaterialManager.LoadMaterial("Plaster"),
                ClimbSkillRequirement = ClimbingCategory.None,
            },

            new WallMaterialDef()
            {
                DefName = "Tiles",
                Label = "tiles",
                Description = "Small blue bathroom tiles",
                Material = MaterialManager.LoadMaterial("TilesBlue"),
                ClimbSkillRequirement = ClimbingCategory.None,
            },
        };
    }
}
