using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
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

        public int MinX => Position.x;
        public int MaxX => Position.x + Dimensions.x;
        public int MinY => Position.y;
        public int MaxY => Position.y + Dimensions.y;

        public Parcel(World world, Vector2Int position, Vector2Int dimensions)
        {
            World = world;
            Position = position;
            Dimensions = dimensions;
        }
        public Parcel(BlockmapNode swNode, int size = 1)
        {
            World = swNode.World;
            Position = swNode.WorldCoordinates;
            Dimensions = new Vector2Int(size, size);
        }

        public Vector2Int CornerSW => Position;
        public Vector2Int CornerSE => Position + new Vector2Int(Dimensions.x, 0);
        public Vector2Int CornerNE => Position + new Vector2Int(Dimensions.x, Dimensions.y);
        public Vector2Int CornerNW => Position + new Vector2Int(0, Dimensions.y);


        #region Getters

        public bool IsInWorld()
        {
            if (!World.IsInWorld(CornerNW)) return false;
            if (!World.IsInWorld(CornerNE)) return false;
            if (!World.IsInWorld(CornerSE)) return false;
            if (!World.IsInWorld(CornerSW)) return false;
            return true;
        }

        public bool IntersectsZone(Zone z)
        {
            // Quick bounding box check
            if (z.MaxX < MinX || z.MinX > MaxX || z.MaxY < MinY || z.MinY > MaxY)
                 return false; // No overlap in bounding boxes

            // Check each coordinate
            foreach (var coord in z.WorldCoordinates)
            {
                if (coord.x >= MinX && coord.x <= MaxX && coord.y >= MinY && coord.y <= MaxY)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyWater()
        {
            return HasAny(n => n is WaterNode);
        }
        public bool HasAnyNodesWithSurface(SurfaceDef def)
        {
            return HasAny(n => n.SurfaceDef == def);
        }

        private bool HasAny(Func<BlockmapNode, bool> predicate)
        {
            for (int x = Position.x; x < Position.x + Dimensions.x; x++)
            {
                for (int y = Position.y; y < Position.y + Dimensions.y; y++)
                {
                    if (World.GetNodes(new Vector2Int(x, y)).Any(predicate)) return true;
                }
            }
            return false;
        }

        #endregion
    }
}
