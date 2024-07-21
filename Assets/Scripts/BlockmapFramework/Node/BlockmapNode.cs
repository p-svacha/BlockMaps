using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents a specific location on the worldmap, which is used for pathfinding.
    /// <br/> A BlockmapNode is on one specific world coordinate but can have different heights for its corners.
    /// <br/> All entities are tied to a BlockmapNode.
    /// </summary>
    public abstract class BlockmapNode
    {
        public int Id { get; private set; }

        /// <summary>
        /// Height of the 4 corners of the node: {SW, SE, NE, NW}
        /// </summary>
        public Dictionary<Direction, int> Altitude { get; protected set; }

        /// <summary>
        /// Lowest y coordinate of this node.
        /// </summary>
        public int BaseAltitude { get; private set; }
        public float BaseWorldHeight => BaseAltitude * World.TILE_HEIGHT;

        /// <summary>
        /// Highest point of this node.
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
        public Vector2Int WorldCoordinates { get; private set; }
        public Vector2 WorldCenter2D => WorldCoordinates + new Vector2(0.5f, 0.5f);
        public Vector2Int LocalCoordinates { get; private set; }
        public abstract NodeType Type { get; }
        public abstract bool IsSolid { get; } // Flag if entities can (generally) be placed on top of this node

        // Connections
        /// <summary>
        /// ALL transition going out from this node to other nodes. Dictionary key is the target node.
        /// </summary>
        public Dictionary<BlockmapNode, Transition> Transitions { get; private set; }
        /// <summary>
        /// All transitions going out from this node to adjacent nodes (in all 8 directions) that can be reached with normal walking in that direction.
        /// </summary>
        public Dictionary<Direction, Transition> WalkTransitions { get; private set; }

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
        /// List containing all players that have explored this node.
        /// </summary>
        private HashSet<Actor> ExploredBy = new HashSet<Actor>();

        /// <summary>
        /// List containing all entities that currently see this node.
        /// </summary>
        private HashSet<Entity> SeenBy = new HashSet<Entity>();

        /// <summary>
        /// The mesh in the world that this node is drawn on.
        /// </summary>
        protected ChunkMesh Mesh { get; private set; }

        #region Initialize

        protected BlockmapNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height)
        {
            World = world;
            Chunk = chunk;
            Id = id;

            LocalCoordinates = new Vector2Int(localCoordinates.x, localCoordinates.y);
            WorldCoordinates = chunk.GetWorldCoordinates(LocalCoordinates);
            Altitude = height;

            RecalculateShape();
            Transitions = new Dictionary<BlockmapNode, Transition>();
            WalkTransitions = new Dictionary<Direction, Transition>();
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

        #region Transitions
        /// ************* TRANSITIONS *************
        /// When the navmesh of an area gets updated, a specific order needs to be followed.
        /// Each step needs to be executed for ALL affected nodes before the next step can be executed for ALL affected nodes:
        /// 1. ResetTransitions()
        /// 2. SetStraightAdjacentTransitions()
        /// 3. SetDiagonalAdjacentTransitions()
        /// 4. SetCliffClimbTransitions()
        /// </summary>

        public void ResetTransitions()
        {
            Transitions.Clear();
            WalkTransitions.Clear();
        }

        /// <summary>
        /// Removes
        /// </summary>
        public void RemoveTransition(BlockmapNode target)
        {
            Transitions.Remove(target);
        }

        /// <summary>
        /// Updates the straight neighbours by applying the general rule:
        /// If there is an adjacent passable node in the direction with matching heights, connect it as a neighbour.
        /// </summary>
        public void SetStraightAdjacentTransitions()
        {
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

            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (!adjNode.IsPassable(HelperFunctions.GetOppositeDirection(dir))) continue;

                if(ShouldConnectToNodeDirectly(adjNode, dir)) // Connect to node directly
                {
                    AdjacentWalkTransition t = new AdjacentWalkTransition(this, adjNode, dir);
                    Transitions.Add(adjNode, t);
                    WalkTransitions.Add(dir, t);
                }
            }
        }
        /// <summary>
        /// Returns if this node should be directly connected to the given node (that is adjacent in the given direction)
        /// </summary>
        protected virtual bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            return World.DoAdjacentHeightsMatch(this, adjNode, dir);
        }

        /// <summary>
        /// Updates diagonal neighbours by applying the genereal rule:
        /// If the path N>E results in the same node as E>N, then connect NE to that node
        /// </summary>
        public void SetDiagonalAdjacentTransitions()
        {
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
            if (!sideConnectedNodePre.IsPassable(HelperFunctions.GetMirroredCorner(dir, preDirection))) return;
            if (!sideConnectedNodePost.IsPassable(HelperFunctions.GetMirroredCorner(dir, postDirection))) return;

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

                if (canConnect)
                {
                    AdjacentWalkTransition t = new AdjacentWalkTransition(this, targetNode, dir);
                    Transitions.Add(targetNode, t);
                    WalkTransitions.Add(dir, t);
                }
            }
        }

        public void SetClimbTransitions()
        {
            SetClimbTransition(Direction.N);
            SetClimbTransition(Direction.E);
            SetClimbTransition(Direction.S);
            SetClimbTransition(Direction.W);
        }
        private void SetClimbTransition(Direction dir)
        {
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (ShouldCreateSingleClimbTransition(adjNode, dir, out List<IClimbable> climbList))
                {
                    Debug.Log("Creating single climb transition from " + ToString() + " to " + adjNode.ToString() + " in direction " + dir.ToString());
                    SingleClimbTransition t = new SingleClimbTransition(this, adjNode, dir, climbList);
                    Transitions.Add(adjNode, t);
                }
                else if(ShouldCreateDoubleClimbTransition(adjNode, dir, out List<IClimbable> climpUp, out List<IClimbable> climbDown))
                {
                    Debug.Log("Creating double climb transition from " + ToString() + " to " + adjNode.ToString() + " in direction " + dir.ToString());
                    DoubleClimbTransition t = new DoubleClimbTransition(this, adjNode, dir, climpUp, climbDown);
                    Transitions.Add(adjNode, t);
                }
            }
        }

        /// <summary>
        /// Returns if SingleClimbTransition should be created between a particular node and another node that is adjacent and higher.
        /// </summary>
        private bool ShouldCreateSingleClimbTransition(BlockmapNode to, Direction dir, out List<IClimbable> climb)
        {
            climb = new List<IClimbable>();

            // Calculate some important values
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            int fromAltitude = GetMinAltitude(dir);
            int toAltitude = to.GetMaxAltitude(oppositeDir);
            int climbHeight = toAltitude - fromAltitude;

            if (!IsFlat(dir)) return false;
            if (!to.IsFlat(oppositeDir)) return false;

                if (climbHeight > 0)
            {
                climb = GetClimbUp(dir);
                return climb.Count == Mathf.Abs(climbHeight);
            }
            if (climbHeight < 0)
            {
                climb = to.GetClimbUp(oppositeDir);
                climb.Reverse();
                return climb.Count == Mathf.Abs(climbHeight);
            }
            return false;
        }

        private bool ShouldCreateDoubleClimbTransition(BlockmapNode to, Direction dir, out List<IClimbable> climbUp, out List<IClimbable> climbDown)
        {
            // Calculate some important values
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            int fromAltitude = GetMinAltitude(dir);
            int toAltitude = to.GetMaxAltitude(oppositeDir);

            climbUp = GetClimbUp(dir);
            climbDown = to.GetClimbUp(oppositeDir);
            climbDown.Reverse();

            if (climbUp.Count == 0 || climbDown.Count == 0) return false; // climb up and down doesn't match up
            if (GetFreeHeadSpace(dir) < climbUp.Count + 1) return false; // not enough headspace at end of climb up
            if (to.GetFreeHeadSpace(oppositeDir) < climbDown.Count + 1) return false; // not enough headspace at start of climb down

            return (fromAltitude + climbUp.Count - climbDown.Count) == toAltitude;
        }

        /// <summary>
        /// Returns the whole climb when trying to climb up from this node in a given side direction as an IClimbable-List, where each element represents the climbable for one altitude level.
        /// </summary>
        public List<IClimbable> GetClimbUp(Direction dir)
        {
            if (!HelperFunctions.GetSides().Contains(dir)) throw new System.Exception("A climb can only happen in a side direction (N/E/S/W)");

            // Set some important values
            List<IClimbable> climb = new List<IClimbable>();
            int currentAltitude = GetMinAltitude(dir);

            IClimbable nextClimbingPiece = World.GetClimbable(new Vector3Int(WorldCoordinates.x, currentAltitude, WorldCoordinates.y), dir);
            while(nextClimbingPiece != null)
            {
                climb.Add(nextClimbingPiece);
                currentAltitude++;

                nextClimbingPiece = World.GetClimbable(new Vector3Int(WorldCoordinates.x, currentAltitude, WorldCoordinates.y), dir);
            }

            return climb;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adds all of this nodes vertices and triangles to the given MeshBuilder.
        /// </summary>
        public abstract void Draw(MeshBuilder meshBuilder);

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

        public void AddVisionBy(Entity e)
        {
            ExploredBy.Add(e.Owner);
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

        #endregion

        #region Getters

        public bool HasFence => Fences.Count > 0;
        public abstract Surface GetSurface();
        public abstract SurfaceProperties GetSurfaceProperties();
        public abstract Vector3 GetCenterWorldPosition();

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
                if (adjNode.GetSurface() == GetSurface() && World.DoAdjacentHeightsMatch(this, adjNode, dir))
                    return true;
            return false;
        }

        /// <summary>
        /// Checks and returns if an adjacent node in the given direction with a seamless connection has an entity of the given type prefix.
        /// </summary>
        public bool HasEntityConnection(Direction dir, string typeId)
        {
            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
                if (World.DoAdjacentHeightsMatch(this, adjNode, dir) && adjNode.Entities.Any(x => x.TypeId == typeId))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns the relative height (compared to BaseHeight) at the relative position within this node.
        /// </summary>
        public float GetRelativeHeightAt(Vector2 relativePosition)
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
                    if (GetSurface() != null && GetSurface().UseLongEdges) return false;
                    else return true;

                case "1000":
                case "0010":
                case "0111":
                case "1101":
                    if (GetSurface() != null && GetSurface().UseLongEdges) return true;
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
            return BaseWorldHeight + (World.TILE_HEIGHT * GetRelativeHeightAt(relativePosition));
        }

        /// <summary>
        /// Returns if this node is visible for the specified player.
        /// </summary>
        public bool IsVisibleBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            if (Zones.Any(x => x.ProvidesVision && x.Actor == actor)) return true; // Node is in a zone of player that provides vision
            if (SeenBy.FirstOrDefault(x => x.Owner == actor) != null) return true; // Node is seen by an entity of player

            return false;
        }

        /// <summary>
        /// Returns if the node has been explored by the specified player.
        /// </summary>
        public bool IsExploredBy(Actor player)
        {
            if (player == null) return true; // Everything is visible
            return ExploredBy.Contains(player);
        }

        public bool IsSeenBy(Entity e) => SeenBy.Contains(e);

        /// <summary>
        /// Returns if an entity can stand on this node.
        /// <br/> If entity is null a general check will be made for the navmesh.
        /// </summary>
        public virtual bool IsPassable(Entity entity = null)
        {
            if (Entities.Any(x => !x.IsPassable)) return false; // An entity is blocking this node

            return true;
        }

        /// <summary>
        /// Returns if an entity can pass through a specific side (N/E/S/W) of this node.
        /// <br/> If entity is null a general check will be made for the navmesh.
        /// </summary>
        public bool IsPassable(Direction dir, Entity entity = null, bool checkClimbables = true)
        {
            // Check if node is generally passable
            if (!IsPassable(entity)) return false;

            // Special checks for corner directions
            if(HelperFunctions.GetCorners().Contains(dir))
            {
                if (Fences.ContainsKey(dir)) return false;
                if (!HelperFunctions.GetAffectedSides(dir).All(x => IsPassable(x, entity))) return false;

                return true;
            }

            // Check if a climbable is blocking
            if (checkClimbables)
            {
                // Ladders
                if (SourceLadders.ContainsKey(dir)) return false;

                // Fences
                if (Fences.ContainsKey(dir)) return false;

                // Walls
                for(int i = GetMinAltitude(dir); i <= GetMaxAltitude(dir); i++)
                {
                    Vector3Int globalCellCoordinates = new Vector3Int(WorldCoordinates.x, i, WorldCoordinates.y);
                    List<Wall> walls = World.GetWalls(globalCellCoordinates);
                    foreach (Wall w in walls)
                        if (w.Side == dir) return false;
                }
            }

            // Check if the side has enough head space for the entity
            int headSpace = GetFreeHeadSpace(dir);
            if (headSpace <= 0) return false; // Another node above this one is blocking this(by overlapping in at least 1 corner)
            if (entity != null && entity.Height > headSpace) return false; // A node above is blocking the space for the entity

            return true;
        }

        public bool IsFlat() => Altitude.Values.All(x => x == Altitude[Direction.SW]);
        public bool IsFlat(Direction dir) => Altitude.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Select(x => x.Value).All(x => x == Altitude[HelperFunctions.GetAffectedCorners(dir)[0]]);

        public bool IsSlope()
        {
            if (Altitude[Direction.NW] == Altitude[Direction.NE] && Altitude[Direction.SW] == Altitude[Direction.SE] && Altitude[Direction.NW] != Altitude[Direction.SW]) return true;
            if (Altitude[Direction.NW] == Altitude[Direction.SW] && Altitude[Direction.NE] == Altitude[Direction.SE] && Altitude[Direction.NW] != Altitude[Direction.NE]) return true;
            return false;
        }

        /// <summary>
        /// Returns the minimun amount of space (in amount of tiles) that is free above this node in the given direction by checking all corners that are affected by that direction.
        /// <br/> For example a flat node right above this flat node would be 1.
        /// <br/> If any corner of an above node overlaps with this node 0 is returned.
        /// <br/> Direction.None can be passed to check all corners.
        /// </summary>
        public int GetFreeHeadSpace(Direction dir)
        {
            List<BlockmapNode> nodesAbove = World.GetNodes(WorldCoordinates, MaxAltitude, World.MAX_ALTITUDE).Where(x => x != this && x.IsSolid && !World.DoFullyOverlap(this, x)).ToList();

            int minHeight = World.MAX_ALTITUDE;

            foreach (BlockmapNode node in nodesAbove)
            {
                foreach(Direction corner in HelperFunctions.GetAffectedCorners(dir))
                {
                    int diff = node.Altitude[corner] - Altitude[corner];
                    if (diff < minHeight) minHeight = diff;
                }
            }

            return minHeight;
        }

        public override string ToString()
        {
            return Type.ToString() + WorldCoordinates.ToString() +  " alt:" + BaseAltitude + " " + GetSurfaceProperties().Name;
        }

        #endregion

        #region Save / Load

        public static BlockmapNode Load(World world, Chunk chunk, NodeData data)
        {
            switch(data.Type)
            {
                case NodeType.Ground:
                    return new GroundNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), (SurfaceId)data.SubType);

                case NodeType.Air:
                    return new AirNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), (SurfaceId)data.SubType);

                case NodeType.Water:
                    return new WaterNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height));
            }
            throw new System.Exception("Type " + data.Type.ToString() + " not handled.");
        }
        private static Dictionary<Direction, int> LoadHeight(int[] height)
        {
            return new Dictionary<Direction, int>()
            {
                { Direction.SW, height[0] },
                { Direction.SE, height[1] },
                { Direction.NE, height[2] },
                { Direction.NW, height[3] },
            };
        }

        public NodeData Save()
        {
            return new NodeData
            {
                Id = Id,
                LocalCoordinateX = LocalCoordinates.x,
                LocalCoordinateY = LocalCoordinates.y,
                Height = new int[] { Altitude[Direction.SW], Altitude[Direction.SE], Altitude[Direction.NE], Altitude[Direction.NW] },
                Type = Type,
                SubType = GetSubType()
            };
        }

        public abstract int GetSubType();

        #endregion
    }
}
