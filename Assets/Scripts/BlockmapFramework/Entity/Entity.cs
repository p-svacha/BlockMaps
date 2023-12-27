using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Entity : MonoBehaviour
    {
        public World World;

        /// <summary>
        /// Node that the southwest corner of this entity is on at this moment.
        /// </summary>
        protected BlockmapNode OriginNode { get; private set; }

        /// <summary>
        /// List of tiles that this entity is currently on.
        /// </summary>
        public List<SurfaceNode> OccupiedTerrainNodes { get; private set; }

        /// <summary>
        /// List of all nodes that this entity currently sees.
        /// </summary>
        public List<BlockmapNode> VisibleNodes { get; private set; }

        /// <summary>
        /// Who this entity belongs to.
        /// </summary>
        public Player Player { get; private set; }
        /// <summary>
        /// How far this entity can see.
        /// </summary>
        public float VisionRange { get; private set; }
        public bool[,,] Shape;   // Size and shape of the entity, starting from southwest bottom corner

        public virtual void Init(World world, BlockmapNode position, bool[,,] shape, Player player, float visionRange)
        {
            OccupiedTerrainNodes = new List<SurfaceNode>();
            VisibleNodes = new List<BlockmapNode>();

            World = world;
            Shape = shape;
            VisionRange = visionRange;
            Player = player;
            SetOriginNode(position);

            gameObject.layer = World.Layer_Entity;
            transform.position = position.GetCenterWorldPosition();
        }

        /// <summary>
        /// Sets OccupiedNodes according to the current OriginNode of the entity. 
        /// </summary>
        private void UpdateOccupiedTerrainTiles()
        {
            // Remove entity from all currently occupied tiles
            foreach (SurfaceNode t in OccupiedTerrainNodes) t.RemoveEntity(this);
            OccupiedTerrainNodes.Clear();

            if (OriginNode.Type == NodeType.Surface)
            {
                // Set new occupied tiles and add entity to them
                for (int x = 0; x < Shape.GetLength(0); x++)
                {
                    for (int y = 0; y < Shape.GetLength(1); y++)
                    {
                        for (int z = 0; z < Shape.GetLength(2); z++)
                        {
                            if (Shape[x, y, z])
                            {
                                SurfaceNode occupiedTile = World.GetSurfaceNode(OriginNode.WorldCoordinates + new Vector2Int(x, y));
                                OccupiedTerrainNodes.Add(occupiedTile);
                                occupiedTile.AddEntity(this);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets VisibleNodes according to vision and line of sight rules.
        /// </summary>
        private void UpdateVisibleNodes()
        {
            // Remove entity vision from previously visible nodes
            HashSet<BlockmapNode> previousVisibleNodes = new HashSet<BlockmapNode>(VisibleNodes);
            foreach (BlockmapNode n in previousVisibleNodes) n.RemoveVisionBy(this);

            // Update what nodes are visible from the current position
            VisibleNodes = GetVisibleNodes();

            // Add entitiy vision to newly visible nodes
            HashSet<BlockmapNode> newVisibleNodes = new HashSet<BlockmapNode>(VisibleNodes);
            foreach (BlockmapNode n in newVisibleNodes) n.AddVisionBy(this);

            // Find nodes where the visibility changed
            HashSet<BlockmapNode> changedVisibilityNodes = new HashSet<BlockmapNode>(previousVisibleNodes);
            changedVisibilityNodes.SymmetricExceptWith(newVisibleNodes);

            // Add all adjacent nodes as well because vision goes over node edge
            HashSet<BlockmapNode> adjNodes = new HashSet<BlockmapNode>();
            foreach(BlockmapNode n in changedVisibilityNodes)
            {
                foreach(Direction dir in HelperFunctions.GetAllDirections8())
                {
                    foreach (BlockmapNode adjNode in World.GetAdjacentNodes(n.WorldCoordinates, dir))
                        adjNodes.Add(adjNode);
                }
            }
            foreach (BlockmapNode adjNode in adjNodes) changedVisibilityNodes.Add(adjNode);

            // Get chunks where visibility changed
            HashSet<Chunk> changedVisibilityChunks = new HashSet<Chunk>();
            foreach (BlockmapNode n in changedVisibilityNodes) changedVisibilityChunks.Add(n.Chunk);

            // Redraw visibility of affected chunks
            foreach (Chunk c in changedVisibilityChunks) World.OnVisibilityChanged(c, Player);
        }

        /// <summary>
        /// Returns a list of all visible nodes from the given position.
        /// </summary>
        private List<BlockmapNode> GetVisibleNodes()
        {
            List<BlockmapNode> visibleNodes = new List<BlockmapNode>();

            for(int x = (int)(-VisionRange - 1); x <= VisionRange; x++)
            {
                for(int y = (int)(-VisionRange - 1); y <= VisionRange; y++)
                {
                    Vector2Int targetWorldCoordinates = new Vector2Int(OriginNode.WorldCoordinates.x + x, OriginNode.WorldCoordinates.y + y);

                    foreach(BlockmapNode targetNode in World.GetNodes(targetWorldCoordinates))
                    {
                        if(IsInVision(targetNode)) visibleNodes.Add(targetNode);
                    }
                }
            }

            return visibleNodes;
        }

        /// <summary>
        /// Returns if the given node is currently visible by this entity.
        /// </summary>
        public bool IsInVision(BlockmapNode node)
        {
            float distance = Vector2.Distance(OriginNode.WorldCoordinates, node.WorldCoordinates);
            if (distance > VisionRange) return false;

            return true;
        }


        public static Quaternion Get2dRotationByDirection(Direction dir)
        {
            if (dir == Direction.N) return Quaternion.Euler(0f, 90f, 0f);
            if (dir == Direction.NE) return Quaternion.Euler(0f, 135f, 0f);
            if (dir == Direction.E) return Quaternion.Euler(0f, 180f, 0f);
            if (dir == Direction.SE) return Quaternion.Euler(0f, 225f, 0f);
            if (dir == Direction.S) return Quaternion.Euler(0f, 270f, 0f);
            if (dir == Direction.SW) return Quaternion.Euler(0f, 315f, 0f);
            if (dir == Direction.W) return Quaternion.Euler(0f, 0f, 0f);
            if (dir == Direction.NW) return Quaternion.Euler(0f, 45f, 0f);
            return Quaternion.Euler(0f, 0f, 0f);
        }

        public bool IsVisible(Player player) => OriginNode.IsVisibleBy(player);

        protected void SetOriginNode(BlockmapNode node)
        {
            OriginNode = node;
            UpdateVisibleNodes();
            UpdateOccupiedTerrainTiles();
        }

    }
}
