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




        #region Getters

        public Vector2Int CornerSW => Position;
        public Vector2Int CornerSE => Position + new Vector2Int(Dimensions.x, 0);
        public Vector2Int CornerNE => Position + new Vector2Int(Dimensions.x, Dimensions.y);
        public Vector2Int CornerNW => Position + new Vector2Int(0, Dimensions.y);

        /// <summary>
        /// Converts local coordinates (within the parcel) to world coordinates.
        /// </summary>
        public Vector2Int GetWorldCoordinates(int x, int y)
        {
            return Position + new Vector2Int(x, y);
        }
        public Vector2Int GetWorldCoordinates(Vector2Int v) => GetWorldCoordinates(v.x, v.y); 

        /// <summary>
        /// Returns if this parcel is fully within the given world.
        /// </summary>
        public bool IsInWorld(World world)
        {
            if (!world.IsInWorld(CornerNW)) return false;
            if (!world.IsInWorld(CornerNE)) return false;
            if (!world.IsInWorld(CornerSE)) return false;
            if (!world.IsInWorld(CornerSW)) return false;
            return true;
        }

        /// <summary>
        /// Returns if any coordinate on this parcel intersects with any coordinate in the provided zone.
        /// </summary>
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

        public List<Vector2Int> GetBorderCoordinates(Direction side)
        {
            List<Vector2Int> coords;

            switch (side)
            {
                case Direction.N:
                    {
                        int y = MaxY - 1; // top interior row
                        coords = new List<Vector2Int>(Dimensions.x);
                        for (int x = MinX; x < MaxX; x++)
                            coords.Add(new Vector2Int(x, y));
                        break;
                    }
                case Direction.S:
                    {
                        int y = MinY; // bottom interior row
                        coords = new List<Vector2Int>(Dimensions.x);
                        for (int x = MinX; x < MaxX; x++)
                            coords.Add(new Vector2Int(x, y));
                        break;
                    }
                case Direction.E:
                    {
                        int x = MaxX - 1; // right interior column
                        coords = new List<Vector2Int>(Dimensions.y);
                        for (int y = MinY; y < MaxY; y++)
                            coords.Add(new Vector2Int(x, y));
                        break;
                    }
                case Direction.W:
                    {
                        int x = MinX; // left interior column
                        coords = new List<Vector2Int>(Dimensions.y);
                        for (int y = MinY; y < MaxY; y++)
                            coords.Add(new Vector2Int(x, y));
                        break;
                    }
                default:
                    coords = new List<Vector2Int>();
                    break;
            }

            return coords;
        }

        #endregion
    }
}
