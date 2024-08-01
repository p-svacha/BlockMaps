using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A zone represents a set of 2d world coordinates. A zone is the same across all height levels. Provides some functionality for it.
    /// </summary>
    public class Zone
    {
        public int Id { get; private set; }
        private World World;
        public Actor Actor { get; private set; }
        public HashSet<Vector2Int> WorldCoordinates { get; private set; }
        public List<BlockmapNode> Nodes { get; private set; }
        public HashSet<Chunk> AffectedChunks { get; private set; }
        public bool IsBorderVisible { get; private set; }
        public bool ProvidesVision { get; private set; }

        public Zone(World world, int id, Actor actor, HashSet<Vector2Int> coordinates, bool providesVision, bool showBorders)
        {
            Id = id;
            World = world;
            Actor = actor;
            WorldCoordinates = coordinates;
            ProvidesVision = providesVision;

            Nodes = new List<BlockmapNode>();
            AffectedChunks = new HashSet<Chunk>();

            UpdateAffectedNodes();
            SetBorderStyle(showBorders, redraw: false);
        }

        /// <summary>
        /// Updates the zone references in all nodes and chunks this zone is on.
        /// </summary>
        private void UpdateAffectedNodes()
        {
            // Remove this zone from all previously affected nodes
            foreach (BlockmapNode node in Nodes)
                node.RemoveZone(this);

            // Recalculate affected nodes
            Nodes.Clear();
            foreach (Vector2Int coords in WorldCoordinates)
                Nodes.AddRange(World.GetNodes(coords));

            // Add this zone as a reference to all affected nodes
            foreach (BlockmapNode node in Nodes)
                node.AddZone(this);


            // Remove this zone from all previously affected chunks
            foreach (Chunk chunk in AffectedChunks)
                chunk.RemoveZone(this);

            // Recalculate affected chunks
            AffectedChunks.Clear();
            foreach (BlockmapNode node in Nodes)
                AffectedChunks.Add(node.Chunk);

            // Add this zone as a reference to all affected chunks
            foreach (Chunk chunk in AffectedChunks)
                chunk.AddZone(this);
        }

        public void SetBorderStyle(bool visible, bool redraw)
        {
            IsBorderVisible = visible;

            if (redraw)
            {
                foreach (Chunk chunk in AffectedChunks) chunk.DrawZoneBorders();
            }
        }

        #region Getters

        public bool ContainsNode(BlockmapNode node)
        {
            return WorldCoordinates.Contains(node.WorldCoordinates);
        }

        /// <summary>
        /// Returns a bool[] for all nodes in the given chunk.
        /// <br/> The bool[] represents if a border should be drawn N/E/S/W on that node for this zone.
        /// </summary>
        public List<bool[]> GetChunkZoneBorders(Chunk chunk)
        {
            List<bool[]> nodeBorders = new List<bool[]>();

            for (int x = 0; x < chunk.Size; x++)
            {
                for (int y = 0; y < chunk.Size; y++)
                {
                    Vector2Int worldCoords = chunk.GetWorldCoordinates(new Vector2Int(x, y));
                    bool[] borders = new bool[4];

                    if (WorldCoordinates.Contains(worldCoords))
                    {
                        Vector2Int coords_N = HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.N);
                        Vector2Int coords_E = HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.E);
                        Vector2Int coords_S = HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.S);
                        Vector2Int coords_W = HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.W);
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

        #endregion

        #region Save / Load

        public static Zone Load(World world, ZoneData data)
        {
            return new Zone(world, data.Id, world.GetActor(data.ActorId), data.Coordinates.Select(x => new Vector2Int(x.Item1, x.Item2)).ToHashSet(), data.ProvidesVision, data.ShowBorders);
        }

        public ZoneData Save()
        {
            return new ZoneData
            {
                Id = Id,
                ActorId = Actor.Id,
                ShowBorders = IsBorderVisible,
                ProvidesVision = ProvidesVision,
                Coordinates = WorldCoordinates.Select(x => new System.Tuple<int, int>(x.x, x.y)).ToList()
            };
        }

        #endregion
    }
}
