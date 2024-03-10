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
        public Dictionary<Direction, Wall> Walls = new Dictionary<Direction, Wall>();
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
            Height = height;

            RecalculateShape();
            Transitions = new Dictionary<BlockmapNode, Transition>();
            WalkTransitions = new Dictionary<Direction, Transition>();
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

            Direction preDirection = HelperFunctions.GetNextAnticlockwiseDirection8(dir);
            Direction postDirection = HelperFunctions.GetNextClockwiseDirection8(dir);
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
                    SingleClimbTransition t = new SingleClimbTransition(this, adjNode, dir, climbList);
                    Transitions.Add(adjNode, t);
                }
                else if(ShouldCreateDoubleClimbTransition(adjNode, dir, out List<IClimbable> climpUp, out List<IClimbable> climbDown))
                {
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
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);

            // Calculate some important values
            int fromHeight = GetMinHeight(dir);
            int toHeight = to.GetMaxHeight(oppositeDir);
            int heightDiff = Mathf.Abs(toHeight - fromHeight);
            bool isAscend = toHeight > fromHeight;
            BlockmapNode lowerNode = isAscend ? this : to;
            Direction lowerSide = isAscend ? dir : oppositeDir;
            BlockmapNode higherNode = isAscend ? to : this;
            Direction higherSide = isAscend ? oppositeDir : dir;

            // Make some initial checks
            if (fromHeight == toHeight) return false; // No transition when matching height
            if (!lowerNode.IsPassable(lowerSide, checkClimbables: false)) return false; // Lower node is not passable in the needed direction
            if (!higherNode.IsPassable(higherSide)) return false; // Higher node is not passable in the needed direction

            int headspace = lowerNode.GetFreeHeadSpace(lowerSide);
            if (headspace <= heightDiff) return false; // Another node is blocking the transition
            if (!IsFlat(dir)) return false; // Transition base needs to be flat
            if (!to.IsFlat(oppositeDir)) return false; // Transition target needs to be flat

            // Costruct climb list
            int startHeight = lowerNode.GetMinHeight(lowerSide);
            int endHeight = higherNode.GetMaxHeight(higherSide);

            if(CanConnectUpwardsThroughClimbing(lowerNode, lowerSide, startHeight, higherNode, higherSide, endHeight, out climb))
            {
                return true;
            }

            return false;
        }

        private bool ShouldCreateDoubleClimbTransition(BlockmapNode to, Direction dir, out List<IClimbable> climbUp, out List<IClimbable> climbDown)
        {
            climbUp = new List<IClimbable>();
            climbDown = new List<IClimbable>();

            // Calculate some important values
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            int fromHeight = GetMinHeight(dir);
            int toHeight = to.GetMaxHeight(oppositeDir);
            int minHeight = Mathf.Min(fromHeight, toHeight);

            // Passability checks
            if (!IsPassable(dir, checkClimbables: false)) return false; // Lower node is not passable in the needed direction
            if (!to.IsPassable(oppositeDir, checkClimbables: false)) return false; // Higher node is not passable in the needed direction

            if (!IsFlat(dir)) return false; // Transition base needs to be flat
            if (!to.IsFlat(oppositeDir)) return false; // Transition target needs to be flat

            // Get walls on both sides
            Wall fromWall, toWall;
            Walls.TryGetValue(dir, out fromWall);
            to.Walls.TryGetValue(oppositeDir, out toWall);
            if (fromWall == null && toWall == null) return false; // At least one wall needed

            // Check if walls are climbable
            if (fromWall != null && !fromWall.IsClimbable) return false; // Wall is unclimbable
            if (toWall != null && !toWall.IsClimbable) return false; // Wall is unclimbable

            int maxHeight = Mathf.Max(fromWall != null ? fromWall.MaxHeight : 0, toWall != null ? toWall.MaxHeight : 0);
            int totalClimbHeight = maxHeight - minHeight;
            if(totalClimbHeight > MovingEntity.MAX_ADVANCED_CLIMB_HEIGHT) return false; // Too high

            // Headspeace checks
            int headspaceFrom = GetFreeHeadSpace(dir);
            if (headspaceFrom <= maxHeight - fromHeight) return false; // Another node is blocking on the from-side
            int headspaceTo = to.GetFreeHeadSpace(oppositeDir);
            if (headspaceTo <= maxHeight - toHeight) return false; // Another node is blocking on the to-side
            

            // Case 1: Nodes are at same height
            if (fromHeight == toHeight)
            {
                if (SourceLadders.ContainsKey(dir)) return false; // Ladder blocks since it leads to another node higher up
                if (to.SourceLadders.ContainsKey(oppositeDir)) return false; // Ladder blocks since it leads to another node higher up

                int startHeight = fromHeight;

                for(int i = 0; i < totalClimbHeight; i++)
                {
                    // ClimbUp
                    if (fromWall != null && i <= fromWall.MaxHeight - startHeight) climbUp.Add(fromWall);
                    else climbUp.Add(toWall);

                    // ClimbDown
                    if (toWall != null && i <= toWall.MaxHeight - startHeight) climbDown.Add(toWall);
                    else climbDown.Add(fromWall);
                }
                return true;
            }

            // Case 2: From node is higher
            if(fromHeight > toHeight)
            {
                if (SourceLadders.ContainsKey(dir)) return false; // Ladder blocks since it leads to another node higher up

                // ClimbUp
                if (fromWall == null && toWall.MaxHeight <= fromHeight) return false; // Lower wall must go higher if the higher node has no wall

                int currentHeight = fromHeight;
                if (fromWall != null)
                {
                    int climbHeight = fromWall.MaxHeight - fromHeight;
                    for (int i = 0; i < climbHeight; i++) climbUp.Add(fromWall);
                    currentHeight += climbHeight;
                }

                if(currentHeight < maxHeight)
                    for (int i = 0; i < maxHeight - currentHeight; i++) climbUp.Add(toWall);

                // ClimbDown
                if (!CanConnectUpwardsThroughClimbing(to, oppositeDir, toHeight, this, dir, maxHeight, out climbDown)) return false;

                return true;
            }

            // Case 3: To node is higher
            if (fromHeight < toHeight)
            {
                if (to.SourceLadders.ContainsKey(oppositeDir)) return false; // Ladder blocks since it leads to another node higher up

                // ClimbUp
                if (!CanConnectUpwardsThroughClimbing(this, dir, fromHeight, to, oppositeDir, maxHeight, out climbUp)) return false;

                // ClimbDown
                if (toWall == null && fromWall.MaxHeight <= toHeight) return false; // Lower wall must go higher if the higher node has no wall

                int currentHeight = toHeight;
                if (toWall != null)
                {
                    int climbHeight = toWall.MaxHeight - toHeight;
                    for (int i = 0; i < climbHeight; i++) climbDown.Add(toWall);
                    currentHeight += climbHeight;
                }

                if (currentHeight < maxHeight)
                    for (int i = 0; i < maxHeight - currentHeight; i++) climbDown.Add(fromWall);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a climbing path exists (through ladders, walls, cliffs, etc.) from the given lowerNode to the given higherNode.
        /// </summary>
        private bool CanConnectUpwardsThroughClimbing(BlockmapNode lowerNode, Direction lowerSide, int startHeight, BlockmapNode higherNode, Direction higherSide, int endHeight, out List<IClimbable> climb)
        {
            climb = new List<IClimbable>();
            int currentHeight = startHeight;

            // Step 1: First check if lower node has a wall
            if (lowerNode.Walls.TryGetValue(lowerSide, out Wall lowerWall))
            {
                int climbHeight = lowerWall.MaxHeight - startHeight;

                if (!lowerWall.IsClimbable) return false; // Wall is unclimbable
                if (lowerWall.MaxHeight > endHeight) return false; // Wall goes higher than the height we want
                if (climbHeight > lowerWall.MaxClimbHeight(ClimbingCategory.Advanced)) return false; // Wall is too high to climb

                // Add the wall as the first part of the climb
                for (int i = 0; i < climbHeight; i++) climb.Add(lowerWall);
                currentHeight += climbHeight;

                // Check if we reached the target height
                if (currentHeight == endHeight) return true;
            }

            // Step 2: If no wall, check if lower node has a ladder
            else if (lowerNode.SourceLadders.TryGetValue(lowerSide, out Ladder ladder))
            {
                int climbHeight = ladder.MaxHeight - startHeight;

                if (ladder.MaxHeight > endHeight) return false; // Ladder goes higher than the height we want
                if (climbHeight > ladder.MaxClimbHeight(ClimbingCategory.Advanced)) return false; // Ladder is too high to climb

                // Add the ladder as the first part of the climb
                for (int i = 0; i < climbHeight; i++) climb.Add(ladder);
                currentHeight += climbHeight;

                // Check if we reached the target height
                if (currentHeight == endHeight) return true;
            }

            // Step 3: Check the surface node of the coordinates we are climbing to. If its higher than we currently are in our climb, add it to our climb.
            BlockmapNode toSurfaceNode = World.GetSurfaceNode(higherNode.WorldCoordinates);
            if (toSurfaceNode == higherNode && toSurfaceNode.GetMinHeight(higherSide) == endHeight) // The surface node is our target
            {
                int climbHeight = endHeight - currentHeight;
                if (climbHeight > Cliff.Instance.MaxClimbHeight(ClimbingCategory.Advanced)) return false; // Cliff is too high to climb

                // Complete the climb list
                for (int i = 0; i < climbHeight; i++) climb.Add(Cliff.Instance);
                return true;
            }
            else if (toSurfaceNode.GetMinHeight(higherSide) > currentHeight) // Surface node is not our target, but higher than we currently are
            {
                int climbHeight = toSurfaceNode.GetMinHeight(higherSide) - currentHeight;

                if (!toSurfaceNode.IsFlat(higherSide)) return false; // Can't climb up to a sloped cliff
                if (climbHeight > Cliff.Instance.MaxClimbHeight(ClimbingCategory.Advanced)) return false; // Cliff is too high to climb

                // Add the cliff to the climb list
                for (int i = 0; i < climbHeight; i++) climb.Add(Cliff.Instance);
                currentHeight += climbHeight;
            }

            // Step 4: Check if we can complete the climb with walls below higherNode
            List<BlockmapNode> nodesBelowHigher = World.GetNodes(higherNode.WorldCoordinates).OrderBy(x => x.BaseHeight).ToList();
            foreach (BlockmapNode nodeBelow in nodesBelowHigher)
            {
                if (nodeBelow.Walls.TryGetValue(higherSide, out Wall higherWall))
                {
                    int climbHeight = higherWall.MaxHeight - currentHeight;

                    if (higherWall.MaxHeight <= currentHeight) continue; // Wall is too low, not interested

                    if (!higherWall.IsClimbable) return false;
                    if (higherWall.MinHeight > currentHeight) return false; // Wall is too high up
                    if (higherWall.MaxHeight > endHeight) return false; // Wall goes higher than the height we want
                    if (climbHeight > higherWall.MaxClimbHeight(ClimbingCategory.Advanced)) return false; // Wall is too high to climb

                    // Add the wall as the first part of the climb
                    for (int i = 0; i < climbHeight; i++) climb.Add(higherWall);
                    currentHeight += climbHeight;

                    // Check if we reached the target height
                    if (currentHeight == endHeight) return true;
                }
            }

            return false;
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

        public bool HasWall => Walls.Count > 0;
        public abstract Surface GetSurface();
        public abstract SurfaceProperties GetSurfaceProperties();
        public abstract Vector3 GetCenterWorldPosition();

        public int GetMinHeight(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Min(x => x.Value);
        public int GetMaxHeight(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Max(x => x.Value);

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
        /// Checks and returns if an adjacent node in the given direction with a seamless connection has an entity of the given type.
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
                    else return -(relativePosition.x + relativePosition.y - 1f);

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

                case "1010":
                    if(UseAlternativeVariant)
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
                    if (UseAlternativeVariant)
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
        /// </summary>
        public bool IsPassable(Direction dir, Entity entity = null, bool checkClimbables = true)
        {
            // Check if node is generally passable
            if (!IsPassable(entity)) return false;

            // Special checks for corner directions
            if(HelperFunctions.GetCorners().Contains(dir))
            {
                if (Walls.ContainsKey(dir)) return false;
                if (!HelperFunctions.GetAffectedSides(dir).All(x => IsPassable(x, entity))) return false;

                return true;
            }

            // Check if ladder is blocking
            if (checkClimbables)
            {
                if (SourceLadders.ContainsKey(dir)) return false;
                if (Walls.ContainsKey(dir)) return false;
            }

            // Check if the side has enough head space for the entity
            int headSpace = GetFreeHeadSpace(dir);
            if (headSpace <= 0) return false; // Another node above this one is blocking this(by overlapping in at least 1 corner)
            if (entity != null && entity.Height > headSpace) return false; // A node above is blocking the space for the entity

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
                    return new SurfaceNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height), SurfaceManager.Instance.GetSurface((SurfaceId)data.SubType));

                case NodeType.Air:
                    return new AirNode(world, chunk, data.Id, new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY), LoadHeight(data.Height));

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
                Height = new int[] { Height[Direction.SW], Height[Direction.SE], Height[Direction.NE], Height[Direction.NW] },
                Type = Type,
                SubType = GetSubType()
            };
        }

        public abstract int GetSubType();

        #endregion
    }
}
