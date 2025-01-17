using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A zone represents a set of 2d world coordinates. A zone is the same across all height levels. Provides some functionality for it.
    /// </summary>
    public class Zone : WorldDatabaseObject, ISaveAndLoadable
    {
        private int id;
        public override int Id => id;
        private World World;
        public Actor Actor;
        public HashSet<Vector2Int> WorldCoordinates;
        public List<BlockmapNode> Nodes { get; private set; }
        public List<Wall> Walls { get; private set; }
        public HashSet<Chunk> AffectedChunks { get; private set; }
        public ZoneVisibility Visibility;
        public bool ProvidesVision;

        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        #region Initialize

        public Zone() { }
        public Zone(World world, int id, Actor actor, HashSet<Vector2Int> coordinates, bool providesVision, ZoneVisibility visibility)
        {
            this.id = id;
            World = world;
            Actor = actor;
            WorldCoordinates = coordinates;
            ProvidesVision = providesVision;
            Visibility = visibility;

            Init();
        }

        public override void PostLoad()
        {
            Init();
        }

        /// <summary>
        /// Gets called after instancing, either through being spawned or when being loaded.
        /// </summary>
        public void Init()
        {
            Nodes = new List<BlockmapNode>();
            Walls = new List<Wall>();
            AffectedChunks = new HashSet<Chunk>();

            UpdateAffectedWorldObjects();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Updates the zone references in all nodes, walls and chunks this zone is on.
        /// </summary>
        private void UpdateAffectedWorldObjects()
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


            // Remove this zone from all previously affected walls
            foreach (Wall wall in Walls)
                wall.RemoveZone(this);

            // Recalculate affected walls
            Walls.Clear();
            foreach (Vector2Int coords in WorldCoordinates)
                Walls.AddRange(World.GetWalls(coords));

            // Add this zone as a reference to all affected wall
            foreach (Wall wall in Walls)
                wall.AddZone(this);


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

            // Calculate bounds
            MinX = WorldCoordinates.Min(c => c.x);
            MaxX = WorldCoordinates.Max(c => c.x);
            MinY = WorldCoordinates.Min(c => c.y);
            MaxY = WorldCoordinates.Max(c => c.y);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Returns if the borders of this zone can be seen by the given actor. (In general, not right now).
        /// </summary>
        public bool CanBeSeenBy(Actor actor)
        {
            if (Visibility == ZoneVisibility.VisibleForEveryone) return true;
            if (Visibility == ZoneVisibility.VisibleForOwner) return (actor == null || actor == Actor);
            if (Visibility == ZoneVisibility.Invisible) return false;
            return false;
        }

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
                        Vector2Int coords_N = HelperFunctions.GetCoordinatesInDirection(worldCoords, Direction.N);
                        Vector2Int coords_E = HelperFunctions.GetCoordinatesInDirection(worldCoords, Direction.E);
                        Vector2Int coords_S = HelperFunctions.GetCoordinatesInDirection(worldCoords, Direction.S);
                        Vector2Int coords_W = HelperFunctions.GetCoordinatesInDirection(worldCoords, Direction.W);
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

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadReference(ref Actor, "actor");
            SaveLoadManager.SaveOrLoadPrimitive(ref Visibility, "visibility");
            SaveLoadManager.SaveOrLoadPrimitive(ref ProvidesVision, "providesVision");
            SaveLoadManager.SaveOrLoadVector2IntSet(ref WorldCoordinates, "coordinates");
        }

        #endregion
    }

    public enum ZoneVisibility
    {
        VisibleForEveryone,
        VisibleForOwner,
        Invisible
    }
}
