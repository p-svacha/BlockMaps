using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents a specific location on the worldmap, where entities can be located on.
    /// <br/> A BlockmapNode is on one specific world coordinate but can have different heights for its corners.
    /// <br/> BlockmapNodes can have a rendered representation in the world but don't have to.
    /// <br/> Used for pathfinding.
    /// </summary>
    public abstract class BlockmapNode : IVisionTarget, ISaveAndLoadable
    {
        /// <summary>
        /// Unique identifier of the node.
        /// </summary>
        private int id;
        public int Id => id;

        /// <summary>
        /// The surface that defines many gameplay behaviours of this node.
        /// </summary>
        public SurfaceDef SurfaceDef;

        /// <summary>
        /// Altitude of the 4 corners of the node: {SW, SE, NE, NW}
        /// </summary>
        public Dictionary<Direction, int> Altitude;

        /// <summary>
        /// Lowest altitude of this node.
        /// </summary>
        public int BaseAltitude { get; private set; }
        public float BaseWorldHeight => BaseAltitude * World.NodeHeight;

        /// <summary>
        /// Highest altitude of this node.
        /// </summary>
        public int MaxAltitude { get; private set; }

        /// <summary>
        /// Shape is saved in a string with 4 chars, where each char is a corner (SW, SE, NE, NW) storing the height above the min height of the node.
        /// <br/> For example "1001" is a west-facing up-slope.
        /// </summary>
        public string Shape { get; protected set; }

        /// <summary>
        /// Shapes with the format "1010" or "0101" have two possible variants (center high or center low). This flag decides which variant is used in that case.
        /// </summary>
        public bool LastHeightChangeWasIncrease;

        // Node attributes
        public World World { get; private set; }
        public Chunk Chunk { get; private set; }
        public Vector2Int WorldCoordinates;
        public Vector2 WorldCenter2D => WorldCoordinates + new Vector2(0.5f, 0.5f);
        public Vector2Int LocalCoordinates;
        public abstract NodeType Type { get; }

        /// <summary>
        /// Flag if entities can be placed on top of nodes of this type.
        /// </summary>
        public abstract bool SupportsEntities { get; }

        // Connections
        public List<Transition> Transitions { get; private set; }
        /// <summary>
        /// All transition going out from this node to other nodes grouped by target node.
        /// </summary>
        public Dictionary<BlockmapNode, List<Transition>> TransitionsByTarget { get; private set; }
        /// <summary>
        /// All transitions going out from this node to adjacent nodes (in all 8 directions) that can be reached with normal walking in that direction.
        /// </summary>
        public Dictionary<Direction, Transition> WalkTransitions { get; private set; }

        /// <summary>
        /// Dictionary containing the values of what the maximum height for an entity is so it can still pass through this node in the given direction.
        /// <br/> Also includes Direction.None for the maximum height of something just standing on this node. This value is not affected by walls, fences, etc.
        /// <br/>Should be recalculated whenever something near the nodes changes.
        /// </summary>
        public Dictionary<Direction, int> MaxPassableHeight { get; private set; }

        /// <summary>
        /// Dictionary containing the amount of free head space on each direction. Free head space is calculated by checking the first node or wall (only on sides/corners) above this node that blocks the way.
        /// <br/> Climbables directly on this node are ignored for this value.
        /// </summary>
        public Dictionary<Direction, int> FreeHeadSpace { get; private set; }

        // Things on this node
        public HashSet<Entity> Entities = new HashSet<Entity>();
        public Dictionary<Direction, Fence> Fences = new Dictionary<Direction, Fence>();
        public List<Zone> Zones = new List<Zone>();

        /// <summary>
        /// Ladders that come from this node
        /// </summary>
        public Dictionary<Direction, Ladder> SourceLadders = new Dictionary<Direction, Ladder>();
        /// <summary>
        /// Ladders that lead to this node
        /// </summary>
        public Dictionary<Direction, Ladder> TargetLadders = new Dictionary<Direction, Ladder>();

        /// <summary>
        /// Doors on this node.
        /// </summary>
        public Dictionary<Direction, Door> Doors = new Dictionary<Direction, Door>();

        /// <summary>
        /// The mesh in the world that this node is drawn on.
        /// </summary>
        protected ChunkMesh Mesh { get; private set; }

        // Cache
        private Dictionary<Direction, List<IClimbable>> ClimbUpCache = new Dictionary<Direction, List<IClimbable>>();


        #region Initialize

        /// <summary>
        /// Default constructor used for loading through reflection.
        /// </summary>
        protected BlockmapNode() { }

        /// <summary>
        /// Constructor used when creating new node at runtime.
        /// </summary>
        protected BlockmapNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceDef surfaceDef)
        {
            World = world;
            Chunk = chunk;
            this.id = id;
            LocalCoordinates = new Vector2Int(localCoordinates.x, localCoordinates.y);
            WorldCoordinates = chunk.GetWorldCoordinates(LocalCoordinates);
            
            Altitude = height;
            SurfaceDef = surfaceDef;

            Init();
        }

        /// <summary>
        /// Gets called when loading a world after all values have been loaded from the save file.
        /// </summary>
        public void PostLoad()
        {
            Chunk = World.GetChunk(WorldCoordinates);
            World.RegisterNode(this, registerInWorld: false);

            Init();
        }

        /// <summary>
        /// Gets called after this Entity got instantiated, either through being spawned or when being loaded.
        /// </summary>
        public void Init()
        {
            RecalculateShape();
            Transitions = new List<Transition>();
            TransitionsByTarget = new Dictionary<BlockmapNode, List<Transition>>();
            WalkTransitions = new Dictionary<Direction, Transition>();

            FreeHeadSpace = new Dictionary<Direction, int>();
            foreach (Direction dir in HelperFunctions.GetAllDirections9()) FreeHeadSpace.Add(dir, 0);
            MaxPassableHeight = new Dictionary<Direction, int>();
            foreach (Direction dir in HelperFunctions.GetAllDirections9()) MaxPassableHeight.Add(dir, 0);

            RecalculateCenterWorldPosition();
        }

        /// <summary>
        /// Calculates the base height, relative heights and shape according th this nodes heights.
        /// </summary>
        protected void RecalculateShape()
        {
            BaseAltitude = Altitude.Values.Min();
            MaxAltitude = Altitude.Values.Max();
            Shape = GetShape(Altitude);
        }

        protected string GetShape(Dictionary<Direction, int> height)
        {
            List<int> distinctHeights = height.Values.Distinct().OrderBy(x => x).ToList();
            int baseHeight = height.Values.Min();
            string binaryShape = "";
            foreach (Direction dir in HelperFunctions.GetCorners()) binaryShape += distinctHeights.IndexOf(height[dir]);
            return binaryShape;
        }

        #endregion

        #region Navmesh Transitions
        //
        // ************* TRANSITIONS *************
        // 
        // When the navmesh of an area gets updated, a specific order needs to be followed.
        // Each step needs to be executed for ALL affected nodes before the next step can be executed for ALL affected nodes:
        // 1. RecalcuatePassability()
        // 2. ResetTransitions()
        // 3. SetStraightAdjacentTransitions()
        // 4. SetDiagonalAdjacentTransitions()
        // 5. SetCliffClimbTransitions()

        
        /// <summary>
        /// Returns the cheapest transition for the given entity to get from this node to the specified target node.
        /// <br/>Returns null if no transition to the target node exists.
        /// </summary>
        public Transition GetCheapestTransition(Entity entity, BlockmapNode target)
        {
            Transition cheapestTransition = null;
            float cheapestCost = float.MaxValue;
            foreach(Transition t in TransitionsByTarget[target])
            {
                float cost = t.GetMovementCost(entity);
                if(cost < cheapestCost)
                {
                    cheapestCost = cost;
                    cheapestTransition = t;
                }
            }
            return cheapestTransition;
        }

        /// <summary>
        /// Clears all transitions of this node.
        /// </summary>
        public void ResetTransitions()
        {
            Transitions.Clear();
            TransitionsByTarget.Clear();
            WalkTransitions.Clear();
            ClimbUpCache.Clear();
        }

        /// <summary>
        /// All transitions should be added through this function.
        /// </summary>
        private void AddTransition(Transition t, bool isWalkTransition)
        {
            Transitions.Add(t);
            if (TransitionsByTarget.ContainsKey(t.To)) TransitionsByTarget[t.To].Add(t);
            else TransitionsByTarget.Add(t.To, new List<Transition>() { t });

            if (isWalkTransition) WalkTransitions.Add(t.Direction, t);
        }

        /// <summary>
        /// Recalculates the maximum height an entity is allowed to have so it can still pass through this node for all directions.
        /// </summary>
        public void RecalcuatePassability()
        {
            foreach (Direction corner in HelperFunctions.GetCorners()) MaxPassableHeight[corner] = -1;

            // Set max possible to 0 for all sides if not generally passable
            if (!IsGenerallyPassable())
            {
                foreach (Direction dir in HelperFunctions.GetAllDirections9()) MaxPassableHeight[dir] = 0;
                return;
            }

            // Get free head space for all directions
            RecalculateFreeHeadSpace();

            // Assign general/center headspace first (ignores all walls/fences/etc)
            MaxPassableHeight[Direction.None] = FreeHeadSpace[Direction.None];

            // Check sides first (since they can also block corners in some cases)
            foreach (Direction side in HelperFunctions.GetSides())
            {
                // Check if the side is blocked by a climbable. If yes, set maxheight for this side and its corners to 0
                if (IsSideBlocked(side))
                {
                    MaxPassableHeight[side] = 0;
                    foreach (Direction corner in HelperFunctions.GetAffectedCorners(side))
                    {
                        MaxPassableHeight[corner] = 0;
                    }
                    continue;
                }

                // If side is not blocked, calculate the free head space on this side.
                MaxPassableHeight[side] = FreeHeadSpace[side];
            }

            // Check corners
            foreach (Direction corner in HelperFunctions.GetCorners())
            {
                // Check if the corner has already been set by the side
                if (MaxPassableHeight[corner] != -1) continue;

                // Check if there is something right on the corner blocking it
                if (IsCornerBlocked(corner))
                {
                    MaxPassableHeight[corner] = 0;
                    continue;
                }

                // If corner is not blocked, calculate the free headspace for all relevant sides and corners and take the minimum out of all of them.
                // This includes the corner itself and the two affected sides on the same node.
                int selfCornerHeadspace = FreeHeadSpace[corner];
                int prevSideHeadspace = MaxPassableHeight[HelperFunctions.GetPreviousDirection8(corner)];
                int nextSideHeadspace = MaxPassableHeight[HelperFunctions.GetNextDirection8(corner)];

                MaxPassableHeight[corner] = Mathf.Min(selfCornerHeadspace, prevSideHeadspace, nextSideHeadspace);
            }
        }

        /// <summary>
        /// Recalculates the free head space for all directions. Free headspace refers to the amount of cells that are unblocked by nodes/walls ABOVE this node.
        /// </summary>
        private void RecalculateFreeHeadSpace()
        {
            foreach (Direction dir in HelperFunctions.GetAllDirections9()) FreeHeadSpace[dir] = World.MAX_ALTITUDE;

            List<BlockmapNode> nodesAbove = World.GetNodes(WorldCoordinates, BaseAltitude, World.MAX_ALTITUDE);
            List<Wall> wallsOnCoordinate = World.GetWalls(WorldCoordinates);

            // 1. Check corners from nodes above (these block corners, side and center)
            foreach (BlockmapNode node in nodesAbove)
            {
                if (node == this) continue;
                if (!node.SupportsEntities) continue;
                if (IsAbove(node)) continue;

                foreach (Direction corner in HelperFunctions.GetCorners())
                {
                    int diff = node.Altitude[corner] - Altitude[corner];
                    FreeHeadSpace[corner] = diff;

                    Direction prevDir = HelperFunctions.GetPreviousDirection8(corner);
                    Direction nextDir = HelperFunctions.GetNextDirection8(corner);
                    if (diff < FreeHeadSpace[prevDir]) FreeHeadSpace[prevDir] = diff;
                    if (diff < FreeHeadSpace[nextDir]) FreeHeadSpace[nextDir] = diff;
                }
            }

            // 1b. Center headspace is only affected by nodes above
            FreeHeadSpace[Direction.None] = Mathf.Min(FreeHeadSpace[Direction.NW], FreeHeadSpace[Direction.NE], FreeHeadSpace[Direction.SW], FreeHeadSpace[Direction.SE]);

            // 2. Check side walls above (these block sides and corners)
            foreach (Direction side in HelperFunctions.GetSides())
            {
                int sideMinAltitude = GetMinAltitude(side);
                int sideMaxAltitude = GetMaxAltitude(side);
                foreach (Wall wall in wallsOnCoordinate)
                {
                    if (wall.Side != side) continue; // Wall is not on this side
                    if (wall.MaxAltitude < sideMinAltitude) continue; // Wall is below node

                    int diff = wall.MaxAltitude - sideMaxAltitude;

                    if (diff < FreeHeadSpace[side]) FreeHeadSpace[side] = diff;

                    // Also blocks corners
                    Direction prevDir = HelperFunctions.GetPreviousDirection8(side);
                    if (diff < FreeHeadSpace[prevDir]) FreeHeadSpace[prevDir] = diff;

                    Direction nextDir = HelperFunctions.GetNextDirection8(side);
                    if (diff < FreeHeadSpace[nextDir]) FreeHeadSpace[nextDir] = diff;
                }
            }

            // 3. Check corner walls (these only block corners)
            foreach (Direction corner in HelperFunctions.GetCorners())
            {
                int nodeAltitde = Altitude[corner];
                foreach (Wall wall in wallsOnCoordinate)
                {
                    if (wall.Side != corner) continue; // Wall is not on this corner
                    if (wall.MaxAltitude < nodeAltitde) continue; // Wall is below node

                    int diff = wall.MaxAltitude - nodeAltitde;

                    if (diff < FreeHeadSpace[corner]) FreeHeadSpace[corner] = diff;
                }
            }
        }

        /// <summary>
        /// Updates the straight neighbours by applying the general rule:
        /// If there is an adjacent passable node in the direction with matching heights, connect it as a neighbour.
        /// </summary>
        public void SetStraightAdjacentTransitions()
        {
            if (!IsPassable()) return;
            SetStraightAdjacentTransition(Direction.N);
            SetStraightAdjacentTransition(Direction.E);
            SetStraightAdjacentTransition(Direction.S);
            SetStraightAdjacentTransition(Direction.W);
        }
        /// <summary>
        /// Sets transitions to nodes that are adjacent in the directions N,E,S,W by checking if they connect seamlessly.
        /// </summary>
        private void SetStraightAdjacentTransition(Direction dir)
        {
            if (!IsPassable(dir)) return;

            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (!adjNode.IsPassable()) continue;
                if (!adjNode.IsPassable(oppositeDir)) continue;

                if (ShouldConnectToNodeDirectly(adjNode, dir)) // Connect to node directly
                {
                    int maxHeight = Mathf.Min(FreeHeadSpace[dir], adjNode.FreeHeadSpace[oppositeDir]);

                    AdjacentWalkTransition t = new AdjacentWalkTransition(this, adjNode, dir, maxHeight);
                    AddTransition(t, isWalkTransition: true);
                }
            }
        }
        /// <summary>
        /// Returns if this node should be directly connected to the given node (that is adjacent in the given direction)
        /// </summary>
        protected virtual bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            if (!World.DoAdjacentHeightsMatch(this, adjNode, dir)) return false;
            return true;
        }

        /// <summary>
        /// Updates diagonal neighbours by applying the genereal rule:
        /// If the path N>E results in the same node as E>N, then connect NE to that node
        /// </summary>
        public void SetDiagonalAdjacentTransitions()
        {
            if (!IsPassable()) return;
            SetDiagonalAdjacentTransition(Direction.NE);
            SetDiagonalAdjacentTransition(Direction.NW);
            SetDiagonalAdjacentTransition(Direction.SE);
            SetDiagonalAdjacentTransition(Direction.SW);
        }
        private void SetDiagonalAdjacentTransition(Direction dir)
        {
            if (!IsPassable(dir)) return;

            Direction preDirection = HelperFunctions.GetPreviousDirection8(dir);
            Direction postDirection = HelperFunctions.GetNextDirection8(dir);
            BlockmapNode sideConnectedNodePre = WalkTransitions.ContainsKey(preDirection) ? WalkTransitions[preDirection].To : null;
            BlockmapNode sideConnectedNodePost = WalkTransitions.ContainsKey(postDirection) ? WalkTransitions[postDirection].To : null;

            if (sideConnectedNodePre == null) return;
            if (sideConnectedNodePost == null) return;
            Direction sideConnectedNodePreDir = HelperFunctions.GetMirroredCorner(dir, preDirection);
            Direction sideConnectedNodeostDir = HelperFunctions.GetMirroredCorner(dir, postDirection);
            if (!sideConnectedNodePre.IsPassable(sideConnectedNodePreDir)) return;
            if (!sideConnectedNodePost.IsPassable(sideConnectedNodeostDir)) return;

            // Check if the path N>E results in the same node as E>N - prerequisite to connect diagonal nodes (N & E are examples)
            if (sideConnectedNodePre.WalkTransitions.ContainsKey(postDirection) && sideConnectedNodePost.WalkTransitions.ContainsKey(preDirection) &&
                sideConnectedNodePre.WalkTransitions[postDirection].To == sideConnectedNodePost.WalkTransitions[preDirection].To)
            {
                // We have a target node
                BlockmapNode targetNode = sideConnectedNodePre.WalkTransitions[postDirection].To;
                bool canConnect = true;

                // Check if the target node is passable in all relevant directions
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(preDirection))) canConnect = false;
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(postDirection))) canConnect = false;
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(dir))) canConnect = false;

                // Check maximum headspace on the 2 adjacent cells between From and To
                int sideConnectedNodePreHeadspace = sideConnectedNodePre.FreeHeadSpace[sideConnectedNodePreDir];
                int sideConnectedNodePostHeadspace = sideConnectedNodePost.FreeHeadSpace[sideConnectedNodeostDir];

                int maxAllowedHeight = Mathf.Min(FreeHeadSpace[dir], targetNode.FreeHeadSpace[HelperFunctions.GetOppositeDirection(dir)], sideConnectedNodePreHeadspace, sideConnectedNodePostHeadspace);

                if (canConnect)
                {
                    AdjacentWalkTransition t = new AdjacentWalkTransition(this, targetNode, dir, maxAllowedHeight);
                    AddTransition(t, isWalkTransition: true);
                }
            }
        }

        public void SetClimbTransitions()
        {
            if (!IsPassable()) return;
            SetClimbTransition(Direction.N);
            SetClimbTransition(Direction.E);
            SetClimbTransition(Direction.S);
            SetClimbTransition(Direction.W);
        }
        private void SetClimbTransition(Direction dir)
        {
            if (!IsFlat(dir)) return;

            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (!adjNode.IsPassable()) continue;
                if (!adjNode.IsFlat(HelperFunctions.GetOppositeDirection(dir))) continue;

                if (ShouldCreateSingleClimbTransition(adjNode, dir, out List<IClimbable> climbList, out int maxHeightSingle))
                {
                    // Debug.Log("Creating single climb transition from " + ToString() + " to " + adjNode.ToString() + " in direction " + dir.ToString());
                    SingleClimbTransition t = new SingleClimbTransition(this, adjNode, dir, climbList, maxHeightSingle);
                    AddTransition(t, isWalkTransition: false);
                }
                else if(ShouldCreateDoubleClimbTransition(adjNode, dir, out List<IClimbable> climpUp, out List<IClimbable> climbDown, out int maxHeightDouble))
                {
                    // Debug.Log("Creating double climb transition from " + ToString() + " to " + adjNode.ToString() + " in direction " + dir.ToString());
                    DoubleClimbTransition t = new DoubleClimbTransition(this, adjNode, dir, climpUp, climbDown, maxHeightDouble);
                    AddTransition(t, isWalkTransition: false);
                }
            }
        }

        /// <summary>
        /// Returns if SingleClimbTransition should be created between a particular node and another node that is adjacent and higher.
        /// </summary>
        private bool ShouldCreateSingleClimbTransition(BlockmapNode to, Direction dir, out List<IClimbable> climb, out int maxTransitionHeight)
        {
            climb = new List<IClimbable>();
            maxTransitionHeight = 0;

            // Calculate some important values
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            int fromAltitude = GetMinAltitude(dir);
            int toAltitude = to.GetMaxAltitude(oppositeDir);
            int climbHeight = toAltitude - fromAltitude;

            if (climbHeight > 0)
            {
                if (FreeHeadSpace[Direction.None] <= climbHeight) return false; // A node above is blocking the climb

                climb = GetClimbUp(dir);

                int climbTopAltitude = toAltitude;
                int freeHeadspaceFrom = World.GetFreeHeadspace(new Vector3Int(WorldCoordinates.x, climbTopAltitude, WorldCoordinates.y)).Where(x => x.Key == Direction.None || x.Key == dir).Min(x => x.Value);
                int freeHeadSpaceTo = to.FreeHeadSpace[oppositeDir];
                maxTransitionHeight = Mathf.Min(freeHeadspaceFrom, freeHeadSpaceTo);

                return climb.Count == Mathf.Abs(climbHeight);
            }
            if (climbHeight < 0)
            {
                if (to.FreeHeadSpace[Direction.None] <= Mathf.Abs(climbHeight)) return false; // A node above is blocking the climb

                climb = to.GetClimbUp(oppositeDir);
                climb.Reverse();

                int climbTopAltitude = fromAltitude;
                int freeHeadspaceFrom = FreeHeadSpace[dir];
                int freeHeadSpaceTo = World.GetFreeHeadspace(new Vector3Int(to.WorldCoordinates.x, climbTopAltitude, to.WorldCoordinates.y)).Where(x => x.Key == Direction.None || x.Key == oppositeDir).Min(x => x.Value);
                maxTransitionHeight = Mathf.Min(freeHeadspaceFrom, freeHeadSpaceTo);

                return climb.Count == Mathf.Abs(climbHeight);
            }
            return false;
        }

        private bool ShouldCreateDoubleClimbTransition(BlockmapNode to, Direction dir, out List<IClimbable> climbUp, out List<IClimbable> climbDown, out int maxTransitionHeight)
        {
            climbUp = new List<IClimbable>();
            climbDown = new List<IClimbable>();
            maxTransitionHeight = 0;

            // Calculate some important values
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            int fromAltitude = GetMinAltitude(dir);
            int toAltitude = to.GetMaxAltitude(oppositeDir);

            climbUp = GetClimbUp(dir);
            climbDown = to.GetClimbUp(oppositeDir);
            climbDown.Reverse();

            int climbTopAltitude = fromAltitude + climbUp.Count;
            int freeHeadSpaceClimbUp = World.GetFreeHeadspace(new Vector3Int(WorldCoordinates.x, climbTopAltitude, WorldCoordinates.y))[dir];
            int freeHeadSpaceClimbDown = World.GetFreeHeadspace(new Vector3Int(to.WorldCoordinates.x, climbTopAltitude, to.WorldCoordinates.y))[oppositeDir];
            maxTransitionHeight = Mathf.Min(freeHeadSpaceClimbUp, freeHeadSpaceClimbDown);

            if (climbUp.Count == 0 || climbDown.Count == 0) return false; // climb up and down doesn't match up
            if (maxTransitionHeight < 1) return false; // not enough headspace

            return (fromAltitude + climbUp.Count - climbDown.Count) == toAltitude;
        }

        /// <summary>
        /// Returns the whole climb when trying to climb up from this node in a given side direction as an IClimbable-List, where each element represents the climbable for one altitude level.
        /// </summary>
        private List<IClimbable> GetClimbUp(Direction dir)
        {
            // Cache
            if (ClimbUpCache.TryGetValue(dir, out List<IClimbable> cachedClimb)) return cachedClimb;

            // Validation
            if (!HelperFunctions.GetSides().Contains(dir)) throw new System.Exception("A climb can only happen in a side direction (N/E/S/W)");

            // Set some important values
            List<IClimbable> climb = new List<IClimbable>();
            int currentAltitude = GetMinAltitude(dir);

            IClimbable nextClimbingPiece = World.GetClimbable(new Vector3Int(WorldCoordinates.x, currentAltitude, WorldCoordinates.y), dir);
            while(nextClimbingPiece != null)
            {
                if (!nextClimbingPiece.IsClimbable)
                {
                    climb = new List<IClimbable>();
                    ClimbUpCache.Add(dir, climb);
                    return climb; // No climb possible because of unclimbable piece
                }
                climb.Add(nextClimbingPiece);
                currentAltitude++;

                nextClimbingPiece = World.GetClimbable(new Vector3Int(WorldCoordinates.x, currentAltitude, WorldCoordinates.y), dir);
            }

            ClimbUpCache.Add(dir, climb);
            return climb;
        }

        #endregion

        #region Vision Target

        /// <summary>
        /// List containing all actors that have explored this node.
        /// </summary>
        private HashSet<Actor> ExploredBy = new HashSet<Actor>();

        /// <summary>
        /// List containing all entities that currently see this node.
        /// </summary>
        private HashSet<Entity> SeenBy = new HashSet<Entity>();

        public void AddVisionBy(Entity e)
        {
            ExploredBy.Add(e.Actor);
            SeenBy.Add(e);
        }
        public void RemoveVisionBy(Entity e)
        {
            SeenBy.Remove(e);
        }
        public void AddExploredBy(Actor p)
        {
            ExploredBy.Add(p);
        }
        public void RemoveExploredBy(Actor p)
        {
            ExploredBy.Remove(p);
        }

        public bool IsVisibleBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            if (Zones.Any(x => x.ProvidesVision && x.Actor == actor)) return true; // Node is in a zone of actor that provides vision
            if (SeenBy.FirstOrDefault(x => x.Actor == actor) != null) return true; // Node is seen by an entity of given actor

            return false;
        }
        public bool IsExploredBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            return ExploredBy.Contains(actor);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draws the surface mesh of this node onto the meshbuilder.
        /// </summary>
        public void DrawSurface(MeshBuilder meshBuilder)
        {
            if (SurfaceDef.RenderProperties.Type == SurfaceRenderType.NoRender) return;
            if (SurfaceDef.RenderProperties.Type == SurfaceRenderType.FlatBlendableSurface) NodeMeshGenerator.DrawFlatBlendableSurface(this, meshBuilder);
            if (SurfaceDef.RenderProperties.Type == SurfaceRenderType.CustomMeshGeneration) SurfaceDef.RenderProperties.CustomRenderFunction(this, meshBuilder);
        }

        /// <summary>
        /// Shows or hides the currently set tile overlay.
        /// </summary>
        public void ShowOverlay(bool show)
        {
            Mesh.ShowOverlay(show);
        }

        /// <summary>
        /// Shows the given texture as the tile overlay.
        /// <br/> Areas bigger than 1 only work for ground nodes.
        /// </summary>
        public void ShowOverlay(Texture2D texture, Color color, int size = 1)
        {
            Mesh.ShowOverlay(LocalCoordinates, texture, color, size);

            if (size > 1)
            {
                // Get chunks for each corner of the overlay area
                Chunk chunk_NW = World.GetChunk(WorldCoordinates.x, WorldCoordinates.y + size - 1);
                Chunk chunk_NE = World.GetChunk(WorldCoordinates.x + size - 1, WorldCoordinates.y + size - 1);
                Chunk chunk_SE = World.GetChunk(WorldCoordinates.x + size - 1, WorldCoordinates.y);

                if (chunk_NE != null && chunk_NE != Chunk)
                    chunk_NE.GroundMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x - Chunk.Size, LocalCoordinates.y - Chunk.Size), texture, color, size);

                if (chunk_NW != null && chunk_NW != Chunk)
                    chunk_NW.GroundMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x, LocalCoordinates.y - Chunk.Size), texture, color, size);

                if(chunk_SE != null && chunk_SE != Chunk)
                    chunk_SE.GroundMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x - Chunk.Size, LocalCoordinates.y), texture, color, size);
            }
        }

        public void ShowMultiOverlay(Texture2D texture, Color color)
        {
            Mesh.SetMultiOverlayTexture(texture, color);
            Mesh.ShowMultiOverlayOnNode(LocalCoordinates);
        }
        public void HideMultiOverlay()
        {
            Mesh.HideMultiOverlayOnNode(LocalCoordinates);
        }


        public void SetMesh(ChunkMesh mesh)
        {
            Mesh = mesh;
        }

        #endregion

        #region Actions

        public void SetSurface(SurfaceDef def)
        {
            if (def == null) throw new System.Exception("Cannot set SurfaceDef to null. That is not allowed.");
            SurfaceDef = def;
        }

        public void AddEntity(Entity e)
        {
            Entities.Add(e);
        }
        public void RemoveEntity(Entity e)
        {
            Entities.Remove(e);
        }

        public void AddZone(Zone z)
        {
            Zones.Add(z);

            // Explore tiles for zone owner if zone provides vision
            if (z.ProvidesVision) AddExploredBy(z.Actor);
        }
        public void RemoveZone(Zone z)
        {
            Zones.Remove(z);
        }

        #endregion

        #region Getters

        public bool HasFence => Fences.Count > 0;
        public Vector3 CenterWorldPosition { get; protected set; }
        public abstract void RecalculateCenterWorldPosition();

        /// <summary>
        /// Returns the minimum altitude on the given side of this node as a y coordinate. 
        /// </summary>
        public int GetMinAltitude(Direction side) => Altitude.Where(x => HelperFunctions.GetAffectedCorners(side).Contains(x.Key)).Min(x => x.Value);
        /// <summary>
        /// Returns the maximum altitude on the given side of this node as a y coordinate. 
        /// </summary>
        public int GetMaxAltitude(Direction side) => Altitude.Where(x => HelperFunctions.GetAffectedCorners(side).Contains(x.Key)).Max(x => x.Value);

        /// <summary>
        /// Checks and returns if a node with the same surface exists in the given direction with a matching height to this node.
        /// </summary>
        public bool HasSurfaceConnection(Direction dir)
        {
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
                if (adjNode.SurfaceDef == SurfaceDef && World.DoAdjacentHeightsMatch(this, adjNode, dir))
                    return true;
            return false;
        }

        /// <summary>
        /// Checks and returns if an adjacent node in the given direction with a seamless connection has an entity with the given def and height.
        /// </summary>
        public bool HasEntityConnection(Direction dir, EntityDef def, int entityHeight)
        {
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
                if (World.DoAdjacentHeightsMatch(this, adjNode, dir) && adjNode.Entities.Any(x => x.Def == def && x.Height == entityHeight))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns the exact altitude (compared to BaseAltitude) at the relative position within this node.
        /// </summary>
        public float GetExactLocalAltitudeAt(Vector2 relativePosition)
        {
            relativePosition = new Vector2(Mathf.Clamp01(relativePosition.x), Mathf.Clamp01(relativePosition.y)); // Clamp to [0-1]

            switch (Shape)
            {
                case "0000": return 0;
                case "0011": return relativePosition.y;
                case "1001": return (1f - relativePosition.x);
                case "1100": return (1f - relativePosition.y);
                case "0110": return relativePosition.x;

                case "0001":
                    if(GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x > relativePosition.y) return 0f;
                        else return relativePosition.y - relativePosition.x;
                    }
                    else return Mathf.Min(1f - relativePosition.x, relativePosition.y);
                case "0010":
                    if (!GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x + relativePosition.y < 1) return 0f;
                        else return relativePosition.y + relativePosition.x - 1f;
                    }
                    else return Mathf.Min(relativePosition.x, relativePosition.y);
                case "0100":
                    if (GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x < relativePosition.y) return 0f;
                        else return relativePosition.x - relativePosition.y;
                    }
                    else return Mathf.Min(relativePosition.x, 1f - relativePosition.y);
                case "1000":
                    if (!GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x + relativePosition.y > 1) return 0f;
                        else return -(relativePosition.x + relativePosition.y - 1f);
                    }
                    else return Mathf.Min(1f - relativePosition.x, 1f - relativePosition.y);

                case "1110":
                    if (GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x > relativePosition.y) return 1f;
                        else return 1f - (relativePosition.y - relativePosition.x);
                    }
                    else return Mathf.Max(relativePosition.x, 1f - relativePosition.y);
                case "1101":
                    if (!GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x + relativePosition.y < 1) return 1f;
                        else return 1f - (relativePosition.y + relativePosition.x - 1f);
                    }
                    else return Mathf.Max(1f - relativePosition.x, 1f - relativePosition.y);
                case "1011":
                    if (GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x < relativePosition.y) return 1f;
                        else return 1f - (relativePosition.x - relativePosition.y);
                    }
                    else return Mathf.Max(1f - relativePosition.x, relativePosition.y);
                case "0111":
                    if (!GetTriangleMeshShapeVariant())
                    {
                        if (relativePosition.x + relativePosition.y > 1) return 1f;
                        else return relativePosition.y + relativePosition.x;
                    }
                    else return Mathf.Max(relativePosition.x, relativePosition.y);

                case "1010":
                    if(LastHeightChangeWasIncrease)
                    {
                        if (relativePosition.x + relativePosition.y < 1) return -(relativePosition.x + relativePosition.y - 1f);
                        else return relativePosition.y + relativePosition.x - 1f;
                    }
                    else
                    {
                        if (relativePosition.x > relativePosition.y) return 1f - (relativePosition.x - relativePosition.y);
                        else return 1f - (relativePosition.y - relativePosition.x);
                    }
                case "0101":
                    if (LastHeightChangeWasIncrease)
                    {
                        if (relativePosition.x > relativePosition.y) return relativePosition.x - relativePosition.y;
                        else return relativePosition.y - relativePosition.x;
                    }
                    else
                    {
                        if (relativePosition.x + relativePosition.y > 1) return 1f - (relativePosition.y + relativePosition.x - 1f);
                        else return relativePosition.y + relativePosition.x;
                    }

                case "2101":
                    if (relativePosition.x + relativePosition.y < 1) return 1f + (-(relativePosition.x + relativePosition.y - 1f));
                    else return 1f - (relativePosition.y + relativePosition.x - 1f);
                case "0121":
                    if (relativePosition.x + relativePosition.y > 1) return 1f + (relativePosition.y + relativePosition.x - 1f);
                    else return relativePosition.y + relativePosition.x;
                case "1012":
                    if (relativePosition.x < relativePosition.y) return 1f + (relativePosition.y - relativePosition.x);
                    else return 1f - (relativePosition.x - relativePosition.y);
                case "1210":
                    if (relativePosition.x > relativePosition.y) return 1f + (relativePosition.x - relativePosition.y);
                    else return 1f - (relativePosition.y - relativePosition.x);
            }

            throw new System.Exception("Case not yet implemented. Shape " + Shape + " relative height implementation is missing.");
        }

        /// <summary>
        /// Returns how the triangles should be built to draw the mesh of this node.
        /// <br/> If true, the standard variant (SW, SE, NE / SW, NE, NW) is used.
        /// <br/> If false, the alternate variant (SW, SE, NW / SE, NE, NW) is used.
        /// </summary>
        public bool GetTriangleMeshShapeVariant()
        {
            switch (Shape)
            {
                case "0000":
                case "1100":
                case "0110":
                case "0011":
                case "1001":
                case "1012":
                case "1210":
                    return true;

                case "2101":
                case "0121":
                    return false;

                case "0001":
                case "1011":
                case "0100":
                case "1110":
                    if (SurfaceDef.RenderProperties.UseLongEdges) return false;
                    else return true;

                case "1000":
                case "0010":
                case "0111":
                case "1101":
                    if (SurfaceDef.RenderProperties.UseLongEdges) return true;
                    else return false;

                case "1010":
                    if (LastHeightChangeWasIncrease) return false;
                    else return true;

                case "0101":
                    if (LastHeightChangeWasIncrease) return true;
                    else return false;
            }

            throw new System.Exception();
        }

        /// <summary>
        /// Returns the world y position for the relative position (0f-1f) on this node.
        /// </summary>
        public float GetWorldHeightAt(Vector2 relativePosition)
        {
            return BaseWorldHeight + (World.NodeHeight * GetExactLocalAltitudeAt(relativePosition));
        }

        /// <summary>
        /// Returns if it is theoretically possible for some entity to stand on this node.
        /// </summary>
        protected virtual bool IsGenerallyPassable()
        {
            if (Entities.Any(x => x.Def.Impassable)) return false; // An entity is blocking this node
            return true;
        }
        /// <summary>
        /// Returns if an entity can stand on this node.
        /// </summary>
        protected virtual bool CanEntityStandHere(Entity e)
        {
            return true;
        }

        public bool IsPassable() => IsPassable(Direction.None);
        public bool IsPassable(Direction dir) => MaxPassableHeight[dir] > 0;
        public bool IsPassable(Entity e) => IsPassable(Direction.None, e);
        public bool IsPassable(Direction dir, Entity e) => MaxPassableHeight[dir] >= e.Height && CanEntityStandHere(e);

        /// <summary>
        /// Returns if the given corner is blocked by a fence or wall.
        /// </summary>
        private bool IsCornerBlocked(Direction corner)
        {
            if (Fences.ContainsKey(corner)) return true; // Fence on corner

            // Check for wall corner piece
            int cornerAltitude = Altitude[corner];
            Vector3Int globalCellCoordinate = new Vector3Int(WorldCoordinates.x, cornerAltitude, WorldCoordinates.y);
            if (World.GetWall(globalCellCoordinate, corner) != null) return true; // Wall on corner

            return false;
        }

        /// <summary>
        /// Returns if there is a ladder, fence or door directly blocking this side of the node.
        /// </summary>
        private bool IsSideBlocked(Direction side)
        {
            // Ladders
            if (SourceLadders.ContainsKey(side)) return true;

            // Fences
            if (Fences.ContainsKey(side)) return true;

            // Doors
            foreach (Door door in Doors.Values)
                if (door.CurrentBlockingDirection == side)
                    return true;

            // Walls (in sloped nodes, this checks is a wall within the slope, for flat slopes this for loop gets skipped)
            for (int i = GetMinAltitude(side); i < GetMaxAltitude(side); i++)
            {
                Vector3Int globalCellCoordinates = new Vector3Int(WorldCoordinates.x, i, WorldCoordinates.y);
                List<Wall> walls = World.GetWalls(globalCellCoordinates);
                foreach (Wall w in walls)
                    if (w.Side == side) return true;
            }

            return false;
        }

        public bool IsFlat() => Altitude.Values.All(x => x == Altitude[Direction.SW]);
        public bool IsFlat(Direction dir) => Altitude.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Select(x => x.Value).All(x => x == Altitude[HelperFunctions.GetAffectedCorners(dir)[0]]);

        public bool IsSlope()
        {
            if (Altitude[Direction.NW] == Altitude[Direction.NE] && Altitude[Direction.SW] == Altitude[Direction.SE] && Altitude[Direction.NW] != Altitude[Direction.SW]) return true;
            if (Altitude[Direction.NW] == Altitude[Direction.SW] && Altitude[Direction.NE] == Altitude[Direction.SE] && Altitude[Direction.NW] != Altitude[Direction.NE]) return true;
            return false;
        }

        public bool IsAbove(BlockmapNode otherNode)
        {
            return World.IsAbove(Altitude, otherNode.Altitude);
        }

        public override string ToString()
        {
            string mph = "MPH:";
            foreach (var x in MaxPassableHeight) mph += x.Key.ToString() + ":" + x.Value + ",";
            string headspace = "FSH";
            foreach (var x in FreeHeadSpace) headspace += x.Key.ToString() + ":" + x.Value + ",";
            return Type.ToString() + WorldCoordinates.ToString() + " alt:" + BaseAltitude + "-" + MaxAltitude + " prop:" + SurfaceDef.Label + "\n" + headspace + "\n" + mph;
        }
        public string DebugInfoShort()
        {
            return $"{SurfaceDef.LabelCap} {WorldCoordinates} {BaseAltitude}-{MaxAltitude} ({Type})";
        }

        public string DebugInfoLong()
        {
            string text = DebugInfoShort();

            string mph = "Max Passable Height:";
            foreach (var x in MaxPassableHeight) mph += x.Key.ToString() + ":" + x.Value + ",";
            string headspace = "Free Head Space:";
            foreach (var x in FreeHeadSpace) headspace += x.Key.ToString() + ":" + x.Value + ",";

            text += "\nMovement Speed Modifier: " + SurfaceDef.MovementSpeedModifier;
            text += "\nShape: " + World.HoveredNode.Shape;
            text += "\nRelHeight: " + World.HoveredNode.GetExactLocalAltitudeAt(new Vector2(World.HoveredWorldPosition.x - World.HoveredWorldCoordinates.x, World.HoveredWorldPosition.z - World.HoveredWorldCoordinates.y));
            text += "\n" + mph;
            text += "\n" + headspace;
            if (Entities.Count > 0) text += $"\nis origin node of {Entities.Count} entities";

            return text;
        }

        public virtual string ToStringShort() => SurfaceDef.Label + "(" + WorldCoordinates.x + ", " + BaseAltitude + "-" + MaxAltitude + ", " + WorldCoordinates.y + ")";

        /// <summary>
        /// Returns a list with all nodes where a path to exists for the given entity with a cost less than the given limit.
        /// </summary>
        public List<BlockmapNode> GetNodesInRange(float maxCost, Entity entity = null)
        {
            // Setup
            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();

            // Start with origin node
            priorityQueue.Add(this, 0f);
            nodeCosts.Add(this, 0f);

            while (priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach (Transition t in currentNode.Transitions)
                {
                    BlockmapNode toNode = t.To;
                    float transitionCost = t.GetMovementCost(entity);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > maxCost) continue; // not within cost limit
                    if (entity != null && !t.CanPass(entity)) continue;

                    // Node has not yet been visited or cost is lower than previously lowest cost => Update
                    if (!nodeCosts.ContainsKey(toNode) || totalCost < nodeCosts[toNode])
                    {
                        // Update cost to this node
                        nodeCosts[toNode] = totalCost;

                        // Add target node to queue to continue search
                        if (!priorityQueue.ContainsKey(toNode) || priorityQueue[toNode] > totalCost)
                            priorityQueue[toNode] = totalCost;
                    }
                }
            }

            // No more nodes to check -> target not in range
            return nodeCosts.Keys.ToList();
        }

        #endregion

        #region Save / Load

        public void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadVector2Int(ref WorldCoordinates, "worldCoordinates");
            SaveLoadManager.SaveOrLoadVector2Int(ref LocalCoordinates, "localCoordinates");
            SaveLoadManager.SaveOrLoadAltitudeDictionary(ref Altitude);
            SaveLoadManager.SaveOrLoadDef(ref SurfaceDef, "surface");
        }

        #endregion
    }
}
