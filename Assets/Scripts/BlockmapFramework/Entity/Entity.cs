using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Flag if this entity blocks the vision from other entities.
        /// </summary>
        public bool BlocksVision;

        #region Initialize

        public void Init(World world, BlockmapNode position, Player player)
        {
            OccupiedNodes = new List<BlockmapNode>();
            VisibleNodes = new List<BlockmapNode>();

            World = world;
            Player = player;
            SetOriginNode(position);

            gameObject.layer = World.Layer_Entity;
            transform.position = GetWorldPosition(World, position);

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(Dimensions.x / transform.localScale.x, (Dimensions.y * World.TILE_HEIGHT) / transform.localScale.y, Dimensions.z / transform.localScale.z);
            collider.center = new Vector3(0f, collider.size.y / 2, 0f);

            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        #endregion

        #region Update

        public abstract void UpdateEntity();

        /// <summary>
        /// Sets OccupiedNodes according to the current OriginNode and Dimensions of the entity. 
        /// </summary>
        private void UpdateOccupiedNodes()
        {
            // Remove entity from all currently occupied nodes and chunks
            foreach (BlockmapNode t in OccupiedNodes)
            {
                t.RemoveEntity(this);
                t.Chunk.Entities.Remove(this);
            }

            // Get list of nodes that are currently occupied
            OccupiedNodes = GetOccupiedNodes(OriginNode);

            // Add entity to all newly occupies nodes and chunks
            foreach (BlockmapNode node in OccupiedNodes)
            {
                node.AddEntity(this);
                node.Chunk.Entities.Add(this);
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
            VisibleNodes = GetVisibleNodes(OriginNode, out List<BlockmapNode> exploredNodes);

            // Add entitiy vision to newly visible nodes
            HashSet<BlockmapNode> newVisibleNodes = new HashSet<BlockmapNode>(VisibleNodes);
            foreach (BlockmapNode n in newVisibleNodes) n.AddVisionBy(this);

            // Set nodes as explored that are explored by this entity but not visible (in fog of war)
            foreach (BlockmapNode n in exploredNodes) n.AddExploredBy(Player);

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
        /// Shows, hides or tints (fog of war) this entity according to if its visible by the given player.
        /// </summary>
        public void UpdateVisiblity(Player player)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();

            if (IsVisibleBy(player))
            {
                renderer.enabled = true;
                renderer.material.color = Color.white;
            }
            else if (IsExploredBy(player))
            {
                renderer.enabled = true;
                renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Fog of war, todo: change this when making dedicated entity shader
            }
            else renderer.enabled = false;
        }

        #endregion

        #region Getters

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

        /// <summary>
        /// Returns all nodes that would be visible by this entity when placed on the given originNode.
        /// <br/> Additionaly returns a list of nodes that are only explored by this node, but not visible.
        /// </summary>
        private List<BlockmapNode> GetVisibleNodes(BlockmapNode originNode, out List<BlockmapNode> exploredNodes)
        {
            List<BlockmapNode> visibleNodes = new List<BlockmapNode>();
            exploredNodes = new List<BlockmapNode>();

            for (int x = (int)(-VisionRange - 1); x <= VisionRange; x++)
            {
                for(int y = (int)(-VisionRange - 1); y <= VisionRange; y++)
                {
                    Vector2Int targetWorldCoordinates = new Vector2Int(originNode.WorldCoordinates.x + x, originNode.WorldCoordinates.y + y);

                    foreach(BlockmapNode targetNode in World.GetNodes(targetWorldCoordinates))
                    {
                        VisionType vision = GetVisionType(targetNode);
                        if (vision == VisionType.Visible) visibleNodes.Add(targetNode);
                        else if (vision == VisionType.FogOfWar) exploredNodes.Add(targetNode);
                    }
                }
            }

            return visibleNodes;
        }

        /// <summary>
        /// Returns if the given node is visible, in fog of war or unexplored by this entity when standing on the given originNode.
        /// </summary>
        public VisionType GetVisionType(BlockmapNode node)
        {
            // Check if node is out of vision range (only checks in 2d)
            float distance = Vector2.Distance(OriginNode.WorldCoordinates, node.WorldCoordinates);
            if (distance > VisionRange) return VisionType.Unexplored;

            // Shoot ray from eye to the node with infinite range and check if we hit the correct node
            Vector3 raySource = GetEyePosition();
            Vector3 nodeRayTarget = node.GetCenterWorldPosition();
            Vector3 nodeRayDirection = nodeRayTarget - raySource;

            Ray nodeRay = new Ray(raySource, nodeRayDirection);
            if(Physics.Raycast(nodeRay, out RaycastHit nodeHit))
            {
                GameObject objectHit = nodeHit.transform.gameObject;
                if(objectHit != null)
                {
                    Vector3 hitPosition = nodeHit.point;
                    Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);

                    // If the position we hit approximately matches the position of the node we are checking, mark it as visible
                    if (hitWorldCoordinates == node.WorldCoordinates) return VisionType.Visible;

                    // Make proximity checks (needed when hitting a node edge)
                    float epsilon = 0.01f;
                    float xFrac = hitPosition.x % 1f;
                    float yFrac = hitPosition.z % 1f;

                    if (xFrac < epsilon && yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(-1, -1)) == node.WorldCoordinates) return VisionType.Visible; // SW
                    else if (xFrac > 1f - epsilon && yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(1, -1)) == node.WorldCoordinates) return VisionType.Visible; // SE
                    else if (xFrac > 1f - epsilon && yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(1, 1)) == node.WorldCoordinates) return VisionType.Visible; // NE
                    else if (xFrac < epsilon && yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(-1, 1)) == node.WorldCoordinates) return VisionType.Visible; // NW
                    else if (xFrac < epsilon && (hitWorldCoordinates + new Vector2Int(-1, 0)) == node.WorldCoordinates) return VisionType.Visible; // W
                    else if (xFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(1, 0)) == node.WorldCoordinates) return VisionType.Visible; // E
                    else if (yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(0, 1)) == node.WorldCoordinates) return VisionType.Visible; // N
                    else if (yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(0, -1)) == node.WorldCoordinates) return VisionType.Visible; // S

                    // If we hit an entity somewhere else, check if the node we are looking for is an occupied node by this entity. If so, mark it as Fog of War
                    else if(objectHit.layer == World.Layer_Entity)
                    {
                        Entity e = objectHit.GetComponent<Entity>();
                        if (e.OccupiedNodes.Contains(node)) return VisionType.FogOfWar;
                    }
                }
            }

            // If the node has entities shoot a ray at them. If we hit one, mark the node as Fog of War (because we can't see the node directly but the entity that's on it.)
            foreach(Entity e in node.Entities)
            {
                Vector3 entitiyRayTarget = e.GetComponent<MeshRenderer>().bounds.center;
                Vector3 entityRayDirection = entitiyRayTarget - raySource;

                Ray entityRay = new Ray(raySource, entityRayDirection);
                if (Physics.Raycast(entityRay, out RaycastHit entityHit))
                {
                    GameObject objectHit = entityHit.transform.gameObject;
                    if (objectHit != null)
                    {
                        if(objectHit.layer == World.Layer_Entity)
                        {
                            Entity hitEntity = objectHit.GetComponent<Entity>();
                            if (hitEntity == e) return VisionType.FogOfWar;
                        }
                    }
                }
            }

            // If he ray doesn't hit anything, it's unexplored
            return VisionType.Unexplored;
        }

        /// <summary>
        /// Returns the world position at which the "eyes" of this entity are located.
        /// <br/> Rays for calculating vision are shot from this position.
        /// </summary>
        public Vector3 GetEyePosition()
        {
            return OriginNode.GetCenterWorldPosition() + new Vector3(0f, (Dimensions.y * World.TILE_HEIGHT) - (World.TILE_HEIGHT * 0.5f), 0f);
        }

        public bool CanBePlacedOn(BlockmapNode node) => GetOccupiedNodes(node) != null;

        /// <summary>
        /// Returns if the given player can see this entity.
        /// </summary>
        public bool IsVisibleBy(Player player)
        {
            return OccupiedNodes.Any(x => x.IsVisibleBy(player));
        }
        /// <summary>
        /// Returns if the given player has explored this entity.
        /// </summary>
        public bool IsExploredBy(Player player)
        {
            return OccupiedNodes.Any(x => x.IsExploredBy(player));
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

        #endregion

        #region Setters

        protected void SetOriginNode(BlockmapNode node)
        {
            OriginNode = node;
            UpdateOccupiedNodes();
            UpdateVisibleNodes();
        }

        #endregion

    }
}
