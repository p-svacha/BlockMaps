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
        // public abstract List<Direction> ValidSides { get; }
        public Sprite PreviewSprite => Resources.Load<Sprite>("Editor/Thumbnails/WallShapes/" + Id.ToString());
        public abstract float Width { get; }

        public abstract void GenerateMesh(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, Material material);
    }
}
