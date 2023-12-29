using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathNode : BlockmapNode
    {
        public override int Layer => World.Layer_AirNode;
        public override bool IsPath => true;

        public AirPathNode(World world, Chunk chunk, NodeData data) : base(world, chunk, data) { }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            PathMeshBuilder.BuildPath(this, meshBuilder, pathSubmesh: 0, pathCurbSubmesh: 1);
        }

        #endregion

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight), WorldCoordinates.y + 0.5f);
        }

        #endregion
    }
}
