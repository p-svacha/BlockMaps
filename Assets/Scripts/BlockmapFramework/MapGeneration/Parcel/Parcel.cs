using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// A parcel represents a plot of land as a 2d rectangular area in the world that has it's own style, rules and generator during the world generation.
    /// </summary>
    public abstract class Parcel
    {
        protected World World;

        public abstract ParcelType Type { get; }

        /// <summary>
        /// The position, aka the coordinates of the SW corner of the parcel.
        /// </summary>
        public Vector2Int Position { get; private set; }

        /// <summary>
        /// The width and length of the parcel in amount of tiles/nodes.
        /// </summary>
        public Vector2Int Dimensions { get; private set; }

        protected Parcel(World world, Vector2Int position, Vector2Int dimensions)
        {
            World = world;
            Position = position;
            Dimensions = dimensions;
        }

        public abstract void Generate();

        // Helpers
        protected void FillGround(SurfaceId surface)
        {
            for (int x = Position.x; x < Position.x + Dimensions.x; x++)
            {
                for (int y = Position.y; y < Position.y + Dimensions.y; y++)
                {
                    GroundNode groundNode = World.GetGroundNode(x, y);
                    World.SetSurface(groundNode, surface, updateWorld: false);
                }
            }
        }
    }
}
