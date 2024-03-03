using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A zone represents a set of 2d world coordinates. A zone is the same across all height levels. Provides some functionality for it.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        private World World;
        public HashSet<Vector2Int> WorldCoordinates { get; private set; }
        public HashSet<Chunk> AffectedChunks { get; private set; }
        public bool IsBorderVisible { get; private set; }



        public Zone(World world, HashSet<Vector2Int> coordinates)
        {
            World = world;
            WorldCoordinates = coordinates;
            AffectedChunks = new HashSet<Chunk>();

            UpdateAffectedChunks();
        }

        /// <summary>
        /// Updates the zone references in all chunks if this zone has a world position in that zone or not.
        /// </summary>
        private void UpdateAffectedChunks()
        {
            // Remove this zone from all previously affected chunks
            foreach (Chunk chunk in AffectedChunks)
                chunk.RemoveZone(this);

            // Recalculate affected chunks
            AffectedChunks.Clear();
            foreach (Vector2Int coords in WorldCoordinates)
                AffectedChunks.Add(World.GetChunk(coords));

            // Add this zone as a reference to all affected chunks
            foreach (Chunk chunk in AffectedChunks)
                chunk.AddZone(this);
        }

        /// <summary>
        /// Returns a bool[] for all nodes in the given chunk.
        /// <br/> The bool[] represents if a border should be drawn N/E/S/W on that node for this zone.
        /// </summary>
        public List<bool[]> GetZoneBorders(Chunk chunk)
        {
            List<bool[]> nodeBorders = new List<bool[]>();

            for(int x = 0; x < chunk.Size; x++)
            {
                for (int y = 0; y < chunk.Size; y++)
                {
                    Vector2Int worldCoords = chunk.GetWorldCoordinates(new Vector2Int(x, y));
                    bool[] borders = new bool[4];

                    if (WorldCoordinates.Contains(worldCoords))
                    {
                        Vector2Int coords_N = World.GetWorldCoordinatesInDirection(worldCoords, Direction.N);
                        Vector2Int coords_E = World.GetWorldCoordinatesInDirection(worldCoords, Direction.E);
                        Vector2Int coords_S = World.GetWorldCoordinatesInDirection(worldCoords, Direction.S);
                        Vector2Int coords_W = World.GetWorldCoordinatesInDirection(worldCoords, Direction.W);
                        if (World.IsInWorld(coords_N) && !WorldCoordinates.Contains(coords_N)) borders[0] = true;
                        if (World.IsInWorld(coords_E) && !WorldCoordinates.Contains(coords_E)) borders[1] = true;
                        if (World.IsInWorld(coords_S) && !WorldCoordinates.Contains(coords_S)) borders[2] = true;
                        if (World.IsInWorld(coords_W) && !WorldCoordinates.Contains(coords_W)) borders[3] = true;
                    }

                    nodeBorders.Add(borders);
                }
            }

            return nodeBorders;
        }

        public void DrawBorders(bool show)
        {
            IsBorderVisible = show;
            foreach (Chunk chunk in AffectedChunks) chunk.DrawZoneBorders();
        }
    }
}
