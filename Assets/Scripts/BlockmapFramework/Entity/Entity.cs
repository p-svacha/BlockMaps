using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class Entity : MonoBehaviour
    {
        public World World { get; protected set; }

        /// <summary>
        /// Node that the southwest corner of this entity is on at this moment.
        /// </summary>
        protected BlockmapNode OriginNode { get; private set; }

        /// <summary>
        /// List of tiles that this entity is currently on.
        /// </summary>
        public List<BlockmapNode> OccupiedNodes { get; private set; }

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
        public float VisionRange { get; protected set; }

        /// <summary>
        /// Size of this entity in all 3 dimensions.
        /// </summary>
        public Vector3Int Dimensions;

        /// <summary>
        /// Flag if other entities can move through this entity.
        /// </summary>
        public bool IsPassable;

        public void Init(World world, BlockmapNode position, Player player)
        {
            OccupiedNodes = new List<BlockmapNode>();
            VisibleNodes = new List<BlockmapNode>();

            World = world;
            Player = player;
            SetOriginNode(position);

            gameObject.layer = World.Layer_Entity;
            transform.position = GetWorldPosition(World, position);

            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        /// <summary>
        /// Returns the world position of this entity when placed on the given originNode.
        /// </summary>
        public Vector3 GetWorldPosition(World world, BlockmapNode originNode)
        {
            if (Dimensions.x == 1 && Dimensions.z == 1) return originNode.GetCenterWorldPosition();

            float relX = (Dimensions.x % 2 == 0) ? 0f : 0.5f;
            float relY = (Dimensions.z % 2 == 0) ? 0f : 0.5f;

            BlockmapNode targetNode = originNode;
            for (int i = 0; i < (int)(Dimensions.x / 2); i++)
            {
                if (!targetNode.ConnectedNodes.ContainsKey(Direction.E))
                    return new Vector3(originNode.WorldCoordinates.x + (int)(Dimensions.x / 2) + relX, originNode.BaseWorldHeight, originNode.WorldCoordinates.y + (int)(Dimensions.z / 2) + relY);

                targetNode = targetNode.ConnectedNodes[Direction.E];
            }
            for (int i = 0; i < (int)(Dimensions.z / 2); i++)
            {
                if (!targetNode.ConnectedNodes.ContainsKey(Direction.N))
                    return new Vector3(originNode.WorldCoordinates.x + (int)(Dimensions.x / 2) + relX, originNode.BaseWorldHeight, originNode.WorldCoordinates.y + (int)(Dimensions.z / 2) + relY);

                targetNode = targetNode.ConnectedNodes[Direction.N];
            }

            

            float y = world.GetWorldHeightAt(new Vector2(targetNode.WorldCoordinates.x + relX, targetNode.WorldCoordinates.y + relY), targetNode);
            return new Vector3(targetNode.WorldCoordinates.x + relX, y, targetNode.WorldCoordinates.y + relY);
        }

        public abstract void UpdateEntity();

        /// <summary>
        /// Sets OccupiedNodes according to the current OriginNode and Dimensions of the entity. 
        /// </summary>
        private void UpdateOccupiedTerrainTiles()
        {
            // Remove entity from all currently occupied tiles
            foreach (SurfaceNode t in OccupiedNodes) t.RemoveEntity(this);

            OccupiedNodes = GetOccupiedNodes(OriginNode);
            foreach (BlockmapNode node in OccupiedNodes) node.AddEntity(this);
        }

        /// <summary>
        /// Returns all nodes that would be occupied by this entity when placed on the given originNode.
        /// <br/> Returns null if entity can't be placed on that null.
        /// </summary>
        public List<BlockmapNode> GetOccupiedNodes(BlockmapNode originNode)
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int z = 0; z < Dimensions.z; z++)
                {
                    BlockmapNode targetNode = originNode;

                    for (int i = 0; i < x; i++)
                    {
                        if (!targetNode.ConnectedNodes.ContainsKey(Direction.E)) return null;
                        targetNode = targetNode.ConnectedNodes[Direction.E];
                    }

                    for (int i = 0; i < z; i++)
                    {
                        if (!targetNode.ConnectedNodes.ContainsKey(Direction.N)) return null;
                        targetNode = targetNode.ConnectedNodes[Direction.N];
                    }

                    nodes.Add(targetNode);
                }
            }
            return nodes;
        }

        public bool CanBePlacedOn(BlockmapNode node) => GetOccupiedNodes(node) != null;

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

        #region Getters

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

        #endregion

        #region Setters

        protected void SetOriginNode(BlockmapNode node)
        {
            OriginNode = node;
            UpdateVisibleNodes();
            UpdateOccupiedTerrainTiles();
        }

        #endregion

    }
}
