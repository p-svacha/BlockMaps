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

        public Parcel(Vector2Int position, Vector2Int dimensions)
        {
            Position = position;
            Dimensions = dimensions;
        }
        public Parcel(BlockmapNode swNode, int size = 1)
        {
            Position = swNode.WorldCoordinates;
            Dimensions = new Vector2Int(size, size);
        }

        public Vector2Int CornerSW => Position;
        public Vector2Int CornerSE => Position + new Vector2Int(Dimensions.x, 0);
        public Vector2Int CornerNE => Position + new Vector2Int(Dimensions.x, Dimensions.y);
        public Vector2Int CornerNW => Position + new Vector2Int(0, Dimensions.y);


        #region Getters

        public bool IsInWorld(World world)
        {
            if (!world.IsInWorld(CornerNW)) return false;
            if (!world.IsInWorld(CornerNE)) return false;
            if (!world.IsInWorld(CornerSE)) return false;
            if (!world.IsInWorld(CornerSW)) return false;
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

        public bool HasAnyWater(World world)
        {
            return HasAny(world, n => n is WaterNode);
        }
        public bool HasAnyNodesWithSurface(World world, SurfaceDef def)
        {
            return HasAny(world, n => n.SurfaceDef == def);
        }

        private bool HasAny(World world, Func<BlockmapNode, bool> predicate)
        {
            for (int x = Position.x; x < Position.x + Dimensions.x; x++)
            {
                for (int y = Position.y; y < Position.y + Dimensions.y; y++)
                {
                    if (world.GetNodes(new Vector2Int(x, y)).Any(predicate)) return true;
                }
            }
            return false;
        }

        #endregion
    }
}
