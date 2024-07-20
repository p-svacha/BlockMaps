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
    public class Wall
    {
        /// <summary>
        /// Unique identifier of this specific wall.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The world cell coordiantes of the wall.
        /// </summary>
        public Vector3Int CellCoordinates { get; private set; }

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

        #region Init

        public Wall(int id, Vector3Int cellCoordinates, Direction side, WallShape shape, WallMaterial material)
        {
            Id = id;
            CellCoordinates = cellCoordinates;
            Side = side;
            Shape = shape;
            Material = material;
        }

        #endregion

        #region Save / Load

        public static Wall Load(WallData data)
        {
            Wall wall = new Wall(data.Id, new Vector3Int(data.CellX, data.CellY, data.CellZ), (Direction)data.Side, WallManager.Instance.GetWallShape((WallShapeId)data.ShapeId), WallManager.Instance.GetWallMaterial((WallMaterialId)data.MaterialId));
            return wall;
        }

        public WallData Save()
        {
            return new WallData
            {
                Id = Id,
                CellX = CellCoordinates.x,
                CellY = CellCoordinates.y,
                CellZ = CellCoordinates.z,
                Side = (int)Side,
                ShapeId = (int)Shape.Id,
                MaterialId = (int)Material.Id
            };
        }

        #endregion
    }
}
