using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Water nodes make up navigable water bodies.
    /// </summary>
    public class WaterNode : BlockmapNode
    {
        public override NodeType Type => NodeType.Water;
        public override bool SupportsEntities => false;

        public WaterBody WaterBody { get; private set; }
        public GroundNode GroundNode { get; private set; }

        public WaterNode() { }
        public WaterNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, int altitude) : base(world, chunk, id, localCoordinates, HelperFunctions.GetFlatHeights(altitude), SurfaceDefOf.Water) { }

        public void Init(WaterBody waterBody, GroundNode surfaceNode)
        {
            WaterBody = waterBody;
            GroundNode = surfaceNode;
            GroundNode.SetWaterNode(this);
        }

        protected override bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            // Always connect to diagonal shore
            if(adjNode is GroundNode surfaceNode)
            {
                if(surfaceNode.WaterNode != null && !surfaceNode.IsCenterUnderWater) return true;
            }

            return base.ShouldConnectToNodeDirectly(adjNode, dir);
        }

        #region Getters

        protected override Vector3 GetMeshCenter()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, WaterBody.WaterSurfaceWorldHeight, WorldCoordinates.y + 0.5f);
        }

        public override bool IsImpassable()
        {
            if (!GroundNode.IsCenterUnderWater) return true; // Surface node on same spot is passable
            return base.IsImpassable();
        }
        protected override bool CanEntityStandHere(Entity entity)
        {
            if (entity == null) return false;
            if (entity is MovingEntity mEntity && !mEntity.CanSwim) return false;
            
            return base.CanEntityStandHere(entity);
        }

        #endregion
    }
}
