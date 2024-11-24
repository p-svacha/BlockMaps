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
    public class Entity : WorldDatabaseObject, IVisionTarget, ISaveAndLoadable
    {
        public int id;
        public override int Id => id;

        /// <summary>
        /// The world in which this entity exists.
        /// </summary>
        public World World { get; protected set; }

        /// <summary>
        /// The blueprint that defines the looks, attributes and behaviour rules of this entity.
        /// </summary>
        public EntityDef Def;

        /// <summary>
        /// Components that add custom behaviour to this entity.
        /// </summary>
        public List<EntityComp> Components;

        /// <summary>
        /// Node that the southwest corner of this entity is on at this moment.
        /// </summary>
        public BlockmapNode OriginNode;

        /// <summary>
        /// The chunk that the origin of this entity is on.
        /// </summary>
        public Chunk Chunk => OriginNode.Chunk;

        /// <summary>
        /// What direction this entity is facing. [N/E/S/W]
        /// </summary>
        public Direction Rotation;

        /// <summary>
        /// For entities with a variable height, this value is used for height.
        /// </summary>
        protected int overrideHeight;

        /// <summary>
        /// Flag if this is the mirrored variant of the entity.
        /// </summary>
        public bool IsMirrored;

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
        public Actor Actor;

        // GameObjects
        /// <summary>
        /// Root GameObject of all GameObjects belonging to this entity.
        /// </summary>
        protected GameObject Wrapper;

        /// <summary>
        /// The GameObject that holds the MeshRenderer and MeshCollider for standalone entities.
        /// <br/>Null for entities that are not standalone.
        /// </summary>
        public GameObject MeshObject;

        /// <summary>
        /// The MeshRenderer that renders standalone entities.
        /// <br/>Null for entities that are not standalone.
        /// </summary>
        private MeshRenderer MeshRenderer;

        /// <summary>
        /// The collider that is used for hovering and selecting the entity with the cursor.
        /// <br/>Null for entities that are not standalone. Detection there is handled with custom logic in World.UpdateHoveredObjects.
        /// </summary>
        public MeshCollider MeshCollider { get; protected set; }

        /// <summary>
        /// The object holding the vision collider(s) that are used for the vision sytem.
        /// </summary>
        public GameObject VisionColliderObject { get; protected set; }

        /// <summary>
        /// Object that gets activated when this entity is selected.
        /// </summary>
        private Projector SelectionIndicator;

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

        public Entity() { }

        /// <summary>
        /// Gets called after this Entity got instantiated when spawned in the world.
        /// </summary>
        public void OnCreate(EntityDef def, int id, World world, BlockmapNode origin, int height, Direction rotation, Actor owner, bool isMirrored)
        {
            Def = def;

            if (!Def.VariableHeight && height != -1) throw new System.Exception($"Cannot create entity with def {def.DefName} with a custom height because that def doesn't support variable heights.");

            this.id = id;
            World = world;
            OriginNode = origin;
            overrideHeight = height;
            Rotation = rotation;
            Actor = owner;
            IsMirrored = isMirrored;

            OnCreated();
        }

        public override void PostLoad()
        {
            World.RegisterEntity(this, registerInWorld: false, updateWorld: false);

            OnPostLoad();
            Init();
        }

        /// <summary>
        /// Gets called after this Entity got instantiated, either through being spawned or when being loaded.
        /// </summary>
        public void Init()
        {
            // Validate
            if (Height <= 0) throw new System.Exception($"Cannot create an entity with height = {Height}. Must be positive.");

            // Initialize some objects
            OccupiedNodes = new HashSet<BlockmapNode>();
            CurrentVision = new VisionData();
            LastKnownPosition = new Dictionary<Actor, Vector3?>();
            foreach (Actor p in World.GetAllActors()) LastKnownPosition.Add(p, null);
            LastKnownRotation = new Dictionary<Actor, Quaternion?>();
            foreach (Actor p in World.GetAllActors()) LastKnownRotation.Add(p, null);

            // Set position and rotation
            ResetWorldPositonAndRotation();

            // Initialize components
            InitializeComps();

            // Initialize game objects
            InitializeGameObject();

            // Create a collider for entity vision on a seperate object
            CreateVisionCollider();

            // Move entity to spawn position (also registers it on all nodes)
            SetOriginNode(OriginNode);

            // Subclass hook
            OnInitialized();
        }

        /// <summary>
        /// Creates all components 
        /// </summary>
        private void InitializeComps()
        {
            Components = new List<EntityComp>();
            foreach (CompProperties compProps in Def.Components)
            {
                EntityComp newComp = newComp = (EntityComp)System.Activator.CreateInstance(compProps.CompClass);
                newComp.Entity = this;
                Components.Add(newComp);
                newComp.Initialize(compProps);

                try
                {
                    newComp.Validate();
                }
                catch(System.Exception e)
                {
                    throw new System.Exception($"The EntityComp {newComp.GetType()} is not valid on the entity {Label} (DefName={Def.DefName}).\nReason: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Creates the Unity GameObjects related to this entity.
        /// </summary>
        private void InitializeGameObject()
        {
            // Create a wrapper that acts as a container for all entity-related objects (mesh, mesh collider, vision collider)
            Wrapper = new GameObject(Label + "_wrapper");
            Wrapper.transform.SetParent(World.WorldObject.transform);

            // Create object that holds the mesh and mesh collider
            if(IsStandaloneEntity)
            {
                MeshObject = new GameObject(Label);
                MeshObject.layer = World.Layer_EntityMesh;
                MeshObject.transform.SetParent(Wrapper.transform);

                // Mesh
                if (Def.RenderProperties.RenderType == EntityRenderType.StandaloneModel)
                {
                    MeshFilter meshFilter = MeshObject.AddComponent<MeshFilter>();
                    meshFilter.mesh = Def.RenderProperties.Model.GetComponent<MeshFilter>().sharedMesh;
                    MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
                    MeshRenderer.sharedMaterials = Def.RenderProperties.Model.GetComponent<MeshRenderer>().sharedMaterials;
                    MeshCollider = MeshObject.AddComponent<MeshCollider>();

                    // Scale
                    MeshObject.transform.localScale = new Vector3(Def.RenderProperties.ModelScale, Def.RenderProperties.ModelScale, Def.RenderProperties.ModelScale);
                    if (IsMirrored) HelperFunctions.SetAsMirrored(MeshObject);
                }
                if(Def.RenderProperties.RenderType == EntityRenderType.StandaloneGenerated)
                {
                    MeshBuilder meshBuilder = new MeshBuilder(MeshObject);
                    Def.RenderProperties.StandaloneRenderFunction(meshBuilder, Height, IsMirrored, false);
                    meshBuilder.ApplyMesh();
                    MeshRenderer = MeshObject.GetComponent<MeshRenderer>();
                    MeshCollider = MeshObject.GetComponent<MeshCollider>();
                }

                // Player color
                if (Def.RenderProperties.PlayerColorMaterialIndex != -1)
                {
                    MeshRenderer.materials[Def.RenderProperties.PlayerColorMaterialIndex].color = Actor.Color;
                    MeshRenderer.materials[Def.RenderProperties.PlayerColorMaterialIndex].SetColor("_TintColor", Actor.Color);
                }

                // Draw mode
                ShowTextures(World.IsShowingTextures);

                // Reference entity in its collider
                WorldObjectCollider ec = MeshObject.AddComponent<WorldObjectCollider>();
                ec.Object = this;

                // Selection indicator
                SelectionIndicator = GameObject.Instantiate(Resources.Load<Projector>("BlockmapFramework/Prefabs/SelectionIndicator"));
                SelectionIndicator.transform.SetParent(MeshObject != null ? MeshObject.transform : Wrapper.transform);
                SelectionIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                SelectionIndicator.orthographicSize = 0.5f;
                SetSelected(false);
            }
        }

        /// <summary>
        /// Creates the box collider(s) for this entity that are used to calculate the vision of other entities around it and adds them to the wrapper.
        /// </summary>
        protected void CreateVisionCollider()
        {
            VisionColliderObject = new GameObject("visionCollider");
            VisionColliderObject.layer = World.Layer_EntityVisionCollider;
            VisionColliderObject.transform.SetParent(Wrapper.transform);
            if (MeshObject != null) VisionColliderObject.transform.localScale = MeshObject.transform.localScale;

            if (Def.VisionImpact.VisionColliderType == VisionColliderType.FullBox) // Create a single box collider with the bounds of the whole entity
            {
                BoxCollider collider = VisionColliderObject.AddComponent<BoxCollider>();
                if (MeshObject != null) collider.size = new Vector3(Dimensions.x / MeshObject.transform.localScale.x, (Dimensions.y * World.NodeHeight) / MeshObject.transform.localScale.y, Dimensions.z / MeshObject.transform.localScale.z);
                else collider.size = new Vector3(Dimensions.x, (Dimensions.y * World.NodeHeight), Dimensions.z);
                collider.center = new Vector3(0f, collider.size.y / 2, 0f);

                WorldObjectCollider evc = VisionColliderObject.AddComponent<WorldObjectCollider>();
                evc.Object = this;
            }
            else if (Def.VisionImpact.VisionColliderType == VisionColliderType.MeshCollider) // Create a vision collider that is the same as the entitys mesh collider
            {
                MeshCollider collider = VisionColliderObject.AddComponent<MeshCollider>();
                collider.sharedMesh = MeshCollider.sharedMesh;

                WorldObjectCollider evc = VisionColliderObject.AddComponent<WorldObjectCollider>();
                evc.Object = this;
            }
            else if(Def.VisionImpact.VisionColliderType == VisionColliderType.BlockPerNode) // Create a box collider per node, each one with its own height
            {
                for (int x = 0; x < Dimensions.x; x++)
                {
                    for (int y = 0; y < Dimensions.z; y++)
                    {
                        Vector2Int localCoords = new Vector2Int(x, y);

                        GameObject perNodeColliderObject = new GameObject("visionCollider_" + x + "_" + y);
                        perNodeColliderObject.layer = World.Layer_EntityVisionCollider;
                        perNodeColliderObject.transform.SetParent(VisionColliderObject.transform);
                        BoxCollider collider = perNodeColliderObject.AddComponent<BoxCollider>();

                        float height = Dimensions.y; // default height
                        if (Def.VisionImpact.VisionBlockHeights.TryGetValue(localCoords, out int overwrittenHeight)) height = overwrittenHeight; // overwritten height

                        if (MeshObject != null) collider.size = new Vector3(1f / MeshObject.transform.localScale.x, (height * World.NodeHeight) / MeshObject.transform.localScale.y, 1f / MeshObject.transform.localScale.z);
                        collider.center = new Vector3((Dimensions.x / 2f) - x - 0.5f, collider.size.y / 2, (Dimensions.z / 2f) - y - 0.5f);
                        if (IsMirrored) collider.center = new Vector3(collider.center.x * -1f, collider.center.y, collider.center.z);

                        WorldObjectCollider evc = perNodeColliderObject.AddComponent<WorldObjectCollider>();
                        evc.Object = this;
                    }
                }
            }
        }

        public void DestroyGameObject()
        {
            GameObject.Destroy(Wrapper);
        }

        #endregion

        #region Hooks

        /// <summary>
        /// Gets called right after the entity has been created and before it has been initialized.
        /// </summary>
        protected virtual void OnCreated() { }

        /// <summary>
        /// Gets called when loading a world after all values have been loaded from the save file and before initialization of this entity.
        /// </summary>
        protected virtual void OnPostLoad() { }

        /// <summary>
        /// Gets called when an entity gets registered in the world. Useful if subtypes need additional registering in specific places.
        /// </summary>
        public virtual void OnRegister() { }

        /// <summary>
        /// Gets called when an entity gets de-registered from the world. Useful if subtypes need additional de-registering in specific places.
        /// </summary>
        public virtual void OnDeregister() { }

        /// <summary>
        /// Gets called when main intialization is done so subtype-specific initialization steps can be performed.
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// Gets called every tick.
        /// </summary>
        protected virtual void OnTick() { }

        #endregion

        #region Update

        /// <summary>
        /// Gets called every tick.
        /// </summary>
        public void Tick()
        {
            foreach (EntityComp comp in Components) comp.Tick();

            OnTick();
        }

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
            foreach (BlockmapNode n in CurrentVision.ExploredNodes) n.AddExploredBy(Actor);

            // Update last known position and rotation of all currently visible entities
            //Debug.Log(CurrentVision.VisibleEntities.Count + " entities are visible");
            foreach (Entity e in CurrentVision.VisibleEntities) e.AddVisionBy(this);

            // Add entity vision to visible walls
            foreach (Wall w in CurrentVision.VisibleWalls) w.AddVisionBy(this);

            // Set walls as explored
            foreach (Wall w in CurrentVision.ExploredWalls) w.AddExploredBy(Actor);

            // Find nodes and walls where the visibility changed
            HashSet<BlockmapNode> changedVisibilityNodes = new HashSet<BlockmapNode>(previousVision.VisibleNodes.Concat(previousVision.ExploredNodes));
            HashSet<Wall> changedVisibilityWalls = new HashSet<Wall>(previousVision.VisibleWalls.Concat(previousVision.ExploredWalls));
            changedVisibilityNodes.SymmetricExceptWith(CurrentVision.VisibleNodes.Concat(CurrentVision.ExploredNodes));
            changedVisibilityWalls.SymmetricExceptWith(CurrentVision.VisibleWalls.Concat(CurrentVision.ExploredWalls));
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
            foreach (Wall w in changedVisibilityWalls) changedVisibilityChunks.Add(w.Chunk);

            // Redraw visibility of affected chunks
            //Debug.Log("Visibility of " + changedVisibilityChunks.Count + " chunks changed.");
            foreach (Chunk c in changedVisibilityChunks) World.OnVisibilityChanged(c, Actor);

            pm_HandleVisibilityChange.End();

            pm_UpdateVision.End();
        }


        #endregion

        #region Vision Target

        /// <summary>
        /// List containing all entities that currently see this entity.
        /// </summary>
        public HashSet<Entity> SeenBy = new HashSet<Entity>();

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
            UpdateLastKnownPositionFor(e.Actor);
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
            if (Actor == actor) return true; // This entity belongs to the given actor
            if (OccupiedNodes.Any(n => n.Zones.Any(z => z.ProvidesVision && z.Actor == actor))) return true; // Entity is inside a zone of actor that provides vision
            if (SeenBy.FirstOrDefault(x => x.Actor == actor) != null) return true; // Entity is seen by an entity of given actor

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
            if (Def.RenderProperties.RenderType == EntityRenderType.NoRender) return;
            if (Def.RenderProperties.RenderType == EntityRenderType.Batch) return; // Visibility of batch entities is handled through chunk mesh shader

            if (IsStandaloneEntity)
            {
                // Entity is currently visible => render normally at current position
                if (IsVisibleBy(player))
                {
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_TintColor", Color.clear);

                    MeshObject.transform.position = WorldPosition;
                    MeshObject.transform.rotation = WorldRotation;

                    foreach (Material m in MeshRenderer.materials) m.SetFloat("_Transparency", 0);
                }

                // Entity was explored before but not currently visible => render transparent at last known position
                else if (IsExploredBy(player))
                {
                    MeshRenderer.enabled = true;
                    MeshRenderer.material.SetColor("_TintColor", new Color(0f, 0f, 0f, 0.5f));

                    MeshObject.transform.position = LastKnownPosition[player].Value;
                    MeshObject.transform.rotation = LastKnownRotation[player].Value;

                    foreach (Material m in MeshRenderer.materials) m.SetFloat("_Transparency", 0.5f);
                }

                // Entity was not yet explored => don't render it
                else MeshRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Sets the draw mode of this entity to textured or flat mode. Only works for standalone entites, else it's handled through the batch mesh.
        /// </summary>
        public void ShowTextures(bool show)
        {
            if (!IsStandaloneEntity) throw new System.Exception($"Cannot set draw mode of an entity that's not drawn as a standalone object. ({Label} / DefName = {Def.DefName})");

            for (int i = 0; i < MeshRenderer.materials.Length; i++)
                MeshRenderer.materials[i].SetFloat("_UseTextures", show ? 1 : 0);
        }

        #endregion

        #region Getters

        public virtual string Label => Def.Label;
        public virtual string LabelCap => Label.CapitalizeFirst();
        public virtual string Description => Def.Description;

        public virtual float VisionRange => Def.VisionRange;
        public virtual bool BlocksVision => Def.VisionImpact.BlocksVision;
        public virtual bool RequiresFlatTerrain => Def.RequiresFlatTerrain;
        public virtual Vector3Int Dimensions => Def.VariableHeight ? new Vector3Int(Def.Dimensions.x, overrideHeight, Def.Dimensions.z) : Def.Dimensions;

        public int MinAltitude => Mathf.FloorToInt(GetWorldPosition(OriginNode, Rotation, IsMirrored).y / World.NodeHeight); // Rounded down to y-position of its center
        public int MaxAltitude => Mathf.CeilToInt((GetWorldPosition(OriginNode, Rotation, IsMirrored).y / World.NodeHeight) + (Height - 1)); // Rounded up to y-position of its center + height
        public int Height => Dimensions.y;
        public float WorldHeight => World.GetWorldHeight(Height);
        public Vector3 WorldSize => Vector3.Scale(MeshObject.GetComponent<MeshFilter>().mesh.bounds.size, MeshObject.transform.localScale);
        public bool CanSee => VisionRange > 0;

        /// <summary>
        /// Returns the dimensions of this object taking into account its current rotation.
        /// </summary>
        public Vector3Int GetTranslatedDimensions()
        {
            return EntityManager.TranslatedDimensions(Def, Rotation, overrideHeight);
        }

        /// <summary>
        /// Retrieve a specific comp of this entity (or null if it doesn't have it).
        /// </summary>
        public T GetComponent<T>() where T : EntityComp
        {
            if (Components != null)
            {
                int i = 0;
                for (int count = Components.Count; i < count; i++)
                {
                    if (Components[i] is T result)
                    {
                        return result;
                    }
                }
            }
            throw new System.Exception($"Component {typeof(T)} not found on Entity {Label}. Entity has {Components.Count} components.");
        }

        /// <summary>
        /// Returns the world position of this entity when placed on the given originNode.
        /// <br/>By default this is in the center of the entity in the x and z axis and on the bottom in the y axis.
        /// </summary>
        public virtual Vector3 GetWorldPosition(BlockmapNode originNode, Direction rotation, bool isMirrored) => Def.RenderProperties.GetWorldPositionFunction(Def, World, originNode, rotation, isMirrored);

        /// <summary>
        /// Returns the world position of the center of this entity.
        /// </summary>
        public virtual Vector3 GetWorldCenter() => GetWorldPosition(OriginNode, Rotation, IsMirrored) + new Vector3(0f, WorldHeight / 2f, 0f);

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

        /// <summary>
        /// Returns all nodes this entity is currently occupying (as in standing on). 
        /// </summary>
        public HashSet<BlockmapNode> GetOccupiedNodes()
        {
            return EntityManager.GetOccupiedNodes(Def, World, OriginNode, Rotation, overrideHeight);
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
                            if (e.Actor == Actor) continue; // Don't check for entities of the same actor since they see them anyway
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
                    (1 << World.Layer_Fence) | (1 << World.Layer_WallVisionCollider);
            RaycastHit[] hits = Physics.RaycastAll(ray, VisionRange, layerMask);
            System.Array.Sort(hits, (a, b) => (a.distance.CompareTo(b.distance))); // sort hits by distance

            foreach (RaycastHit hit in hits)
            {
                GameObject objectHit = hit.transform.gameObject;

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

                        if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
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

                        if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
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
                    if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
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
                    if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
                    return vision;
                }

                // Hit entity (vision collider)
                if (objectHit.layer == World.Layer_EntityVisionCollider)
                {
                    // Get the entity we hit from the collider
                    Entity hitEntity = (Entity)objectHit.GetComponent<WorldObjectCollider>().Object;

                    // If the thing we hit is ourselves, go to the next thing
                    if (hitEntity == this) continue;

                    // Mark entity as visible
                    vision.AddVisibleEntity(hitEntity);

                    // Mark all nodes that the entity stands on as explored
                    foreach (BlockmapNode n in hitEntity.OccupiedNodes) vision.AddExploredNode(n);

                    if (!hitEntity.BlocksVision) continue; // Continue search if entity doesn't block vision
                    else
                    {
                        if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
                        return vision; // End search if entity blocks vision
                    }
                }

                // Hit fence
                if (objectHit.layer == World.Layer_Fence)
                {
                    Fence hitFence = World.GetFenceFromRaycastHit(hit);

                    if (hitFence == null) return vision; // Somehow we couldn't detect what fence we hit => just stop search

                    // Mark node that fence is on as explored
                    vision.AddExploredNode(hitFence.Node);

                    if (!hitFence.BlocksVision) continue; // Continue search if fence doesn't block vision
                    else
                    {
                        if(debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
                        return vision; // End search if fence blocks vision
                    }
                }

                // Hit wall
                if (objectHit.layer == World.Layer_WallVisionCollider)
                {
                    Wall hitWall = (Wall)objectHit.GetComponent<WorldObjectCollider>().Object;

                    // Mark wall as visible
                    vision.AddVisibleWall(hitWall);

                    if (!hitWall.BlocksVision) continue; // Continue search if wall doesn't block vision
                    else
                    {
                        if (debugVisionRay) ShowDebugBlockedVisionRay(sourcePosition, targetPosition, hit.point);
                        return vision; // End search if wall blocks vision
                    }
                }
            }

            // Nothing more to see
            if (debugVisionRay) ShowDebugUnblockedVisionRay(sourcePosition, targetPosition, vision.ExploredNodes.Count > 0 || vision.ExploredWalls.Count > 0 ? Color.green : Color.blue);
            return vision;
        }

        private void ShowDebugBlockedVisionRay(Vector3 source, Vector3 rayTarget, Vector3 blockedHitPoint)
        {
            float debugDrawDuration = 60f; // seconds

            Color segment1Color = Color.yellow;
            Vector3 segment1Source = source;
            Vector3 segment1Target = blockedHitPoint;
            Debug.DrawRay(segment1Source, segment1Target - segment1Source, segment1Color, debugDrawDuration);

            Color segment2Color = Color.red;
            Vector3 segment2Source = blockedHitPoint;
            Vector3 segment2Target = rayTarget;
            Debug.DrawRay(segment2Source, segment2Target - segment2Source, segment2Color, debugDrawDuration);
        }
        private void ShowDebugUnblockedVisionRay(Vector3 source, Vector3 rayTarget, Color c)
        {
            float debugDrawDuration = 60f; // seconds
            Debug.DrawRay(source, rayTarget - source, c, debugDrawDuration);

        }

        /// <summary>
        /// Returns the world position at which the "eyes" of this entity currently located.
        /// <br/> Rays for calculating vision are shot from this position.
        /// </summary>
        private Vector3 GetEyePosition()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("Eye position not yet implemented for entities bigger than 1x1");

            return OriginNode.CenterWorldPosition + new Vector3(0f, (Dimensions.y * World.NodeHeight) - (World.NodeHeight * 0.5f), 0f);
        }

        /// <summary>
        /// Returns if this entity is drawn as its own object with its own MeshObject.
        /// </summary>
        public bool IsStandaloneEntity => Def.RenderProperties.RenderType == EntityRenderType.StandaloneModel || Def.RenderProperties.RenderType == EntityRenderType.StandaloneGenerated;

        public override string ToString()
        {
            return $"{Label} ({Def.DefName}) alt: {MinAltitude} - {MaxAltitude} {Rotation}";
        }

        #endregion

        #region Setters

        /// <summary>
        /// Shows/hides the selection indicator of this entity.
        /// </summary>
        public void SetSelected(bool value)
        {
            SelectionIndicator.gameObject.SetActive(value);
        }

        /// <summary>
        /// Instantly teleports this entity to the given node.
        /// </summary>
        public void Teleport(BlockmapNode targetNode)
        {
            BlockmapNode sourceNode = OriginNode;
            SetOriginNode(targetNode);
            ResetWorldPositonAndRotation();

            // Update vision from entities around source and target position
            List<Entity> entitiesNearSourcePosition = World.GetNearbyEntities(sourceNode.CenterWorldPosition);
            List<Entity> entitiesNearTargetPosition = World.GetNearbyEntities(targetNode.CenterWorldPosition);
            List<Entity> entitiesToUpdate = entitiesNearSourcePosition.Union(entitiesNearTargetPosition).ToList();

            if (BlocksVision) World.UpdateEntityVisionDelayed(entitiesToUpdate, callback: OnPostTeleportVisionCalcDone);
            else
            {
                // Only update the vision of itself and of entities from other actors
                entitiesToUpdate = entitiesToUpdate.Where(x => x.Actor != Actor).ToList(); 
                World.UpdateEntityVisionDelayed(entitiesToUpdate, callback: OnPostTeleportVisionCalcDone);
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
                if (actor == Actor) continue;
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
            if (!Def.VariableHeight) throw new System.Exception($"Cannot set height of an entity without variable height (defName = {Def.DefName}");
            this.overrideHeight = height;
        }

        /// <summary>
        /// Changes the origin node and updates all relevant information with it.
        /// </summary>
        public void SetOriginNode(BlockmapNode node)
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
        protected void UpdateVisionColliderPosition()
        {
            VisionColliderObject.transform.position = GetWorldPosition(OriginNode, Rotation, IsMirrored);
            VisionColliderObject.transform.rotation = WorldRotation;
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

        /// <summary>
        /// Sets the WorldPosition and WorldRotation to the default position according to the current OriginNode and Rotation.
        /// </summary>
        public virtual void ResetWorldPositonAndRotation()
        {
            WorldPosition = GetWorldPosition(OriginNode, Rotation, IsMirrored);
            WorldRotation = HelperFunctions.Get2dRotationByDirection(Rotation);
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

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadDef(ref Def, "def");
            SaveLoadManager.SaveOrLoadReference(ref OriginNode, "originNode");
            SaveLoadManager.SaveOrLoadPrimitive(ref overrideHeight, "overrideHeight");
            SaveLoadManager.SaveOrLoadPrimitive(ref IsMirrored, "isMirrored");
            SaveLoadManager.SaveOrLoadPrimitive(ref Rotation, "rotation");
            SaveLoadManager.SaveOrLoadReference(ref Actor, "owner");
        }

        #endregion

    }
}
