using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Represents a plot of land as a 2d rectangular area in the world that has it's own style, rules and generator during the world generation.
    /// </summary>
    public abstract class Region : Parcel
    {
        private World World;
        public abstract ParcelType Type { get; }

        protected Region(World world, Vector2Int position, Vector2Int dimensions) : base(position, dimensions)
        {
            World = world;
        }

        public abstract void Generate();

        // Helpers
        protected void FillGround(SurfaceDef surface)
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
