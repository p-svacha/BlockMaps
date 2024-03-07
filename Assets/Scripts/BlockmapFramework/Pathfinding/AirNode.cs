using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirNode : BlockmapNode
    {
        public SurfaceProperties SurfaceProperties { get; private set; }

        public override NodeType Type => NodeType.Air;
        public override bool IsSolid => true;
        public override bool IsPath => true;

        public AirNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height) : base(world, chunk, id, localCoordinates, height)
        {
            SurfaceProperties = SurfaceManager.Instance.GetSurfaceProperties(SurfacePropertyId.Tarmac);
        }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            PathMeshBuilder.BuildPath(this, meshBuilder);
        }

        #endregion

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight + ((MaxHeight - BaseHeight) * 0.5f)), WorldCoordinates.y + 0.5f);
        }

        public override SurfaceProperties GetSurfaceProperties() => SurfaceProperties;

        public override int GetSubType() => 0;

        #endregion
    }
}
