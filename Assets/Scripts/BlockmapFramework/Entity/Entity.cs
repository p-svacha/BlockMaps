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
        public BlockmapNode OriginNode { get; private set; }

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

        private Projector SelectionIndicator;

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

            // Collider
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(Dimensions.x / transform.localScale.x, (Dimensions.y * World.TILE_HEIGHT) / transform.localScale.y, Dimensions.z / transform.localScale.z);
            collider.center = new Vector3(0f, collider.size.y / 2, 0f);

            // Selection indicator
            SelectionIndicator = Instantiate(ResourceManager.Singleton.SelectionIndicator);
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = new Vector3(0f, 5f, 0f);
            SelectionIndicator.orthographicSize = Mathf.Max(Dimensions.x, Dimensions.z) * 0.5f;
            SetSelected(false);

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
                t.Chunk.RemoveEntity(this);
            }

            // Get list of nodes that are currently occupied
            OccupiedNodes = GetOccupiedNodes(OriginNode);

            // Add entity to all newly occupies nodes and chunks
            foreach (BlockmapNode node in OccupiedNodes)
            {
                node.AddEntity(this);
                node.Chunk.AddEntity(this);
            }
        }

        /// <summary>
        /// Sets VisibleNodes according to vision and line of sight rules.
        /// </summary>
        public void UpdateVisibleNodes()
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
            //Debug.Log("Visiblity of " + changedVisibilityNodes.Count + " nodes changed."); 

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
        /// Returns the world position of the center of this entity.
        /// </summary>
        public Vector3 GetWorldCenter() => GetComponent<MeshRenderer>().bounds.center;

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

            if (VisionRange == 0) return new List<BlockmapNode>(OccupiedNodes);

            for (int x = (int)(-VisionRange - 1); x <= VisionRange; x++)
            {
                for(int y = (int)(-VisionRange - 1); y <= VisionRange; y++)
                {
                    Vector2Int targetWorldCoordinates = new Vector2Int(originNode.WorldCoordinates.x + x, originNode.WorldCoordinates.y + y);

                    foreach(BlockmapNode targetNode in World.GetNodes(targetWorldCoordinates))
                    {
                        VisionType vision = GetNodeVision(targetNode);
                        if (vision == VisionType.Visible) visibleNodes.Add(targetNode);
                        else if (vision == VisionType.FogOfWar) exploredNodes.Add(targetNode);
                    }
                }
            }

            return visibleNodes;
        }

        /// <summary>
        /// Returns if the given node is currently visible, in fog of war or unexplored by this entity.
        /// </summary>
        public VisionType GetNodeVision(BlockmapNode node)
        {
            // Check if node is out of 2d vision range (quick check to increase performance)
            float distance = Vector2.Distance(OriginNode.WorldCoordinates, node.WorldCoordinates);
            if (distance > VisionRange) return VisionType.Unexplored;

            // Shoot ray from eye to the node with infinite range and check if we hit the correct node
            RaycastHit? nodeHit = Look(node.GetCenterWorldPosition());

            if (nodeHit != null)
            {
                RaycastHit hit = (RaycastHit)nodeHit;

                // Get hit position and coordinates
                Vector3 hitPosition = hit.point;
                Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);

                // Check if the seen object is at the correct height
                int seenYCoordinate = (int)(hitPosition.y / World.TILE_HEIGHT);
                if (seenYCoordinate >= node.BaseHeight || seenYCoordinate <= node.MaxHeight)
                {

                    // If the position we hit matches the position of the node we are checking, mark it as visible
                    if (hitWorldCoordinates == node.WorldCoordinates) return VisionType.Visible;

                    // Also mark it as visible when we are very close to hitting the correct node (needed for cliffs)
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
                }
            }


            // If the node has entities shoot a ray at them. If we hit one, mark the node as Fog of War (because we can't see the node directly but the entity that's on it.)
            foreach(Entity e in node.Entities)
            {
                RaycastHit? entityHit = Look(e.GetWorldCenter());

                if(entityHit != null)
                {
                    RaycastHit hit = (RaycastHit)entityHit;
                    GameObject objectHit = hit.transform.gameObject;

                    if (objectHit.layer == World.Layer_Entity && objectHit.GetComponent<Entity>() == e)
                        return VisionType.FogOfWar;
                }
            }

            // No check was successful => unexplored
            return VisionType.Unexplored;
        }

        /// <summary>
        /// Shoots a vision ray from this entity's eyes at the given target position.
        /// <br/> Returns the object and exact position on it that is currently seen by this entity.
        /// <br/> Returns null if nothing was hit.
        /// </summary>
        private RaycastHit? Look(Vector3 targetPosition)
        {
            // Create a ray from eye to target with VisionRange as max range
            Vector3 source = GetEyePosition();
            Vector3 direction = targetPosition - source;

            RaycastHit[] hits = Physics.RaycastAll(source, direction, VisionRange);
            System.Array.Sort(hits, (a, b) => (a.distance.CompareTo(b.distance))); // sort hits by distance

            foreach (RaycastHit hit in hits)
            {
                GameObject objectHit = hit.transform.gameObject;

                // If the thing we hit is ourselves, go to the next thing
                if (objectHit == gameObject) continue;

                // If the thing we hit is an entity that doesn't block vision, go to the next thing we hit
                if (objectHit.layer == World.Layer_Entity && !objectHit.GetComponent<Entity>().BlocksVision) continue;

                //Debug.DrawRay(source, hit.point - source, Color.red, 60f);

                // Return it since it's the first thing that we actually see
                return hit;
            }

            // Nothing was hit
            return null;
        }

        /// <summary>
        /// Returns the world position at which the "eyes" of this entity currently located.
        /// <br/> Rays for calculating vision are shot from this position.
        /// </summary>
        public Vector3 GetEyePosition()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("Eye position not yet implemented for entities bigger than 1x1");

            return OriginNode.GetCenterWorldPosition() + new Vector3(0f, (Dimensions.y * World.TILE_HEIGHT) - (World.TILE_HEIGHT * 0.5f), 0f);
        }

        public bool CanBePlacedOn(BlockmapNode node) => GetOccupiedNodes(node) != null;

        /// <summary>
        /// Returns if the given player can see this entity.
        /// </summary>
        public bool IsVisibleBy(Player player)
        {
            if (player == Player) return true; // The own entities of a player are always visible

            // Entity is visible when any of the nodes it's standing on is visible
            return OccupiedNodes.Any(x => x.IsVisibleBy(player));
        }
        /// <summary>
        /// Returns if the given player has explored this entity.
        /// </summary>
        public bool IsExploredBy(Player player)
        {
            if (player == Player) return true; // The own entities of a player are always explored

            // Entity is explored when any of the nodes it's standing on is explored
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

        /// <summary>
        /// Shows/hides the selection indicator of this entity.
        /// </summary>
        public void SetSelected(bool value)
        {
            SelectionIndicator.gameObject.SetActive(value);
        }

        #endregion

    }
}
