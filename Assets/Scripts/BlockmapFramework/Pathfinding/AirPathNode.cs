using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathNode : BlockmapNode
    {
        public override NodeType Type => NodeType.AirPath;
        public override bool IsSolid => true;
        public override bool IsPath => true;

        public AirPathNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceId surface) : base(world, chunk, id, localCoordinates, height, surface) { }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            PathMeshBuilder.BuildPath(this, meshBuilder, pathSubmesh: 0, pathCurbSubmesh: 1);
        }

        #endregion

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight + ((MaxHeight - BaseHeight) * 0.5f)), WorldCoordinates.y + 0.5f);
        }

        #endregion
    }
}
