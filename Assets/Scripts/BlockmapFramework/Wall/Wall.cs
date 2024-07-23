using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Fences are a kind of barrier that are stored on the cell grid seperate of nodes.
    /// <br/>An instance represents one wall on a single world cell covering a specific direction (side, corner or full cell).
    /// <br/>Each wall consist of a combination of a WallShape and a WallMaterial, which together define how the wall looks and acts.
    /// </summary>
    public class Wall : IClimbable, IVisionTarget
    {
        private World World;

        /// <summary>
        /// Unique identifier of this specific wall.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The world cell coordiantes of the wall.
        /// </summary>
        public Vector3Int GlobalCellCoordinates { get; private set; }
        public Vector3Int LocalCellCoordinates => World.GetLocalCellCoordinates(GlobalCellCoordinates);
        public Vector2Int WorldCoordinates => new Vector2Int(GlobalCellCoordinates.x, GlobalCellCoordinates.z);
        public Chunk Chunk => World.GetChunk(WorldCoordinates);
        public Vector3 CellCenterWorldPosition => new Vector3(GlobalCellCoordinates.x + 0.5f, (GlobalCellCoordinates.y * World.TILE_HEIGHT) + (World.TILE_HEIGHT / 2), GlobalCellCoordinates.z + 0.5f);

        /// <summary>
        /// The side within the cell this wall covers.
        /// </summary>
        public Direction Side { get; private set; }

        /// <summary>
        /// The shape of this wall (i.e. solid, window).
        /// </summary>
        public WallShape Shape { get; private set; }

        /// <summary>
        /// The material this wall is made of.
        /// </summary>
        public WallMaterial Material { get; private set; }

        /// <summary>
        /// Flag if this is the mirrored version of the wall piece.
        /// </summary>
        public bool IsMirrored { get; private set; }

        public float Width => Shape.Width;


        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => Material.ClimbSkillRequirement;
        public float ClimbCostUp => Material.ClimbCostUp;
        public float ClimbCostDown => Material.ClimbCostDown;
        public float ClimbSpeedUp => Material.ClimbSpeedUp;
        public float ClimbSpeedDown => Material.ClimbSpeedDown;
        public float ClimbTransformOffset => Shape.Width;
        public Direction ClimbSide => Side;
        public bool IsClimbable => Shape.IsClimbable && ClimbSkillRequirement != ClimbingCategory.Unclimbable;

        #region Init

        public Wall(World world, int id, Vector3Int globalCellCoordinates, Direction side, WallShape shape, WallMaterial material, bool mirrored)
        {
            World = world;
            Id = id;
            GlobalCellCoordinates = globalCellCoordinates;
            Side = side;
            Shape = shape;
            Material = material;
            IsMirrored = mirrored;

            if (!Shape.ValidSides.Contains(side)) throw new System.Exception("Invalid wall side error. " + shape.Name + " is not allowed to build in the direction " + side.ToString());
        }

        #endregion

        #region Vision Target

        private HashSet<Actor> ExploredBy = new HashSet<Actor>();
        private HashSet<Entity> SeenBy = new HashSet<Entity>();

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

        public bool IsVisibleBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            if (SeenBy.FirstOrDefault(x => x.Owner == actor) != null) return true; // Wall is seen by an entity of given actor

            return false;
        }
        public bool IsExploredBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            return ExploredBy.Contains(actor);
        }

        #endregion

        #region Getters

        public Vector3 GetCenterWorldPosition()
        {
            Vector3 cellCenter = CellCenterWorldPosition;

            Vector3 directionOffset = Side switch
            {
                Direction.N => new Vector3(0f, 0f, 0.5f - (Width/2)),
                Direction.E => new Vector3(0.5f - (Width / 2), 0f, 0f),
                Direction.S => new Vector3(0f, 0f, -(0.5f - (Width / 2))),
                Direction.W => new Vector3(-(0.5f - (Width / 2)), 0f, 0f),
                Direction.NW => new Vector3(-(0.5f - (Width / 2)), 0f, 0.5f - (Width / 2)),
                Direction.NE => new Vector3(0.5f - (Width / 2), 0f, 0.5f - (Width / 2)),
                Direction.SW => new Vector3(-(0.5f - (Width / 2)), 0f, -(0.5f - (Width / 2))),
                Direction.SE => new Vector3(0.5f - (Width / 2), 0f, -(0.5f - (Width / 2))),
                _ => throw new System.Exception("Direction not handled")
            };

            return cellCenter + directionOffset;
        }

        public override string ToString()
        {
            return GlobalCellCoordinates.ToString() + " " + Side.ToString() + " " + Shape.Name + " " + Material.Name;
        }

        public bool BlocksVision => Shape.BlocksVision;

        #endregion

        #region Save / Load

        public static Wall Load(World world, WallData data)
        {
            return new Wall
                (
                world, 
                data.Id, 
                new Vector3Int(data.CellX, data.CellY, data.CellZ), 
                (Direction)data.Side, 
                WallManager.Instance.GetWallShape((WallShapeId)data.ShapeId), 
                WallManager.Instance.GetWallMaterial((WallMaterialId)data.MaterialId), 
                data.IsMirrored
                );
        }

        public WallData Save()
        {
            return new WallData
            {
                Id = Id,
                CellX = GlobalCellCoordinates.x,
                CellY = GlobalCellCoordinates.y,
                CellZ = GlobalCellCoordinates.z,
                Side = (int)Side,
                ShapeId = (int)Shape.Id,
                MaterialId = (int)Material.Id,
                IsMirrored = IsMirrored
            };
        }

        #endregion
    }
}
