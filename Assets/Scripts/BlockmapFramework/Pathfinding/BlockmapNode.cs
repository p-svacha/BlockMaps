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
        public Dictionary<Direction, int> Height { get; protected set; }

        /// <summary>
        /// Lowest point of this node.
        /// </summary>
        public int BaseHeight { get; private set; }
        public float BaseWorldHeight => BaseHeight * World.TILE_HEIGHT;

        /// <summary>
        /// Highest point of this node.
        /// </summary>
        public int MaxHeight { get; private set; }

        /// <summary>
        /// Shape is saved in a string with 4 chars, where each char is a corner (SW, SE, NE, NW) storing the height above the min height of the node.
        /// <br/> For example "1001" is a west-facing up-slope.
        /// </summary>
        public string Shape { get; protected set; }

        /// <summary>
        /// Shapes with the format "1010" or "0101" have two possible variants (center high or center low). This flag decides which variant is used in that case.
        /// </summary>
        public bool UseAlternativeVariant;

        // Node attributes
        public World World { get; private set; }
        public Chunk Chunk { get; private set; }
        public Vector2Int WorldCoordinates { get; private set; }
        public Vector2 WorldCenter2D => WorldCoordinates + new Vector2(0.5f, 0.5f);
        public Vector2Int LocalCoordinates { get; private set; }
        public Surface Surface { get; private set; }
        public abstract NodeType Type { get; }
        public abstract bool IsPath { get; }
        public abstract bool IsSolid { get; } // Flag if entities can (generally) be placed on top of this node

        // Connections
        /// <summary>
        /// ALL transition going out from this node to other nodes. Dictionary key is the target node.
        /// </summary>
        public Dictionary<BlockmapNode, Transition> Transitions { get; private set; }
        /// <summary>
        /// All transitions going out from this node to adjacent nodes (in all 8 directions) that can be reached with normal walking in that direction.
        /// </summary>
        public Dictionary<Direction, Transition> AdjacentTransitions { get; private set; }

        // Things on this node
        public HashSet<Entity> Entities = new HashSet<Entity>();
        public Dictionary<Direction, Wall> Walls = new Dictionary<Direction, Wall>();
        public Dictionary<Direction, Ladder> Ladders = new Dictionary<Direction, Ladder>();

        /// <summary>
        /// List containing all players that have explored this node.
        /// </summary>
        private HashSet<Player> ExploredBy = new HashSet<Player>();

        /// <summary>
        /// List containing all entities that currently see this node.
        /// </summary>
        private HashSet<Entity> SeenBy = new HashSet<Entity>();

        /// <summary>
        /// The mesh in the world that this node is drawn on.
        /// </summary>
        protected ChunkMesh Mesh { get; private set; }

        #region Initialize

        protected BlockmapNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceId surface)
        {
            World = world;
            Chunk = chunk;
            Id = id;

            LocalCoordinates = new Vector2Int(localCoordinates.x, localCoordinates.y);
            WorldCoordinates = chunk.GetWorldCoordinates(LocalCoordinates);
            Height = height;

            RecalculateShape();
            Surface = (surface == SurfaceId.Null) ? null : SurfaceManager.Instance.GetSurface(surface);
            Transitions = new Dictionary<BlockmapNode, Transition>();
            AdjacentTransitions = new Dictionary<Direction, Transition>();
        }

        /// <summary>
        /// Calculates the base height, relative heights and shape according th this nodes heights.
        /// </summary>
        protected void RecalculateShape()
        {
            BaseHeight = Height.Values.Min();
            MaxHeight = Height.Values.Max();
            Shape = GetShape(Height);
        }

        protected string GetShape(Dictionary<Direction, int> height)
        {
            int baseHeight = height.Values.Min();
            string binaryShape = "";
            foreach (Direction dir in HelperFunctions.GetCorners()) binaryShape += (height[dir] - baseHeight);
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
            AdjacentTransitions.Clear();
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
                    AdjacentTransitions.Add(dir, t);
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

            Direction preDirection = HelperFunctions.GetNextAnticlockwiseDirection8(dir);
            Direction postDirection = HelperFunctions.GetNextClockwiseDirection8(dir);
            BlockmapNode sideConnectedNodePre = AdjacentTransitions.ContainsKey(preDirection) ? AdjacentTransitions[preDirection].To : null;
            BlockmapNode sideConnectedNodePost = AdjacentTransitions.ContainsKey(postDirection) ? AdjacentTransitions[postDirection].To : null;

            if (sideConnectedNodePre == null) return;
            if (sideConnectedNodePost == null) return;
            if (!sideConnectedNodePre.IsPassable(HelperFunctions.GetMirroredCorner(dir, preDirection))) return;
            if (!sideConnectedNodePost.IsPassable(HelperFunctions.GetMirroredCorner(dir, postDirection))) return;

            // Check if the path N>E results in the same node as E>N - prerequisite to connect diagonal nodes (N & E are examples)
            if (sideConnectedNodePre.AdjacentTransitions.ContainsKey(postDirection) && sideConnectedNodePost.AdjacentTransitions.ContainsKey(preDirection) &&
                sideConnectedNodePre.AdjacentTransitions[postDirection].To == sideConnectedNodePost.AdjacentTransitions[preDirection].To)
            {
                // We have a target node
                BlockmapNode targetNode = sideConnectedNodePre.AdjacentTransitions[postDirection].To;
                bool canConnect = true;

                // Check if the target node is passable in all relevant directions
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(preDirection))) canConnect = false;
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(postDirection))) canConnect = false;
                if (!targetNode.IsPassable(HelperFunctions.GetOppositeDirection(dir))) canConnect = false;

                if (canConnect)
                {
                    AdjacentWalkTransition t = new AdjacentWalkTransition(this, targetNode, dir);
                    Transitions.Add(targetNode, t);
                    AdjacentTransitions.Add(dir, t);
                }
            }
        }

        public void SetSingleClimbTransitions()
        {
            SetSingleClimbTransition(Direction.N);
            SetSingleClimbTransition(Direction.E);
            SetSingleClimbTransition(Direction.S);
            SetSingleClimbTransition(Direction.W);
        }
        private void SetSingleClimbTransition(Direction dir)
        {
            if (!IsPassable(dir, checkLadder: false)) return;

            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
                if (!adjNode.IsPassable(oppositeDir, checkLadder: false)) continue; // No transition to unpassable nodes

                // Calculate some important values
                int fromHeight = GetMinHeight(dir);
                int toHeight = adjNode.GetMaxHeight(oppositeDir);
                if (fromHeight == toHeight) continue; // No transition when matching height
                int heightDiff = Mathf.Abs(toHeight - fromHeight);
                bool isAscend = toHeight > fromHeight;
                BlockmapNode lowerNode = isAscend ? this : adjNode;
                Direction lowerDir = isAscend ? dir : oppositeDir;

                int headspace = lowerNode.GetFreeHeadSpace(lowerDir);
                if (headspace <= heightDiff) continue; // Another node is blocking the transition

                if (!IsFlat(dir)) continue; // Transition base needs to be flat
                if (!adjNode.IsFlat(oppositeDir)) continue; // Transition target needs to be flat

                if (ShouldCreateLadderTransition(lowerNode, lowerDir))
                {
                    float cost = isAscend ? SingleClimbTransition.LADDER_COST_UP : SingleClimbTransition.LADDER_COST_DOWN;
                    float speed = isAscend ? SingleClimbTransition.LADDER_SPEED_UP : SingleClimbTransition.LADDER_SPEED_DOWN;
                    SingleClimbTransition t = new SingleClimbTransition(this, adjNode, dir, cost, speed, LadderMeshGenerator.LADDER_POLE_SIZE);
                    Transitions.Add(adjNode, t);
                }
                else if (ShouldCreateCliffTransitionTo(adjNode))
                {
                    float cost = isAscend ? SingleClimbTransition.CLIFF_COST_UP : SingleClimbTransition.CLIFF_COST_DOWN;
                    float speed = isAscend ? SingleClimbTransition.CLIFF_SPEED_UP : SingleClimbTransition.CLIFF_SPEED_DOWN;
                    SingleClimbTransition t = new SingleClimbTransition(this, adjNode, dir, cost, speed, 0f);
                    Transitions.Add(adjNode, t);
                }
            }
        }
        /// <summary>
        /// Returns if this node should create a climbing connection to the given node (that is adjacent in the given direction)
        /// </summary>
        protected bool ShouldCreateCliffTransitionTo(BlockmapNode adjNode)
        {
            if (Type != NodeType.Surface) return false;
            if (adjNode.Type != NodeType.Surface) return false;

            return true;
        }
        protected bool ShouldCreateLadderTransition(BlockmapNode lowerNode, Direction side)
        {
            if (!lowerNode.Ladders.ContainsKey(side)) return false;

            return true;
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
        /// <br/> Areas bigger than 1 only work for surface nodes.
        /// </summary>
        public void ShowOverlay(Texture2D texture, Color color, int size = 1)
        {
            Mesh.ShowOverlay(LocalCoordinates, texture, color, size);
            if(LocalCoordinates.x + size >= Chunk.Size && LocalCoordinates.y + size >= Chunk.Size)
            {
                World.Chunks.TryGetValue(new Vector2Int(Chunk.Coordinates.x + 1, Chunk.Coordinates.y + 1), out Chunk chunk_NE);
                if(chunk_NE != null) chunk_NE.SurfaceMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x - Chunk.Size, LocalCoordinates.y - Chunk.Size), texture, color, size);

                World.Chunks.TryGetValue(new Vector2Int(Chunk.Coordinates.x + 1, Chunk.Coordinates.y), out Chunk chunk_E);
                if(chunk_E != null) chunk_E.SurfaceMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x - Chunk.Size, LocalCoordinates.y), texture, color, size);

                World.Chunks.TryGetValue(new Vector2Int(Chunk.Coordinates.x, Chunk.Coordinates.y + 1), out Chunk chunk_N);
                if(chunk_N != null) chunk_N.SurfaceMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x, LocalCoordinates.y - Chunk.Size), texture, color, size);
            }
            else if (LocalCoordinates.x + size >= Chunk.Size)
            {
                World.Chunks.TryGetValue(new Vector2Int(Chunk.Coordinates.x + 1, Chunk.Coordinates.y), out Chunk chunk_E);
                if (chunk_E != null) chunk_E.SurfaceMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x - Chunk.Size, LocalCoordinates.y), texture, color, size);
            }
            else if (LocalCoordinates.y + size >= Chunk.Size)
            {
                World.Chunks.TryGetValue(new Vector2Int(Chunk.Coordinates.x, Chunk.Coordinates.y + 1), out Chunk chunk_N);
                if (chunk_N != null) chunk_N.SurfaceMesh.ShowOverlay(new Vector2Int(LocalCoordinates.x, LocalCoordinates.y - Chunk.Size), texture, color, size);
            }
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

        public void AddVisionBy(Entity e)
        {
            ExploredBy.Add(e.Player);
            SeenBy.Add(e);
        }
        public void RemoveVisionBy(Entity e)
        {
            SeenBy.Remove(e);
        }
        public void AddExploredBy(Player p)
        {
            ExploredBy.Add(p);
        }
        public void RemoveExploredBy(Player p)
        {
            ExploredBy.Remove(p);
        }

        public void SetSurface(SurfaceId id)
        {
            Surface = SurfaceManager.Instance.GetSurface(id);
        }

        #endregion

        #region Getters

        public bool HasWall => Walls.Count > 0;
        public virtual float GetSpeedModifier() => Surface.SpeedModifier;
        public abstract Vector3 GetCenterWorldPosition();

        public int GetMinHeight(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Min(x => x.Value);
        public int GetMaxHeight(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Max(x => x.Value);

        /// <summary>
        /// Returns the relative height (compared to BaseHeight) at the relative position within this node.
        /// </summary>
        private float GetRelativeHeightAt(Vector2 relativePosition)
        {
            if (relativePosition.x < 0 || relativePosition.x > 1 || relativePosition.y < 0 || relativePosition.y > 1) throw new System.Exception("Given position must be relative. It's currently " + relativePosition.x + "/" + relativePosition.y);

            switch (Shape)
            {
                case "0000": return 0;
                case "0011": return relativePosition.y;
                case "1001": return (1f - relativePosition.x);
                case "1100": return (1f - relativePosition.y);
                case "0110": return relativePosition.x;

                case "0001":
                    if (relativePosition.x > relativePosition.y) return 0f;
                    else return relativePosition.y - relativePosition.x;
                case "0010":
                    if (relativePosition.x + relativePosition.y < 1) return 0f;
                    else return relativePosition.y + relativePosition.x - 1f;
                case "0100":
                    if (relativePosition.x < relativePosition.y) return 0f;
                    else return relativePosition.x - relativePosition.y;
                case "1000":
                    if (relativePosition.x + relativePosition.y > 1) return 0f;
                    else return 1f - relativePosition.y + relativePosition.x;

                case "1110":
                    if (relativePosition.x > relativePosition.y) return 1f;
                    else return 1f - (relativePosition.y - relativePosition.x);
                case "1101":
                    if (relativePosition.x + relativePosition.y < 1) return 1f;
                    else return 1f - (relativePosition.y + relativePosition.x - 1f);
                case "1011":
                    if (relativePosition.x < relativePosition.y) return 1f;
                    else return 1f - (relativePosition.x - relativePosition.y);
                case "0111":
                    if (relativePosition.x + relativePosition.y > 1) return 1f;
                    else return relativePosition.y + relativePosition.x;
            }
            //todo
            throw new System.Exception("Case not yet implemented. Shape " + Shape + " relative height should never be used.");
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
        public bool IsVisibleBy(Player player)
        {
            if (World.IsAllVisible) return true; // Everything is visible
            if (SeenBy.FirstOrDefault(x => x.Player == player) != null) return true; // Node is seen by an entity of player

            return false;
        }

        /// <summary>
        /// Returns if the node has been explored by the specified player.
        /// </summary>
        public bool IsExploredBy(Player player)
        {
            if (World.IsAllVisible) return true; // Everything is visible
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
        /// </summary>
        public virtual bool IsPassable(Direction dir, Entity entity = null, bool checkLadder = true)
        {
            // Check if node is generally passable
            if (!IsPassable(entity)) return false;

            // Check if wall blocking this side (including corners)
            if (Walls.ContainsKey(dir)) return false;

            if (dir == Direction.NW) return IsPassable(Direction.N, entity) && IsPassable(Direction.W, entity);
            if (dir == Direction.NE) return IsPassable(Direction.N, entity) && IsPassable(Direction.E, entity);
            if (dir == Direction.SW) return IsPassable(Direction.S, entity) && IsPassable(Direction.W, entity);
            if (dir == Direction.SE) return IsPassable(Direction.S, entity) && IsPassable(Direction.E, entity);

            // Check if ladder is blocking
            if(checkLadder)
                if (Ladders.ContainsKey(dir)) return false;

            // Check if the side has enough head space for the entity
            int headSpace = GetFreeHeadSpace(dir);
            if (headSpace <= 0) return false; // Another node above this one is blocking this(by overlapping in at least 1 corner)
            if (entity != null && entity.Dimensions.y > headSpace) return false; // A node above is blocking the space for the entity

            return true;
        }

        public bool IsFlat() => Height.Values.All(x => x == Height[Direction.SW]);
        public bool IsFlat(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Select(x => x.Value).All(x => x == Height[HelperFunctions.GetAffectedCorners(dir)[0]]);

        public bool IsSlope()
        {
            if (Height[Direction.NW] == Height[Direction.NE] && Height[Direction.SW] == Height[Direction.SE] && Height[Direction.NW] != Height[Direction.SW]) return true;
            if (Height[Direction.NW] == Height[Direction.SW] && Height[Direction.NE] == Height[Direction.SE] && Height[Direction.NW] != Height[Direction.NE]) return true;
            return false;
        }

        /// <summary>
        /// Returns the minimun amount of space (in amount of tiles) that is free above this node.
        /// <br/> For example a flat node right above this flat node would be 1.
        /// <br/> If any corner of an above node overlaps with this node 0 is returned.
        /// <br/> forcedBaseHeight can be passed to check free head space from a specific height instead of node corner.
        /// <br/> Direction.None can be passed to check all corners.
        /// </summary>
        public int GetFreeHeadSpace(Direction dir, int forcedBaseHeight = -1)
        {
            List<BlockmapNode> nodesAbove = World.GetNodes(WorldCoordinates, MaxHeight, World.MAX_HEIGHT).Where(x => x != this && x.IsSolid && !World.DoFullyOverlap(this, x)).ToList();

            int minHeight = World.MAX_HEIGHT;

            foreach (BlockmapNode node in nodesAbove)
            {
                foreach(Direction corner in HelperFunctions.GetAffectedCorners(dir))
                {
                    int diff = node.Height[corner] - Height[corner];
                    if (forcedBaseHeight != -1) diff = node.Height[corner] - forcedBaseHeight;

                    if (diff < minHeight) minHeight = diff;
                }
            }

            return minHeight;
        }

        public override string ToString()
        {
            return Type.ToString() + WorldCoordinates.ToString() + "h:" + BaseHeight;
        }

        #endregion

        #region Save / Load

        public static BlockmapNode Load(World world, Chunk chunk, NodeData data)
        {
            switch(data.Type)
            {
                case NodeType.Surface:
                    return new SurfaceNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), data.Surface);

                case NodeType.AirPath:
                    return new AirNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), data.Surface);

                case NodeType.Water:
                    return new WaterNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), data.Surface);
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
                Height = new int[] { Height[Direction.SW], Height[Direction.SE], Height[Direction.NE], Height[Direction.NW] },
                Surface = Surface.Id,
                Type = Type
            };
        }

        #endregion
    }
}
