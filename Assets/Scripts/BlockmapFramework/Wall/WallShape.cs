using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Each possible wall shape has exactly one instance of this class, which is handled by the singleton WallManager.
    /// <br/>WallShape defines in what directions a wall can be placed and how its mesh is generated.
    /// </summary>
    public abstract class WallShape
    {
        public abstract WallShapeId Id { get; }
        public abstract string Name { get; }
        public abstract bool BlocksVision { get; }
        public abstract bool IsClimbable { get; }
        public abstract float Width { get; }
        public virtual List<Direction> ValidSides => HelperFunctions.GetSides();
        public Sprite PreviewSprite => Resources.Load<Sprite>("Editor/Thumbnails/WallShapes/" + Id.ToString());

        public abstract void GenerateMesh(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored);
    }
}
