using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirNode : BlockmapNode
    {
        public Surface Surface { get; private set; }

        public override NodeType Type => NodeType.Air;
        public override bool IsSolid => true;

        public AirNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height) : base(world, chunk, id, localCoordinates, height)
        {
            Surface = SurfaceManager.Instance.GetSurface(SurfaceId.Concrete);
        }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.BuildBorderedNodeSurface(World, this, meshBuilder, ResourceManager.Singleton.Mat_ConcreteDark, ResourceManager.Singleton.Mat_Concrete2, 0.1f, 0.1f, 0.1f);
        }

        #endregion

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight + ((MaxHeight - BaseHeight) * 0.5f)), WorldCoordinates.y + 0.5f);
        }

        public override Surface GetSurface() => Surface;
        public override SurfaceProperties GetSurfaceProperties() => Surface.Properties;

        public override int GetSubType() => 0;

        #endregion
    }
}
