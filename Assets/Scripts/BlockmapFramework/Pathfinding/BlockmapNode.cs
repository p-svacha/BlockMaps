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
        public Vector2Int LocalCoordinates { get; private set; }
        public Surface Surface { get; private set; }
        public abstract NodeType Type { get; }
        public abstract bool IsPath { get; }
        public abstract bool IsSolid { get; } // Flag if entities can (generally) be placed on top of this node

        // Connections
        public Dictionary<Direction, BlockmapNode> ConnectedNodes;

        public HashSet<Entity> Entities = new HashSet<Entity>();
        public Dictionary<Direction, Wall> Walls = new Dictionary<Direction, Wall>();

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
            Surface = SurfaceManager.Instance.GetSurface(surface);
            ConnectedNodes = new Dictionary<Direction, BlockmapNode>();
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

        #region Connections

        /// <summary>
        /// Updates the straight neighbours by applying the general rule:
        /// If there is an adjacent passable node in the direction with matching heights, connect it as a neighbour.
        /// </summary>
        public void UpdateConnectedNodesStraight()
        {
            ConnectedNodes.Clear();

            UpdateConnectedNodesStraight(Direction.N);
            UpdateConnectedNodesStraight(Direction.E);
            UpdateConnectedNodesStraight(Direction.S);
            UpdateConnectedNodesStraight(Direction.W);
        }

        /// <summary>
        /// Searches if there is a node in the given straight direction that matches the corner heights of this node.
        /// <br/> If so, it is added as a connection.
        /// </summary>
        private void UpdateConnectedNodesStraight(Direction dir)
        {
            if (!IsPassable(dir)) return;

            List<BlockmapNode> adjNodes = World.GetAdjacentNodes(WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (!adjNode.IsPassable(HelperFunctions.GetOppositeDirection(dir))) continue;

                if(ShouldConnectToNode(adjNode, dir))
                {
                    // Surface node connections can be override by air nodes built on a surface. In that case we remove the surface connection first
                    if (ConnectedNodes.ContainsKey(dir) && ConnectedNodes[dir].Type == NodeType.Surface && adjNode.Type == NodeType.AirPath)
                        ConnectedNodes.Remove(dir);

                    // Connect node as a neighbour
                    ConnectedNodes.Add(dir, adjNode);
                }
            }
        }

        /// <summary>
        /// Returns if this node should be connected to the given node (that is adjacent in the given direction)
        /// </summary>
        protected virtual bool ShouldConnectToNode(BlockmapNode adjNode, Direction dir)
        {
            return World.DoAdjacentHeightsMatch(this, adjNode, dir);
        }

        /// <summary>
        /// Updates diagonal neighbours by applying the genereal rule:
        /// If the path N>E results in the same node as E>N, then connect NE to that node
        /// </summary>
        public void UpdateConnectedNodesDiagonal()
        {
            BlockmapNode northNode = ConnectedNodes.ContainsKey(Direction.N) ? ConnectedNodes[Direction.N] : null;
            BlockmapNode eastNode = ConnectedNodes.ContainsKey(Direction.E) ? ConnectedNodes[Direction.E] : null;
            BlockmapNode southNode = ConnectedNodes.ContainsKey(Direction.S) ? ConnectedNodes[Direction.S] : null;
            BlockmapNode westNode = ConnectedNodes.ContainsKey(Direction.W) ? ConnectedNodes[Direction.W] : null;

            // NE
            if (northNode != null && eastNode != null &&
                northNode.ConnectedNodes.ContainsKey(Direction.E) && eastNode.ConnectedNodes.ContainsKey(Direction.N) &&
                northNode.ConnectedNodes[Direction.E] == eastNode.ConnectedNodes[Direction.N] &&
                northNode.ConnectedNodes[Direction.E].IsPassable(Direction.S) &&
                northNode.ConnectedNodes[Direction.E].IsPassable(Direction.W)) 
                ConnectedNodes[Direction.NE] = northNode.ConnectedNodes[Direction.E];

            // NW
            if (northNode != null && westNode != null &&
                northNode.ConnectedNodes.ContainsKey(Direction.W) && westNode.ConnectedNodes.ContainsKey(Direction.N) &&
                northNode.ConnectedNodes[Direction.W] == westNode.ConnectedNodes[Direction.N] &&
                northNode.ConnectedNodes[Direction.W].IsPassable(Direction.S) && 
                northNode.ConnectedNodes[Direction.W].IsPassable(Direction.E)) 
                ConnectedNodes[Direction.NW] = northNode.ConnectedNodes[Direction.W];

            // SE
            if (southNode != null && eastNode != null &&
                southNode.ConnectedNodes.ContainsKey(Direction.E) && eastNode.ConnectedNodes.ContainsKey(Direction.S) &&
                southNode.ConnectedNodes[Direction.E] == eastNode.ConnectedNodes[Direction.S] &&
                southNode.ConnectedNodes[Direction.E].IsPassable(Direction.N) &&
                southNode.ConnectedNodes[Direction.E].IsPassable(Direction.W))
                ConnectedNodes[Direction.SE] = southNode.ConnectedNodes[Direction.E];

            // SW
            if (southNode != null && westNode != null &&
                southNode.ConnectedNodes.ContainsKey(Direction.W) && westNode.ConnectedNodes.ContainsKey(Direction.S) &&
                southNode.ConnectedNodes[Direction.W] == westNode.ConnectedNodes[Direction.S] &&
                southNode.ConnectedNodes[Direction.W].IsPassable(Direction.N) &&
                southNode.ConnectedNodes[Direction.W].IsPassable(Direction.E))
                ConnectedNodes[Direction.SW] = southNode.ConnectedNodes[Direction.W];
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
        /// </summary>
        public void ShowOverlay(Texture2D texture, Color color)
        {
            Mesh.ShowOverlay(LocalCoordinates, texture, color);
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

        public void SetSurface(SurfaceId id)
        {
            Surface = SurfaceManager.Instance.GetSurface(id);
        }

        #endregion

        #region Getters

        public bool HasWall => Walls.Count > 0;
        public virtual float GetSpeedModifier() => Surface.SpeedModifier;
        public abstract Vector3 GetCenterWorldPosition();

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
        public virtual bool IsPassable(Direction dir, Entity entity = null)
        {
            // Check if node is generally passable
            if (!IsPassable(entity)) return false;

            if (dir == Direction.NW) return IsPassable(Direction.N, entity) && IsPassable(Direction.W, entity);
            if (dir == Direction.NE) return IsPassable(Direction.N, entity) && IsPassable(Direction.E, entity);
            if (dir == Direction.SW) return IsPassable(Direction.S, entity) && IsPassable(Direction.W, entity);
            if (dir == Direction.SE) return IsPassable(Direction.S, entity) && IsPassable(Direction.E, entity);

            // Check if wall blocking this side
            if (Walls.ContainsKey(dir)) return false;

            // Check if the side has enough head space for the entity
            int headSpace = GetFreeHeadSpace(dir);
            if (headSpace <= 0) return false; // Another node above this one is blocking this(by overlapping in at least 1 corner)
            if (entity != null && entity.Dimensions.y > headSpace) return false; // A node above is blocking the space for the entity

            return true;
        }

        public bool IsFlat => Height.Values.All(x => x == Height[Direction.SW]);
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
            List<BlockmapNode> nodesAbove = World.GetNodes(WorldCoordinates, MaxHeight, World.MAX_HEIGHT).Where(x => x != this && x.IsSolid).ToList();

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

        /// <summary>
        /// Returns the minimum y coordinate of the affected corners of the given direction.
        /// </summary>
        public int GetMinHeight(Direction dir) => Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Min(x => x.Value);

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
