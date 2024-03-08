using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class Entity : MonoBehaviour
    {
        public World World { get; protected set; }

        /// <summary>
        /// Unique identifier of this specific entity.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Unique identifier of an entity type.
        /// <br/> The same id will always result in the same entity attributes (mesh, size, shape, vision, etc.)
        /// </summary>
        public string TypeId { get; protected set; }

        /// <summary>
        /// Display name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Node that the southwest corner of this entity is on at this moment.
        /// </summary>
        public BlockmapNode OriginNode { get; private set; }

        /// <summary>
        /// What direction this entity is facing. [N/E/S/W]
        /// </summary>
        public Direction Rotation { get; private set; }

        /// <summary>
        /// The exact world position this entity is at the moment.
        /// </br> Equals transform.position when the entity is visible (in vision system).
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>
        /// The exact world rotation this entity is rotated at at the moment.
        /// </br> Equals transform.position when the entity is visible (in vision system).
        /// </summary>
        public Quaternion WorldRotation { get; private set; }

        /// <summary>
        /// Stores the exact world position at which each player has seen this entity the last time.
        /// </summary>
        public Dictionary<Actor, Vector3?> LastKnownPosition { get; private set; }
        /// <summary>
        /// Stores the direction that each player has seen this entity facing at the last time.
        /// </summary>
        public Dictionary<Actor, Quaternion?> LastKnownRotation { get; private set; }


        /// <summary>
        /// List of tiles that this entity is currently on.
        /// </summary>
        public HashSet<BlockmapNode> OccupiedNodes { get; private set; }

        /// <summary>
        /// List of all nodes that this entity currently sees.
        /// </summary>
        public HashSet<BlockmapNode> VisibleNodes { get; private set; }

        /// <summary>
        /// Who this entity belongs to.
        /// </summary>
        public Actor Owner { get; private set; }
        /// <summary>
        /// How far this entity can see.
        /// </summary>
        public float VisionRange;

        [SerializeField]
        /// <summary>
        /// Size of this entity in all 3 dimensions.
        /// </summary>
        protected Vector3Int Dimensions;

        /// <summary>
        /// Flag if other entities can move through this entity.
        /// </summary>
        public bool IsPassable;

        /// <summary>
        /// Flag if this entity blocks the vision from other entities.
        /// </summary>
        public bool BlocksVision;

        /// <summary>
        /// Flag if entity can only be placed when the whole footprint is flat.
        /// </summary>
        public bool RequiresFlatTerrain;

        // Visual
        /// <summary>
        /// The index of the material in the MeshRenderer that is colored based on the owner's player color.
        /// <br/> -1 means there is no material.
        /// </summary>
        public int PlayerColorMaterialIndex = -1;

        // Components
        private GameObject Wrapper; // Root GameObject of all GameObjects belonging to this entity
        private MeshRenderer Renderer;
        private Projector SelectionIndicator;
        private MeshCollider MeshCollider; // used for hovering and selecting with cursor
        private BoxCollider VisionCollider; // used for vision checks for entites

        // Performance Profilers
        static readonly ProfilerMarker pm_SetOriginNode = new ProfilerMarker("SetOriginNode");
        static readonly ProfilerMarker pm_UpdateVision = new ProfilerMarker("UpdateVision");
        static readonly ProfilerMarker pm_GetVisibleNodes = new ProfilerMarker("GetVisibleNodes");
        static readonly ProfilerMarker pm_GetNodeVision = new ProfilerMarker("GetNodeVision");
        static readonly ProfilerMarker pm_Look = new ProfilerMarker("Look");

        #region Initialize

        public void Init(int id, World world, BlockmapNode origin, Direction rotation, Actor player)
        {
            Id = id;
            if(string.IsNullOrEmpty(TypeId)) TypeId = Name;

            Renderer = GetComponent<MeshRenderer>();

            OccupiedNodes = new HashSet<BlockmapNode>();
            VisibleNodes = new HashSet<BlockmapNode>();

            LastKnownPosition = new Dictionary<Actor, Vector3?>();
            foreach (Actor p in world.Actors.Values) LastKnownPosition.Add(p, null);
            LastKnownRotation = new Dictionary<Actor, Quaternion?>();
            foreach (Actor p in world.Actors.Values) LastKnownRotation.Add(p, null);

            World = world;
            Owner = player;
            Rotation = rotation;
            WorldPosition = GetWorldPosition(world, origin, rotation);
            WorldRotation = HelperFunctions.Get2dRotationByDirection(Rotation);

            // Create a mesh collider for selecting the entity
            gameObject.layer = World.Layer_EntityMesh;
            MeshCollider = GetComponent<MeshCollider>();
            if (MeshCollider == null) MeshCollider = gameObject.AddComponent<MeshCollider>();

            // Wrap the entity in a wrapper
            Wrapper = new GameObject(Name + "_wrapper");
            Wrapper.transform.SetParent(transform.parent);
            transform.SetParent(Wrapper.transform);

            // Create a collider for entity vision on a seperate object
            GameObject visionColliderObject = new GameObject("visionCollider");
            visionColliderObject.transform.SetParent(Wrapper.transform);
            visionColliderObject.transform.localScale = transform.localScale;
            visionColliderObject.layer = World.Layer_EntityVisionCollider;
            VisionCollider = visionColliderObject.AddComponent<BoxCollider>();
            VisionCollider.size = new Vector3(Dimensions.x / transform.localScale.x, (Dimensions.y * World.TILE_HEIGHT) / transform.localScale.y, Dimensions.z / transform.localScale.z);
            VisionCollider.center = new Vector3(0f, VisionCollider.size.y / 2, 0f);

            // Move entity to spawn position
            SetOriginNode(origin);

            // Selection indicator
            SelectionIndicator = Instantiate(ResourceManager.Singleton.SelectionIndicator);
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            SelectionIndicator.orthographicSize = Mathf.Max(Dimensions.x, Dimensions.z) * 0.5f;
            SetSelected(false);

            // Player color
            if (PlayerColorMaterialIndex != -1) Renderer.materials[PlayerColorMaterialIndex].color = Owner.Color;

            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        public void DestroySelf()
        {
            Destroy(Wrapper);
        }

        #endregion

        #region Update

        /// <summary>
        /// Gets called every frame by the world.
        /// </summary>
        public virtual void UpdateEntity() { }

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
            OccupiedNodes = GetOccupiedNodes();

            // Add entity to all newly occupies nodes and chunks
            foreach (BlockmapNode node in OccupiedNodes)
            {
                node.AddEntity(this);
                node.Chunk.AddEntity(this);
            }
        }

        /// <summary>
        /// Updates all references of what this entity currently sees according to vision and line of sight rules.
        /// </summary>
        public void UpdateVision()
        {
            pm_UpdateVision.Begin();

            // Remove entity vision from previously visible nodes
            HashSet<BlockmapNode> previousVisibleNodes = new HashSet<BlockmapNode>(VisibleNodes);
            foreach (BlockmapNode n in previousVisibleNodes) n.RemoveVisionBy(this);

            // Get list of everything that this entity currently sees
            pm_GetVisibleNodes.Begin();
            GetVisibleNodes(OriginNode, out HashSet<BlockmapNode> visibleNodes, out HashSet<BlockmapNode> exploredNodes, out HashSet<Entity> visibleEntities);
            pm_GetVisibleNodes.End();

            // Update last known position and rotation of all currently visible entities
            foreach (Entity e in visibleEntities) e.UpdateLastKnownPositionFor(Owner);

            // Add entitiy vision to newly visible nodes
            VisibleNodes = visibleNodes;
            foreach (BlockmapNode n in visibleNodes) n.AddVisionBy(this);

            // Set nodes as explored that are explored by this entity but not visible (in fog of war)
            foreach (BlockmapNode n in exploredNodes) n.AddExploredBy(Owner);

            // Find nodes where the visibility changed
            HashSet<BlockmapNode> changedVisibilityNodes = new HashSet<BlockmapNode>(previousVisibleNodes);
            changedVisibilityNodes.SymmetricExceptWith(visibleNodes);
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
            foreach (Chunk c in changedVisibilityChunks) World.OnVisibilityChanged(c, Owner);

            pm_UpdateVision.End();
        }


        #endregion

        #region Draw

        /// <summary>
        /// Shows, hides or tints (fog of war) this entity according to if its visible by the given player.
        /// <br/> Also Moves the entitiy to the last or currently known position for the given player.
        /// </summary>
        public void UpdateVisiblity(Actor player)
        {
            // Entity is currently visible => render normally at current position
            if (IsVisibleBy(player))
            {
                Renderer.enabled = true;
                Renderer.material.SetColor("_TintColor", Color.clear);

                transform.position = WorldPosition;
                transform.rotation = WorldRotation;

                foreach (Material m in Renderer.materials) m.SetFloat("_Transparency", 0);
            }

            // Entity was explored before but not currently visible => render transparent at last known position
            else if (IsExploredBy(player))
            {
                Renderer.enabled = true;
                Renderer.material.SetColor("_TintColor", new Color(0f, 0f, 0f, 0.5f));

                transform.position = LastKnownPosition[player].Value;
                transform.rotation = LastKnownRotation[player].Value;

                foreach (Material m in Renderer.materials) m.SetFloat("_Transparency", 0.5f);
            }

            // Entity was not yet explored => don't render it
            else Renderer.enabled = false;
        }

        #endregion

        #region Getters

        public virtual int MinHeight => World.GetNodeHeight(GetWorldPosition(World, OriginNode, Rotation).y);
        public virtual int MaxHeight => MinHeight + Dimensions.y;
        public int Height => Dimensions.y;
        public float WorldHeight => World.GetWorldHeight(Height);
        public Vector3 WorldSize => Vector3.Scale(GetComponent<MeshFilter>().mesh.bounds.size, transform.localScale);

        public Vector3Int GetDimensions()
        {
            return GetDimensions(Rotation);
        }
        public Vector3Int GetDimensions(Direction rotation)
        {
            if (rotation == Direction.N || rotation == Direction.S) return Dimensions;
            if (rotation == Direction.E || rotation == Direction.W) return new Vector3Int(Dimensions.z, Dimensions.y, Dimensions.x);
            throw new System.Exception(rotation.ToString() + " is not a valid rotation");
        }

        /// <summary>
        /// Returns the world position of this entity when placed on the given originNode.
        /// </summary>
        public virtual Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            // Take origin node as base position
            Vector3Int dimensions = GetDimensions(rotation);
            Vector2 basePosition = originNode.WorldCoordinates + new Vector2(dimensions.x * 0.5f, dimensions.z * 0.5f);

            // For y, take the lowest node center out of all occupied nodes
            HashSet<BlockmapNode> occupiedNodes = GetOccupiedNodes(world, originNode, rotation);
            float y;
            if (occupiedNodes == null) y = world.GetWorldHeightAt(new Vector2(originNode.WorldCoordinates.x + 0.5f, originNode.WorldCoordinates.y + 0.5f), originNode);
            else y = GetOccupiedNodes(world, originNode, rotation).Min(x => world.GetWorldHeightAt(new Vector2(x.WorldCoordinates.x + 0.5f, x.WorldCoordinates.y + 0.5f), x));

            // Final position
            return new Vector3(basePosition.x, y, basePosition.y);
        }

        /// <summary>
        /// Returns the world position of the center of this entity.
        /// </summary>
        public Vector3 GetWorldCenter() => Renderer.bounds.center;

        public HashSet<BlockmapNode> GetOccupiedNodes()
        {
            return GetOccupiedNodes(World, OriginNode, Rotation);
        }
        /// <summary>
        /// Returns all nodes that would be occupied by this entity when placed on the given originNode with the given rotation.
        /// <br/> Returns null if entity can't be placed on that null.
        /// </summary>
        public HashSet<BlockmapNode> GetOccupiedNodes(World world, BlockmapNode originNode, Direction rotation)
        {
            HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>() { originNode };

            Vector3Int dimensions = GetDimensions(rotation);

            // For each x, try to connect all the way up and see if everything is connected
            BlockmapNode yBaseNode = originNode;
            BlockmapNode cornerNodeNW = null;

            for(int x = 0; x < dimensions.x; x++)
            {
                // Try going east
                if (x > 0)
                {
                    Vector2Int eastCoordinates = world.GetWorldCoordinatesInDirection(yBaseNode.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(yBaseNode, x, Direction.E));
                    if (eastNode == null) return null;

                    yBaseNode = eastNode;
                    nodes.Add(yBaseNode);
                }

                BlockmapNode yNode = yBaseNode;
                for(int y = 0; y < dimensions.z - 1; y++)
                {
                    // Try going north
                    Vector2Int northCoordinates = world.GetWorldCoordinatesInDirection(yNode.WorldCoordinates, Direction.N);
                    List<BlockmapNode> candidateNodesNorth = world.GetNodes(northCoordinates);
                    BlockmapNode northNode = candidateNodesNorth.FirstOrDefault(x => world.DoAdjacentHeightsMatch(yNode, x, Direction.N));
                    if (northNode == null) return null;

                    yNode = northNode;
                    nodes.Add(yNode);
                    if (x == 0 && y == dimensions.z - 2) cornerNodeNW = yNode;
                }
            }

            // Now we have all nodes of the footprint
            // Also check if NW -> NE is fully connected to make sure its valid
            if (dimensions.z > 1)
            {
                for (int i = 0; i < dimensions.x - 1; i++)
                {
                    Vector2Int eastCoordinates = world.GetWorldCoordinatesInDirection(cornerNodeNW.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(cornerNodeNW, x, Direction.E));

                    if (eastNode == null) return null;
                    else cornerNodeNW = eastNode;
                }
            }
            
            return nodes;
        }

        /// <summary>
        /// Returns all nodes that would be visible by this entity when placed on the given originNode.
        /// <br/> Additionaly returns a list of nodes that are only explored by this node, but not visible.
        /// <br/> Also returns a list of all entities that are visible by this node.
        /// </summary>
        private void GetVisibleNodes(BlockmapNode originNode, out HashSet<BlockmapNode> visibleNodes, out HashSet<BlockmapNode> exploredNodes, out HashSet<Entity> visibleEntities)
        {
            visibleNodes = new HashSet<BlockmapNode>(OccupiedNodes);
            exploredNodes = new HashSet<BlockmapNode>();
            visibleEntities = new HashSet<Entity>();

            if (VisionRange == 0) return;

            for (int x = (int)(-VisionRange - 1); x <= VisionRange; x++)
            {
                for(int y = (int)(-VisionRange - 1); y <= VisionRange; y++)
                {
                    Vector2Int targetWorldCoordinates = new Vector2Int(originNode.WorldCoordinates.x + x, originNode.WorldCoordinates.y + y);

                    foreach(BlockmapNode targetNode in World.GetNodes(targetWorldCoordinates))
                    {
                        pm_GetNodeVision.Begin();
                        VisionType vision = GetNodeVision(targetNode);
                        pm_GetNodeVision.End();

                        if (vision == VisionType.Visible)
                        {
                            visibleNodes.Add(targetNode);
                            foreach (Entity e in targetNode.Entities) visibleEntities.Add(e);
                        }
                        else if (vision == VisionType.FogOfWar) exploredNodes.Add(targetNode);
                    }
                }
            }
        }

        /// <summary>
        /// Returns if the given node is currently visible, in fog of war or unexplored by this entity.
        /// </summary>
        public VisionType GetNodeVision(BlockmapNode targetNode)
        {
            // Ignore water since its rendered based on its surface node anyway - so this value is discarded
            if (targetNode is WaterNode) return VisionType.Visible;

            // Check if node is out of 2d vision range (quick check to increase performance)
            float distance = Vector2.Distance(OriginNode.WorldCoordinates, targetNode.WorldCoordinates);
            if (distance > VisionRange) return VisionType.Unexplored;

            bool markAsExplored = false;

            Vector3 nodeCenter = targetNode.GetCenterWorldPosition();
            // Shoot ray from eye to the node with infinite range and check if we hit the correct node
            pm_Look.Begin();
            RaycastHit? nodeHit = Look(nodeCenter);
            pm_Look.End();

            if (nodeHit != null)
            {
                RaycastHit hit = (RaycastHit)nodeHit;

                // Get hit position and coordinates
                Vector3 hitPosition = hit.point;
                Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);

                // Check if the seen object is at the correct height
                int seenYCoordinate = (int)(hitPosition.y / World.TILE_HEIGHT);
                if (seenYCoordinate >= targetNode.BaseHeight || seenYCoordinate <= targetNode.MaxHeight)
                {
                    float epsilon = 0.01f;
                    float xFrac = hitPosition.x % 1f;
                    float yFrac = hitPosition.z % 1f;

                    // Position we hit matches the position of the node we are checking
                    if (hitWorldCoordinates == targetNode.WorldCoordinates)
                    {
                        // If we are not close to hitting an edge, mark the node as visible
                        if (xFrac > epsilon && xFrac < 1f - epsilon && yFrac > epsilon && yFrac < 1f - epsilon) return VisionType.Visible;

                        // If we are on node edge, mark it as explored but not visible (i.e. cliffs)
                        else markAsExplored = true;
                    }

                    // Also make checks for hitting a node edge on nodes adjacent to the one we are checking. If so, mark is as explored
                    if (xFrac < epsilon && yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(-1, -1)) == targetNode.WorldCoordinates) markAsExplored = true; // SW
                    else if (xFrac > 1f - epsilon && yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(1, -1)) == targetNode.WorldCoordinates) markAsExplored = true; // SE
                    else if (xFrac > 1f - epsilon && yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(1, 1)) == targetNode.WorldCoordinates) markAsExplored = true; // NE
                    else if (xFrac < epsilon && yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(-1, 1)) == targetNode.WorldCoordinates) markAsExplored = true; // NW
                    else if (xFrac < epsilon && (hitWorldCoordinates + new Vector2Int(-1, 0)) == targetNode.WorldCoordinates) markAsExplored = true; // W
                    else if (xFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(1, 0)) == targetNode.WorldCoordinates) markAsExplored = true; // E
                    else if (yFrac > 1f - epsilon && (hitWorldCoordinates + new Vector2Int(0, 1)) == targetNode.WorldCoordinates) markAsExplored = true; // N
                    else if (yFrac < epsilon && (hitWorldCoordinates + new Vector2Int(0, -1)) == targetNode.WorldCoordinates) markAsExplored = true; // S
                }

                // Check if we hit the waterbody that covers the node. if so => visible
                if(hit.transform.gameObject.layer == World.Layer_Water && targetNode is SurfaceNode _surfaceNode)
                {
                    if(_surfaceNode.WaterNode != null && World.GetWaterNode(hitWorldCoordinates).WaterBody == _surfaceNode.WaterNode.WaterBody) return VisionType.Visible;
                }

                // Check if we hit an entity on the node. if so => visible
                if (hit.transform.gameObject.layer == World.Layer_EntityVisionCollider)
                {
                    Entity hitEntity = hit.transform.parent.GetComponentInChildren<Entity>();
                    if (targetNode.Entities.Contains(hitEntity)) return VisionType.Visible;
                }
            }

            // If the node has a water body, shoot a ray at the water surface as well
            if(targetNode is SurfaceNode surfaceNode && surfaceNode.WaterNode != null)
            {
                Vector3 targetPos = new Vector3(nodeCenter.x, surfaceNode.WaterNode.WaterBody.WaterSurfaceWorldHeight, nodeCenter.z);
                RaycastHit? waterHit = Look(targetPos);

                if (waterHit != null)
                {
                    RaycastHit hit = (RaycastHit)waterHit;

                    if (hit.transform.gameObject.layer == World.Layer_Water)
                    {
                        Vector3 hitPosition = hit.point;
                        Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);
                        if(World.GetWaterNode(hitWorldCoordinates).WaterBody == surfaceNode.WaterNode.WaterBody) return VisionType.Visible;
                    }
                }
            }

            // If the node has entities shoot a ray at them. If we hit one, mark the node as Fog of War (because we can't see the node directly but the entity that's on it.)
            foreach (Entity e in targetNode.Entities)
            {
                RaycastHit? entityHit = Look(e.GetWorldCenter());

                if(entityHit != null)
                {
                    RaycastHit hit = (RaycastHit)entityHit;
                    GameObject objectHit = hit.transform.gameObject;

                    if (objectHit.layer == World.Layer_EntityVisionCollider && objectHit.transform.parent.GetComponentInChildren<Entity>() == e)
                        markAsExplored = true;
                }
            }



            // No check was successful => unexplored
            if (markAsExplored) return VisionType.FogOfWar;
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

            Ray ray = new Ray(source, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, VisionRange);
            System.Array.Sort(hits, (a, b) => (a.distance.CompareTo(b.distance))); // sort hits by distance

            foreach (RaycastHit hit in hits)
            {
                GameObject objectHit = hit.transform.gameObject;

                // If the thing we hit is ourselves, go to the next thing
                if (objectHit.transform.parent == transform.parent) continue;

                // If the thing we hit is an entity mesh, just continue. For entities we only check their vision collider
                if (objectHit.layer == World.Layer_EntityMesh) continue;

                // If the thing we hit is an entity vision collider that doesn't block vision, go to the next thing we hit
                if (objectHit.layer == World.Layer_EntityVisionCollider && !objectHit.transform.parent.GetComponentInChildren<Entity>().BlocksVision) continue;

                // If the thing we hit is a wall that doesn't block vision, go to the next thing we hit
                if (objectHit.layer == World.Layer_Wall)
                {
                    Wall hitWall = World.GetWallFromRaycastHit(hit);
                    if (hitWall != null && !hitWall.Type.BlocksVision) continue;
                }

                // Debug.DrawRay(source, hit.point - source, Color.red, 60f);

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

        /// <summary>
        /// Returns if the given player can see this entity.
        /// </summary>
        public bool IsVisibleBy(Actor player)
        {
            if (player == null) return true; // Everything is visible
            if (player == Owner) return true; // The own entities of a player are always visible

            // Entity is visible when any of the nodes it's standing on is visible
            return OccupiedNodes.Any(x => x.IsVisibleBy(player));
        }

        /// <summary>
        /// Returns if the given player has seen this entity before.
        /// </summary>
        public bool IsExploredBy(Actor player) => LastKnownPosition[player] != null;

        #endregion

        #region Setters

        /// <summary>
        /// Instantly teleports this entity to the given node.
        /// </summary>
        public void Teleport(BlockmapNode targetNode)
        {
            WorldPosition = GetWorldPosition(World, targetNode, Rotation);
            SetOriginNode(targetNode);

            if (BlocksVision) World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.GetCenterWorldPosition()); // Recalculate vision of all nearby entities when blocking vision
            else UpdateVision(); // Only calculate own vision when being see-through
        }

        /// <summary>
        /// Changes the origin node and updates all relevant information with it.
        /// </summary>
        protected void SetOriginNode(BlockmapNode node)
        {
            pm_SetOriginNode.Begin();

            // Before setting new origin, update last known position for all players seeing this entity
            foreach (Actor p in World.Actors.Values)
                if (IsVisibleBy(p)) UpdateLastKnownPositionFor(p);

            // Set new origin
            OriginNode = node;
            UpdateOccupiedNodes();

            // Update visibility since it could have gone out of / into vision
            UpdateVisiblity(World.ActiveVisionActor);

            // Update position of vision collider
            VisionCollider.transform.position = GetWorldPosition(World, OriginNode, Rotation);
            VisionCollider.transform.rotation = HelperFunctions.Get2dRotationByDirection(Rotation);

            pm_SetOriginNode.End();
        }

        /// <summary>
        /// Sets the exact position the entity's transform is currently at.
        /// <br/> Only gets rendered there if actually visible.
        /// </summary>
        public void SetWorldPosition(Vector3 pos)
        {
            WorldPosition = pos;
        }
        /// <summary>
        /// Sets the direction this entity is currently facing.
        /// <br/> Only gets rendered in that rotation there if actually visible.
        /// </summary>
        public void SetWorldRotation(Quaternion quat)
        {
            WorldRotation = quat;
        }

        public void UpdateLastKnownPositionFor(Actor p)
        {
            LastKnownPosition[p] = WorldPosition;
            LastKnownRotation[p] = WorldRotation;
        }
        public void ResetLastKnownPositionFor(Actor p)
        {
            LastKnownPosition[p] = null;
            LastKnownRotation[p] = null;
        }

        /// <summary>
        /// Shows/hides the selection indicator of this entity.
        /// </summary>
        public void SetSelected(bool value)
        {
            SelectionIndicator.gameObject.SetActive(value);
        }

        #endregion

        #region Save / Load

        public static Entity Load(World world, EntityData data)
        {
            if(data.TypeId.StartsWith(LadderEntity.LADDER_ENTITY_NAME))
            {
                string[] attributes = data.TypeId.Split('_');
                int targetNodeId = int.Parse(attributes[1]);
                world.BuildLadder(world.GetNode(data.OriginNodeId), world.GetNode(targetNodeId), data.Rotation);
                return null;
            }

            Entity instance = world.ContentLibrary.GetEntityInstance(world, data.TypeId);
            instance.Init(data.Id, world, world.GetNode(data.OriginNodeId), data.Rotation, world.Actors[data.PlayerId]);
            return instance;
        }

        public EntityData Save()
        {
            return new EntityData
            {
                Id = Id,
                TypeId = TypeId,
                OriginNodeId = OriginNode.Id,
                Rotation = Rotation,
                PlayerId = Owner.Id
            };
        }

        #endregion

    }
}
