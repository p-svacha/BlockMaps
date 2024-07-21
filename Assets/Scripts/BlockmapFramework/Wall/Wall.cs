using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Fences are a kind of barrier that are stored on the cell grid seperate of nodes.
    /// <br/>An instance represents one wall on a single world cell covering a specific direction (side, corner or full cell).
    /// <br/>Each wall consist of a combination of a WallShape and a WallMaterial, which together define how the wall looks and acts.
    /// </summary>
    public class Wall : IClimbable
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
        public Vector3 CenterWorldPosition => new Vector3(GlobalCellCoordinates.x + 0.5f, GlobalCellCoordinates.y + (World.TILE_HEIGHT / 2), GlobalCellCoordinates.z + 0.5f);

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


        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => Material.ClimbSkillRequirement;
        public float ClimbCostUp => Material.ClimbCostUp;
        public float ClimbCostDown => Material.ClimbCostDown;
        public float ClimbSpeedUp => Material.ClimbSpeedUp;
        public float ClimbSpeedDown => Material.ClimbSpeedDown;
        public float ClimbTransformOffset => Shape.Width;
        public Direction ClimbSide => Side;
        public bool IsClimbable => ClimbSkillRequirement != ClimbingCategory.Unclimbable;

        #region Init

        public Wall(World world, int id, Vector3Int globalCellCoordinates, Direction side, WallShape shape, WallMaterial material)
        {
            World = world;
            Id = id;
            GlobalCellCoordinates = globalCellCoordinates;
            Side = side;
            Shape = shape;
            Material = material;
        }

        #endregion

        #region Getters

        public override string ToString()
        {
            return GlobalCellCoordinates.ToString() + " " + Side.ToString() + " " + Shape.Name + " " + Material.Name;
        }

        #endregion

        #region Save / Load

        public static Wall Load(World world, WallData data)
        {
            Wall wall = new Wall(world, data.Id, new Vector3Int(data.CellX, data.CellY, data.CellZ), (Direction)data.Side, WallManager.Instance.GetWallShape((WallShapeId)data.ShapeId), WallManager.Instance.GetWallMaterial((WallMaterialId)data.MaterialId));
            return wall;
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
                MaterialId = (int)Material.Id
            };
        }

        #endregion
    }
}
