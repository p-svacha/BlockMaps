using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A collection of functions useful for all map feauture generators.
    /// </summary>
    public static class MapGenFeatureFunctions
    {
        /// <summary>
        /// Redraws the world, recalculates the navmesh and updates the entity vision for the affected area.
        /// </summary>
        public static void UpdateWorld(World world, Vector2Int coordinateSW, int rangeEast, int rangeNorth)
        {
            world.RedrawNodesAround(coordinateSW, rangeEast, rangeNorth);
            world.UpdateNavmeshAround(coordinateSW, rangeEast, rangeNorth);
            world.UpdateEntityVisionAround(coordinateSW, rangeEast, rangeNorth);
        }
    }
}
