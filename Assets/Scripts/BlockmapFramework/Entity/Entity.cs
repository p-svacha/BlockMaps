using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// All objects that exist in the world in a specific location (node) are entities.
    /// <br/>Together with nodes they make up everything in the world.
    /// </summary>
    public abstract class Entity : MonoBehaviour, IVisionTarget
    {
        /// <summary>
        /// The world in which this entity exists.
        /// </summary>
        public World World { get; protected set; }

        /// <summary>
        /// The blueprint that defines the looks, attributes and behaviour rules of this entity.
        /// </summary>
        public EntityDef Def { get; private set; }

        /// <summary>
        /// Unique identifier of this specific entity.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The Unity GameObject that this thing is rendered in.
        /// </summary>
        public GameObject GameObject;

        /// <summary>
        /// Identifier used for instancing the correct type of entity.
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
        /// <br/> Equals transform.position when the entity is visible (in vision system).
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>
        /// The exact world rotation this entity is rotated at at the moment.
        /// <br/> Equals transform.position when the entity is visible (in vision system).
        /// </summary>
        public Quaternion WorldRotation { get; protected set; }

        /// <summary>
        /// List of tiles that this entity is currently on.
        /// </summary>
        public HashSet<BlockmapNode> OccupiedNodes { get; private set; }

        /// <summary>
        /// List of all objects that are currenlty visible or half-visible by this entity.
        /// <br/>Half-visible means that the object will be marked as explored but is not currently in vision.
        /// </summary>
        public VisionData CurrentVision { get; private set; }

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
        protected GameObject Wrapper; // Root GameObject of all GameObjects belonging to this entity
        private MeshRenderer Renderer;
        public MeshCollider MeshCollider { get; protected set; } // used for hovering and selecting with cursor
        public Collider VisionCollider { get; protected set; } // used for vision checks for entites

        // Performance Profilers
        static readonly ProfilerMarker pm_SetOriginNode = new ProfilerMarker("SetOriginNode");
        static readonly ProfilerMarker pm_UpdateVision = new ProfilerMarker("UpdateVision");
        static readonly ProfilerMarker pm_GetCurrentVision = new ProfilerMarker("GetCurrentVision");
        static readonly ProfilerMarker pm_Look = new ProfilerMarker("Look");
        static readonly ProfilerMarker pm_AggregateVisionData = new ProfilerMarker("Aggregate Vision Data");
        static readonly ProfilerMarker pm_GetWorldCenters = new ProfilerMarker("Get World Centers");
        static readonly ProfilerMarker pm_GetTargetNodes = new ProfilerMarker("Get Target Nodes");
        static readonly ProfilerMarker pm_GetTargetWalls = new ProfilerMarker("Get Target Walls");
        static readonly ProfilerMarker pm_HandleVisibilityChange = new ProfilerMarker("HandleVisibilityChange");

        #region Initialize

        public void Init(int id, World world, BlockmapNode origin, Direction rotation, Actor player)
        {
            Id = id;
            if (string.IsNullOrEmpty(TypeId)) TypeId = Name;

            Renderer = GetComponent<MeshRenderer>();

            OccupiedNodes = new HashSet<BlockmapNode>();
            CurrentVision = new VisionData();

            LastKnownPosition = new Dictionary<Actor, Vector3?>();
            foreach (Actor p in world.GetAllActors()) LastKnownPosition.Add(p, null);
            LastKnownRotation = new Dictionary<Actor, Quaternion?>();
            foreach (Actor p in world.GetAllActors()) LastKnownRotation.Add(p, null);

            World = world;
            Owner = player;
            Rotation = rotation;
            WorldPosition = GetWorldPosition(world, origin, rotation);
            WorldRotation = HelperFunctions.Get2dRotationByDirection(Rotation);

            // Create a mesh collider for selecting the entity
            gameObject.layer = World.Layer_EntityMesh;
            MeshCollider = GetComponent<MeshCollider>();
            if (MeshCollider == null && GetComponent<MeshRenderer>() != null) MeshCollider = gameObject.AddComponent<MeshCollider>();

            // Wrap the entity in a wrapper
            Wrapper = new GameObject(Name + "_wrapper");
            Wrapper.transform.SetParent(World.transform);
            transform.SetParent(Wrapper.transform);

            // Create a collider for entity vision on a seperate object
            CreateVisionCollider();

            // Move entity to spawn position
            SetOriginNode(origin);

            // Player color
            if (PlayerColorMaterialIndex != -1) Renderer.materials[PlayerColorMaterialIndex].color = Owner.Color;

            OnInitialized();
        }

        /// <summary>
        /// Gets called when an entity gets registered in the world. Useful if subtypes need additional registering in specific places.
        /// </summary>
        public virtual void OnRegister() { }
        /// <summary>
        /// Gets called when an entity gets de-registered from the world. Useful if subtypes need additional de-registering in specific places.
        /// </summary>
        public virtual void OnDeregister() { }

        /// <summary>
        /// Creates the box collider(s) for this entity that are used to calculate the vision of other entities around it and adds them to the wrapper.
        /// </summary>
        protected virtual void CreateVisionCollider()
        {
            GameObject visionColliderObject = new GameObject("visionCollider");
            visionColliderObject.transform.SetParent(Wrapper.transform);
            visionColliderObject.transform.localScale = transform.localScale;
            visionColliderObject.layer = World.Layer_EntityVisionCollider;
            BoxCollider collider = visionColliderObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(Dimensions.x / transform.localScale.x, (Dimensions.y * World.TILE_HEIGHT) / transform.localScale.y, Dimensions.z / transform.localScale.z);
            collider.center = new Vector3(0f, collider.size.y / 2, 0f);
            VisionCollider = collider;
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
        /// Updates all references of what this entity currently sees according to its vision range and line of sight rules.
        /// </summary>
        public void UpdateVision(bool debugVisionRays = false)
        {
            if (!CanSee) return; // This entity cannot see
            pm_UpdateVision.Begin();

            // Remove entity vision from previously visible nodes, entities and walls
            VisionData previousVision = CurrentVision;
            foreach (BlockmapNode n in previousVision.VisibleNodes) n.RemoveVisionBy(this);
            foreach (Entity e in previousVision.VisibleEntities) e.RemoveVisionBy(this);
            foreach (Wall w in previousVision.VisibleWalls) w.RemoveVisionBy(this);

            // Get list of everything that this entity currently sees
            pm_GetCurrentVision.Begin();
            CurrentVision = GetCurrentVision(debugVisionRays);
            pm_GetCurrentVision.End();

            pm_HandleVisibilityChange.Begin();
            // Add entitiy vision to visible nodes
            //Debug.Log(CurrentVision.VisibleNodes.Count + " nodes are visible");
            foreach (BlockmapNode n in CurrentVision.VisibleNodes) n.AddVisionBy(this);

            // Set nodes as explored that are explored by this entity but not visible (in fog of war)
            //Debug.Log(CurrentVision.ExploredNodes.Count + " nodes are explored");
            foreach (BlockmapNode n in CurrentVision.ExploredNodes) n.AddExploredBy(Owner);

            // Update last known position and rotation of all currently visible entities
            //Debug.Log(CurrentVision.VisibleEntities.Count + " entities are visible");
            foreach (Entity e in CurrentVision.VisibleEntities) e.AddVisionBy(this);

            // Add entity vision to visible walls
            foreach (Wall w in CurrentVision.VisibleWalls) w.AddVisionBy(this);

            // Set walls as explored
            foreach (Wall w in CurrentVision.ExploredWalls) w.AddExploredBy(Owner);

            // Find nodes where the visibility changed
            HashSet<BlockmapNode> changedVisibilityNodes = new HashSet<BlockmapNode>(previousVision.VisibleNodes.Concat(previousVision.ExploredNodes));
            changedVisibilityNodes.SymmetricExceptWith(CurrentVision.VisibleNodes.Concat(CurrentVision.ExploredNodes));
            //Debug.Log("Visiblity of " + changedVisibilityNodes.Count + " nodes changed."); 

            // Add all adjacent nodes as well because vision goes over node edge
            HashSet<BlockmapNode> adjNodes = new HashSet<BlockmapNode>();
            foreach (BlockmapNode n in changedVisibilityNodes)
            {
                foreach (Direction dir in HelperFunctions.GetAllDirections8())
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
            //Debug.Log("Visibility of " + changedVisibilityChunks.Count + " chunks changed.");
            foreach (Chunk c in changedVisibilityChunks) World.OnVisibilityChanged(c, Owner);

            pm_HandleVisibilityChange.End();

            pm_UpdateVision.End();
        }


        #endregion

        #region Vision Target

        /// <summary>
        /// List containing all entities that currently see this entity.
        /// </summary>
        protected HashSet<Entity> SeenBy = new HashSet<Entity>();

        /// <summary>
        /// Stores the exact world position at which each player has seen this entity the last time.
        /// </summary>
        public Dictionary<Actor, Vector3?> LastKnownPosition { get; private set; }
        /// <summary>
        /// Stores the direction that each player has seen this entity facing at the last time.
        /// </summary>
        public Dictionary<Actor, Quaternion?> LastKnownRotation { get; private set; }

        public void AddVisionBy(Entity e)
        {
            UpdateLastKnownPositionFor(e.Owner);
            SeenBy.Add(e);
        }
        public void RemoveVisionBy(Entity e)
        {
            SeenBy.Remove(e);
        }

        public bool IsVisibleBy(Entity e)
        {
            return SeenBy.Contains(e);
        }
        public bool IsVisibleBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            if (Owner == actor) return true; // This entity belongs to the given actor
            if (OccupiedNodes.Any(n => n.Zones.Any(z => z.ProvidesVision && z.Actor == actor))) return true; // Entity is inside a zone of actor that provides vision
            if (SeenBy.FirstOrDefault(x => x.Owner == actor) != null) return true; // Entity is seen by an entity of given actor

            return false;
        }

        public bool IsExploredBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            return LastKnownPosition[actor] != null; // There is a last known position for the given actor meaning they have discovered this entity
        }

        #endregion

        #region Draw

        /// <summary>
        /// Updates the visibility according to the current active vision actor.
        /// </summary>
        public void UpdateVisibility() => UpdateVisibility(World.ActiveVisionActor);

        /// <summary>
        /// Shows, hides or tints (fog of war) this entity according to if its visible by the given player.
        /// <br/> Also Moves the entitiy to the last or currently known position for the given player.
        /// </summary>
        public virtual void UpdateVisibility(Actor player)
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

        public virtual string Label => Def.Label;
        public virtual string LabelCap => Label.CapitalizeFirst();
        public virtual string Description => Def.Description;

        public int MinAltitude => Mathf.FloorToInt(GetWorldPosition(World, OriginNode, Rotation).y / World.TILE_HEIGHT); // Rounded down to y-position of its center
        public int MaxAltitude => Mathf.CeilToInt((GetWorldPosition(World, OriginNode, Rotation).y / World.TILE_HEIGHT) + (Height - 1)); // Rounded up to y-position of its center + height
        public int Height => Dimensions.y;
        public float WorldHeight => World.GetWorldHeight(Height);
        public Vector3 WorldSize => Vector3.Scale(GetComponent<MeshFilter>().mesh.bounds.size, transform.localScale);
        public bool CanSee => VisionRange > 0;

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
        /// <br/>By default this is in the center of the entity in the x and z axis and on the bottom in the y axis.
        /// </summary>
        public virtual Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            // Take origin node as base position
            Vector3Int dimensions = GetDimensions(rotation);
            Vector2 basePosition = originNode.WorldCoordinates + new Vector2(dimensions.x * 0.5f, dimensions.z * 0.5f);

            // For y, take the lowest node center out of all occupied nodes
            HashSet<BlockmapNode> occupiedNodes = GetOccupiedNodes(world, originNode, rotation);
            float y;
            if (occupiedNodes == null) y = originNode.GetWorldHeightAt(new Vector2(0.5f, 0.5f));
            else y = GetOccupiedNodes(world, originNode, rotation).Min(x => x.GetWorldHeightAt(new Vector2(0.5f, 0.5f)));

            // Final position
            return new Vector3(basePosition.x, y, basePosition.y);
        }

        /// <summary>
        /// Returns the world position of the center of this entity.
        /// </summary>
        public virtual Vector3 GetWorldCenter() => GetWorldPosition(World, OriginNode, Rotation) + new Vector3(0f, WorldHeight / 2f, 0f);

        /// <summary>
        /// Returns the translated local coordinate in x/y direction depending the rotation of the entity.
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetLocalPosition(Vector2Int localCoord)
        {
            return Rotation switch
            {
                Direction.N => localCoord,
                Direction.E => new Vector2Int(localCoord.y, Dimensions.z - localCoord.y - 1),
                Direction.S => new Vector2Int(Dimensions.x - localCoord.x - 1, Dimensions.z - localCoord.y - 1),
                Direction.W => new Vector2Int(Dimensions.z - localCoord.y - 1, localCoord.x),
                _ => throw new System.Exception("invalid direction")
            };
        }

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

            for (int x = 0; x < dimensions.x; x++)
            {
                // Try going east
                if (x > 0)
                {
                    Vector2Int eastCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(yBaseNode.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(yBaseNode, x, Direction.E));
                    if (eastNode == null) return null;

                    yBaseNode = eastNode;
                    nodes.Add(yBaseNode);
                }

                BlockmapNode yNode = yBaseNode;
                for (int y = 0; y < dimensions.z - 1; y++)
                {
                    // Try going north
                    Vector2Int northCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(yNode.WorldCoordinates, Direction.N);
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
                    Vector2Int eastCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(cornerNodeNW.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(cornerNodeNW, x, Direction.E));

                    if (eastNode == null) return null;
                    else cornerNodeNW = eastNode;
                }
            }

            return nodes;
        }

        /// <summary>
        /// Shoots rays from the entity's current position towards all nodes, entities and walls within vision range.
        /// <br/>Returns several lists containing information about what objects are currenlty visible or should be marked as explored.
        /// </summary>
        private VisionData GetCurrentVision(bool debugVisionRays = false)
        {
            VisionData finalVision = new VisionData();
            if (!CanSee) return finalVision; // This entity cannot see

            // Cache some values
            float visionRangeSquared = VisionRange * VisionRange;
            Vector2Int originCoordinates = OriginNode.WorldCoordinates;
            List<Entity> checkedEntities = new List<Entity>();
            Vector3 visionRaySource = GetEyePosition();

            // Iterate through all world coordinates that could possibly be within vision range
            int start = (int)(-VisionRange - 1);
            for (int x = start; x <= VisionRange; x++)
            {
                for (int y = start; y <= VisionRange; y++)
                {
                    Vector2Int targetWorldCoordinates = new Vector2Int(originCoordinates.x + x, originCoordinates.y + y);
                    if (!World.IsInWorld(targetWorldCoordinates)) continue;

                    // Shoot ray at all nodes on coordinate
                    pm_GetTargetNodes.Begin();
                    List<BlockmapNode> targetNodes = World.GetNodes(targetWorldCoordinates);
                    pm_GetTargetNodes.End();
                    foreach (BlockmapNode targetNode in targetNodes)
                    {
                        if ((targetNode.CenterWorldPosition - visionRaySource).sqrMagnitude > visionRangeSquared) continue;

                        pm_Look.Begin();
                        VisionData nodeRayVision = Look(visionRaySource, targetNode.CenterWorldPosition, debugVisionRays);
                        pm_Look.End();

                        pm_AggregateVisionData.Begin();
                        finalVision.AddVisionData(nodeRayVision);
                        pm_AggregateVisionData.End();

                        // Shoot ray all entities on node
                        foreach (Entity e in targetNode.Entities)
                        {
                            if (e.Owner == Owner) continue; // Don't check for entities of the same actor since they see them anyway
                            if (checkedEntities.Contains(e)) continue;

                            pm_GetWorldCenters.Begin();
                            Vector3 entityCenter = e.GetWorldCenter();
                            pm_GetWorldCenters.End();
                            if ((entityCenter - visionRaySource).sqrMagnitude > visionRangeSquared) continue;
                            

                            pm_Look.Begin();
                            VisionData entityRayVision = Look(visionRaySource, entityCenter, debugVisionRays);
                            pm_Look.End();

                            pm_AggregateVisionData.Begin();
                            finalVision.AddVisionData(entityRayVision);
                            checkedEntities.Add(e);
                            pm_AggregateVisionData.End();
                        }
                    }

                    // Shoot a ray at all walls on coordinate
                    pm_GetTargetWalls.Begin();
                    List<Wall> targetWalls = World.GetWalls(targetWorldCoordinates);
                    pm_GetTargetWalls.End();
                    foreach (Wall wall in targetWalls)
                    {
                        pm_GetWorldCenters.Begin();
                        Vector3 wallCenter = wall.GetCenterWorldPosition();
                        pm_GetWorldCenters.End();
                        if ((wallCenter - visionRaySource).sqrMagnitude > visionRangeSquared) continue;

                        pm_Look.Begin();
                        VisionData wallRayVision = Look(visionRaySource, wallCenter, debugVisionRays);
                        pm_Look.End();

                        pm_AggregateVisionData.Begin();
                        finalVision.AddVisionData(wallRayVision);
                        pm_AggregateVisionData.End();
                    }
                }
            }

            return finalVision;
        }

        /// <summary>
        /// Shoots a vision ray from this entity's eyes at the given target world position with a range equal to this entity's vision range.
        /// <br/> Returns the vision data containing all objects that are visible and explored from this raycast.
        /// </summary>
        private VisionData Look(Vector3 sourcePosition, Vector3 targetPosition, bool debugVisionRay = false)
        {
            VisionData vision = new VisionData();

            // Create a ray from eye to target with VisionRange as max range
            Vector3 direction = targetPosition - sourcePosition;

            Ray ray = new Ray(sourcePosition, direction);
            int layerMask = (1 << World.Layer_GroundNode) | (1 << World.Layer_AirNode) |
                    (1 << World.Layer_Water) | (1 << World.Layer_EntityVisionCollider) |
                    (1 << World.Layer_Fence) | (1 << World.Layer_Wall);
            RaycastHit[] hits = Physics.RaycastAll(ray, VisionRange, layerMask);
            System.Array.Sort(hits, (a, b) => (a.distance.CompareTo(b.distance))); // sort hits by distance

            // Debug
            if (debugVisionRay)
            {
                Color debugColor = Color.red;
                if (hits.Length > 0) debugColor = Color.blue;
                Debug.DrawRay(sourcePosition, direction, debugColor, 60f);
            }

            foreach (RaycastHit hit in hits)
            {
                GameObject objectHit = hit.transform.gameObject;

                // If the thing we hit is ourselves, go to the next thing
                if (objectHit.transform.parent == transform.parent) continue;

                // Hit ground node
                if(objectHit.layer == World.Layer_GroundNode)
                {
                    // Get position of where we hit
                    Vector3 hitPosition = hit.point;
                    Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);
                    GroundNode hitGroundNode = World.GetGroundNode(hitWorldCoordinates);

                    // Check if we hit close to a node edge (necessary because it is likely that we hit a cliff)
                    float epsilon = 0.01f;
                    float xFrac = hitPosition.x % 1f;
                    float yFrac = hitPosition.z % 1f;
                    bool isCloseToEdge = (xFrac < epsilon || xFrac > 1f - epsilon || yFrac < epsilon || yFrac > 1f - epsilon);

                    // If we are clearly on a node (as in not close to an edge), mark this node as visible and stop the search
                    if (!isCloseToEdge)
                    {
                        vision.AddVisibleNode(hitGroundNode);
                        return vision;
                    }

                    // If we are close to an edge, mark all nodes close to the edge as explored and stop the search
                    else
                    {
                        vision.AddExploredNode(hitGroundNode);
                        if (xFrac < epsilon && yFrac < epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.SW));
                        if (xFrac < epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.W));
                        if (xFrac < epsilon && yFrac > 1f - epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.NW));
                        if (yFrac > 1f - epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.N));
                        if (xFrac > 1f - epsilon && yFrac > 1f - epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.NE));
                        if (xFrac > 1f - epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.E));
                        if (xFrac > 1f - epsilon && yFrac < epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.SE));
                        if (yFrac < epsilon) vision.AddExploredNode(World.GetAdjacentGroundNode(hitWorldCoordinates, Direction.S));

                        return vision;
                    }
                }

                // Hit air node
                if (objectHit.layer == World.Layer_AirNode)
                {
                    AirNode hitAirNode = World.GetAirNodeFromRaycastHit(hit);

                    if (hitAirNode == null) return vision; // Somehow we couldn't detect what air node we hit => just stop search

                    // Mark air node as visible
                    vision.AddVisibleNode(hitAirNode);

                    // Stop search as air nodes always block vision
                    return vision;
                }

                // Hit water node
                if (objectHit.layer == World.Layer_Water)
                {
                    // Get position of where we hit
                    Vector3 hitPosition = hit.point;
                    Vector2Int hitWorldCoordinates = World.GetWorldCoordinates(hitPosition);
                    WaterNode hitWaterNode = World.GetWaterNode(hitWorldCoordinates);

                    // Mark the water node as visible
                    vision.AddVisibleNode(hitWaterNode);

                    // Mark the ground node below the water node as visible
                    vision.AddVisibleNode(hitWaterNode.GroundNode);

                    // Stop search as water nodes always block vision
                    return vision;
                }

                // Hit entity (vision collider)
                if (objectHit.layer == World.Layer_EntityVisionCollider)
                {
                    Entity hitEntity = objectHit.transform.parent.GetComponentInChildren<Entity>();

                    // Mark entity as visible
                    vision.AddVisibleEntity(hitEntity);

                    // Mark all nodes that the entity stands on as explored
                    foreach (BlockmapNode n in hitEntity.OccupiedNodes) vision.AddExploredNode(n);

                    if (!hitEntity.BlocksVision) continue; // Continue search if entity doesn't block vision
                    else return vision; // End search if entity blocks vision
                }

                // Hit fence
                if (objectHit.layer == World.Layer_Fence)
                {
                    Fence hitFence = World.GetFenceFromRaycastHit(hit);

                    if (hitFence == null) return vision; // Somehow we couldn't detect what fence we hit => just stop search

                    // Mark node that fence is on as explored
                    vision.AddExploredNode(hitFence.Node);

                    if (!hitFence.Type.BlocksVision) continue; // Continue search if fence doesn't block vision
                    else return vision; // End search if fence blocks vision
                }

                // Hit wall
                if (objectHit.layer == World.Layer_Wall)
                {
                    Wall hitWall = World.GetWallFromRaycastHit(hit);

                    if (hitWall == null) // Somehow we couldn't detect what wall we hit => just stop search
                    {
                        return vision; 
                    }

                    // Mark wall as visible
                    vision.AddVisibleWall(hitWall);

                    if (!hitWall.BlocksVision) continue; // Continue search if wall doesn't block vision
                    else return vision; // End search if wall blocks vision
                }
            }

            // Nothing more to see
            return vision;
        }

        /// <summary>
        /// Returns the world position at which the "eyes" of this entity currently located.
        /// <br/> Rays for calculating vision are shot from this position.
        /// </summary>
        public Vector3 GetEyePosition()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("Eye position not yet implemented for entities bigger than 1x1");

            return OriginNode.CenterWorldPosition + new Vector3(0f, (Dimensions.y * World.TILE_HEIGHT) - (World.TILE_HEIGHT * 0.5f), 0f);
        }

        public virtual Sprite GetThumbnail()
        {
            return null;
            /*
            Texture2D previewThumbnail = AssetPreview.GetAssetPreview(gameObject);
            if (previewThumbnail != null)
                return Sprite.Create(previewThumbnail, new Rect(0.0f, 0.0f, previewThumbnail.width, previewThumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
            return null;
            */
        }

        public override string ToString()
        {
            return Name + " alt:" + MinAltitude + "-" + MaxAltitude + " " + Rotation.ToString();
        }

        #endregion

        #region Setters

        /// <summary>
        /// Instantly teleports this entity to the given node.
        /// </summary>
        public void Teleport(BlockmapNode targetNode)
        {
            BlockmapNode sourceNode = OriginNode;
            WorldPosition = GetWorldPosition(World, targetNode, Rotation);
            SetOriginNode(targetNode);

            // Update vision from entities around source and target position
            List<Entity> entitiesNearSourcePosition = World.GetNearbyEntities(sourceNode.CenterWorldPosition);
            List<Entity> entitiesNearTargetPosition = World.GetNearbyEntities(targetNode.CenterWorldPosition);
            List<Entity> entitiesToUpdate = entitiesNearSourcePosition.Union(entitiesNearTargetPosition).ToList();

            if (BlocksVision) World.UpdateVisionDelayed(entitiesToUpdate, callback: OnPostTeleportVisionCalcDone);
            else
            {
                // Only update the vision of itself and of entities from other actors
                entitiesToUpdate = entitiesToUpdate.Where(x => x.Owner != Owner).ToList(); 
                World.UpdateVisionDelayed(entitiesToUpdate, callback: OnPostTeleportVisionCalcDone);
                UpdateVision();
            }
        }
        /// <summary>
        /// Gets called when the vision of all entities is recalculated after this entity teleported.
        /// <br/>Updates its visibility and removes last known position of all actors that can't see it anymore.
        /// </summary>
        private void OnPostTeleportVisionCalcDone()
        {
            // Remove last know position for all players that don't see it anymore
            foreach(Actor actor in World.GetAllActors())
            {
                if (actor == Owner) continue;
                if (!IsVisibleBy(actor)) LastKnownPosition[actor] = null;
            }

            // Update visibility
            UpdateVisibility();
        }

        /// <summary>
        /// Sets the height of the entity. Does not affect anything immediately.
        /// </summary>
        public void SetHeight(int height)
        {
            Dimensions = new Vector3Int(Dimensions.x, height, Dimensions.z);
        }

        /// <summary>
        /// Changes the origin node and updates all relevant information with it.
        /// </summary>
        protected void SetOriginNode(BlockmapNode node)
        {
            pm_SetOriginNode.Begin();

            // Before setting new origin, update last known position for all players seeing this entity
            foreach (Actor p in World.GetAllActors())
                if (IsVisibleBy(p)) UpdateLastKnownPositionFor(p);

            // Set new origin
            OriginNode = node;
            UpdateOccupiedNodes();

            // Update position of vision collider
            UpdateVisionColliderPosition();

            pm_SetOriginNode.End();
        }

        /// <summary>
        /// Updates the position of all vision colliders according to the current OriginNode and rotation of this entity.
        /// </summary>
        protected virtual void UpdateVisionColliderPosition()
        {
            VisionCollider.transform.position = GetWorldPosition(World, OriginNode, Rotation);
            VisionCollider.transform.rotation = WorldRotation;
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

        #endregion

        #region Save / Load

        public static Entity Load(World world, EntityData data)
        {
            Entity instance = world.EntityLibrary.GetEntityInstance(world, data);
            instance.Init(data.Id, world, world.GetNode(data.OriginNodeId), data.Rotation, world.GetActor(data.PlayerId));
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
