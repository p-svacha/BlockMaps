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
        private static int IdCounter = 0;
        public int Id;

        /// <summary>
        /// Height of the 4 corners of the node: {SW, SE, NE, NW}
        /// </summary>
        public int[] Height { get; protected set; }

        /// <summary>
        /// Height at which this node is starting at. (Its lowest point)
        /// </summary>
        public int BaseHeight { get; private set; }

        public float BaseWorldHeight => BaseHeight * World.TILE_HEIGHT;

        /// <summary>
        /// Shape is saved in a string with 4 chars, where each char is a corner (SW, SE, NE, NW) storing the height above the min height of the node.
        /// <br/> For example "1001" is a west-facing up-slope.
        /// </summary>
        public string Shape { get; protected set; }

        /// <summary>
        /// To what layer this node belongs to in the world.
        /// </summary>
        public abstract int Layer { get; }

        /// <summary>
        /// Shapes with the format "1010" or "0101" have to possible variants (center high or center low). This flag decides which variant is used in that case.
        /// </summary>
        public bool UseAlternativeVariant;

        // Indices for shape
        public const int SW = 0;
        public const int SE = 1;
        public const int NE = 2;
        public const int NW = 3;

        // Node attributes
        public World World { get; private set; }
        public Chunk Chunk { get; private set; }
        public Vector2Int WorldCoordinates { get; private set; }
        public Vector2Int LocalCoordinates { get; private set; }
        public Surface Surface { get; private set; }
        public NodeType Type { get; protected set; }

        // Connections
        public Dictionary<Direction, BlockmapNode> ConnectedNodes;

        // Objects on node
        public HashSet<Entity> Entities = new HashSet<Entity>();

        #region Initialize

        protected BlockmapNode(World world, Chunk chunk, NodeData data)
        {
            World = world;
            Chunk = chunk;
            Id = data.Id;
            LocalCoordinates = new Vector2Int(data.LocalCoordinateX, data.LocalCoordinateY);
            WorldCoordinates = chunk.GetWorldCoordinates(LocalCoordinates);

            Height = data.Height;
            RecalculateShape();

            Surface = SurfaceManager.Instance.GetSurface(data.Surface);
            Type = data.Type;
            ConnectedNodes = new Dictionary<Direction, BlockmapNode>();
        }

        /// <summary>
        /// Calculates the base height, relative heights and shape according th this nodes heights.
        /// </summary>
        protected void RecalculateShape()
        {
            BaseHeight = Height.Min();
            Shape = GetShape(Height);
        }

        protected string GetShape(int[] height)
        {
            int baseHeight = height.Min();
            string binaryShape = "";
            for (int i = 0; i < 4; i++) binaryShape += (height[i] - baseHeight);
            return binaryShape;
        }

        #endregion

        #region Connections

        public abstract void UpdateConnectedNodesStraight();

        /// <summary>
        /// Updates diagonal neighbours by applying the rule:
        /// If the path N>E results in the same node as E>N, then connect NE to that node
        /// </summary>
        public void UpdateConnectedNodesDiagonal()
        {
            BlockmapNode northNode = ConnectedNodes.ContainsKey(Direction.N) ? ConnectedNodes[Direction.N] : null;
            BlockmapNode eastNode = ConnectedNodes.ContainsKey(Direction.E) ? ConnectedNodes[Direction.E] : null;
            BlockmapNode southNode = ConnectedNodes.ContainsKey(Direction.S) ? ConnectedNodes[Direction.S] : null;
            BlockmapNode westNode = ConnectedNodes.ContainsKey(Direction.W) ? ConnectedNodes[Direction.W] : null;

            if (northNode != null && eastNode != null &&
                northNode.ConnectedNodes.ContainsKey(Direction.E) && eastNode.ConnectedNodes.ContainsKey(Direction.N) &&
                northNode.ConnectedNodes[Direction.E] == eastNode.ConnectedNodes[Direction.N]) ConnectedNodes[Direction.NE] = northNode.ConnectedNodes[Direction.E];

            if (northNode != null && westNode != null &&
                northNode.ConnectedNodes.ContainsKey(Direction.W) && westNode.ConnectedNodes.ContainsKey(Direction.N) &&
                northNode.ConnectedNodes[Direction.W] == westNode.ConnectedNodes[Direction.N]) ConnectedNodes[Direction.NW] = northNode.ConnectedNodes[Direction.W];

            if (southNode != null && eastNode != null &&
                southNode.ConnectedNodes.ContainsKey(Direction.E) && eastNode.ConnectedNodes.ContainsKey(Direction.S) &&
                southNode.ConnectedNodes[Direction.E] == eastNode.ConnectedNodes[Direction.S]) ConnectedNodes[Direction.SE] = southNode.ConnectedNodes[Direction.E];

            if (southNode != null && westNode != null &&
                southNode.ConnectedNodes.ContainsKey(Direction.W) && westNode.ConnectedNodes.ContainsKey(Direction.S) &&
                southNode.ConnectedNodes[Direction.W] == westNode.ConnectedNodes[Direction.S]) ConnectedNodes[Direction.SW] = southNode.ConnectedNodes[Direction.W];
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adds all of this nodes vertices and triangles to the given MeshBuilder.
        /// </summary>
        public abstract void Draw(MeshBuilder meshBuilder);

        #endregion

        #region Entities

        public void AddEntity(Entity e)
        {
            Entities.Add(e);
        }

        public void RemoveEntity(Entity e)
        {
            Entities.Remove(e);
        }

        #endregion

        #region Getters

        public virtual float GetSpeedModifier() => Surface.SpeedModifier;
        public abstract Vector3 GetCenterWorldPosition();
        public abstract bool IsPath { get; }

        /// <summary>
        /// Returns the relative height (compared to BaseHeight) at the relative position within this node.
        /// </summary>
        public float GetRelativeHeightAt(Vector2 relativePosition)
        {
            if (relativePosition.x < 0 || relativePosition.x > 1 || relativePosition.y < 0 || relativePosition.y > 1) throw new System.Exception("Given position must be relative. It's currently " + relativePosition.x + "/" + relativePosition.y);

            switch (Shape)
            {
                case "0000": return 0;
                case "0011": return relativePosition.y * World.TILE_HEIGHT;
                case "1001": return (1f - relativePosition.x) * World.TILE_HEIGHT;
                case "1100": return (1f - relativePosition.y) * World.TILE_HEIGHT;
                case "0110": return relativePosition.x * World.TILE_HEIGHT;

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

            throw new System.Exception("Case not yet implemented. Shape " + Shape + " relative height should never be used.");
        }

        public override string ToString()
        {
            return Type.ToString() + " " + WorldCoordinates.ToString();
        }

        #endregion

        #region Setters

        public void SetSurface(SurfaceId id)
        {
            Surface = SurfaceManager.Instance.GetSurface(id);
        }

        #endregion

        #region Save / Load

        public static BlockmapNode Load(World world, Chunk chunk, NodeData data)
        {
            switch(data.Type)
            {
                case NodeType.Surface:
                    return new SurfaceNode(world, chunk, data);

                case NodeType.AirPath:
                    return new AirPathNode(world, chunk, data);

                case NodeType.AirPathSlope:
                    return new AirPathSlopeNode(world, chunk, data);
            }
            throw new System.Exception("Type " + data.Type.ToString() + " not handled.");
        }

        public NodeData Save()
        {
            return new NodeData
            {
                Id = Id,
                LocalCoordinateX = LocalCoordinates.x,
                LocalCoordinateY = LocalCoordinates.y,
                Height = Height,
                Surface = Surface.Id,
                Type = Type
            };
        }

        #endregion
    }
}
