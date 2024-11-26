using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Represents a plot of land as a 2d rectangular area in the world.
    /// </summary>
    public class Parcel
    {
        public World World { get; private set; }

        /// <summary>
        /// The the coordinates of the SW corner of the parcel.
        /// </summary>
        public Vector2Int Position { get; private set; }

        /// <summary>
        /// The width and length of the parcel in amount of tiles/nodes.
        /// </summary>
        public Vector2Int Dimensions { get; private set; }

        public Parcel(World world, Vector2Int position, Vector2Int dimensions)
        {
            World = world;
            Position = position;
            Dimensions = dimensions;
        }

        public Vector2Int CornerSW => Position;
        public Vector2Int CornerSE => Position + new Vector2Int(Dimensions.x, 0);
        public Vector2Int CornerNE => Position + new Vector2Int(Dimensions.x, Dimensions.y);
        public Vector2Int CornerNW => Position + new Vector2Int(0, Dimensions.y);

        /// <summary>
        /// Redraws the world, recalculates the navmesh and updates entity vision for the whole parcel area.
        /// </summary>
        public void UpdateWorld()
        {
            World.RedrawNodesAround(CornerSW, Dimensions.x, Dimensions.y);
            World.UpdateNavmeshAround(CornerSW, Dimensions.x, Dimensions.y);
            World.UpdateEntityVisionAround(CornerSW, Dimensions.x, Dimensions.y);
        }
    }
}
