using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WaterNode : BlockmapNode
    {
        public override NodeType Type => NodeType.Water;
        public override bool IsSolid => false;
        public override bool IsPath => false;

        public WaterBody WaterBody { get; private set; }
        public SurfaceNode SurfaceNode { get; private set; }

        public WaterNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height) : base(world, chunk, id, localCoordinates, height) { }

        public void Init(WaterBody waterBody, SurfaceNode surfaceNode)
        {
            WaterBody = waterBody;
            SurfaceNode = surfaceNode;
            SurfaceNode.SetWaterNode(this);
        }

        protected override bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            // Always connect to diagonal shore
            if(adjNode is SurfaceNode surfaceNode)
            {
                if(surfaceNode.WaterNode != null && !surfaceNode.IsCenterUnderWater) return true;
            }

            return base.ShouldConnectToNodeDirectly(adjNode, dir);
        }

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, WaterBody.WaterSurfaceWorldHeight, WorldCoordinates.y + 0.5f);
        }

        public override bool IsPassable(Entity entity = null)
        {
            if (!SurfaceNode.IsCenterUnderWater) return false; // Surface node on same spot is passable
            if (entity != null && entity is MovingEntity e && !e.CanSwim) return false; // Moving entities can only be on water when they can swim
            
            return base.IsPassable(entity);
        }

        public override SurfaceProperties GetSurfaceProperties() => SurfaceManager.Instance.GetSurfaceProperties(SurfacePropertyId.Water);

        public override int GetSubType() => 0;

        public override void Draw(MeshBuilder meshBuilder)
        {
            throw new System.NotImplementedException(); // drawn as part of a whole waterbody
        }

        #endregion
    }
}
