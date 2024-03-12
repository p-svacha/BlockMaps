using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A DynamicNode is a kind of solid node that has a changeable surface and height.
    /// <br/> DynamicNodes include GroundNodes and AirNodes.
    /// </summary>
    public abstract class DynamicNode : BlockmapNode
    {
        public Surface Surface { get; private set; }

        public DynamicNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceId surfaceId) : base(world, chunk, id, localCoordinates, height)
        {
            Surface = SurfaceManager.Instance.GetSurface(surfaceId);
        }

        #region Actions

        public void SetSurface(SurfaceId id)
        {
            Surface = SurfaceManager.Instance.GetSurface(id);
        }

        public virtual bool CanChangeHeight(Direction mode, bool isIncrease)
        {
            if (Entities.Count > 0) return false;

            // Walls
            foreach (Direction wallDir in Walls.Keys)
                if (HelperFunctions.DoAffectedCornersOverlap(mode, wallDir))
                    return false;

            // Ladders
            foreach (Direction ladderDir in TargetLadders.Keys)
                if (HelperFunctions.DoAffectedCornersOverlap(mode, ladderDir))
                    return false;

            // Calculate new heights
            Dictionary<Direction, int> newHeights = GetNewHeights(mode, isIncrease);
            if (!World.IsValidNodeHeight(newHeights)) return false;
            int newBaseHeight = newHeights.Values.Min();
            int newMaxHeight = newHeights.Values.Max();

            // Check if all heights are within allowed values
            if (newHeights.Values.Any(x => x < 0)) return false;
            if (newHeights.Values.Any(x => x > World.MAX_HEIGHT)) return false;

            // Check if changing height would intersect the node with another one on the same coordinates
            List<BlockmapNode> nodes = World.GetNodes(WorldCoordinates);
            foreach(BlockmapNode node in nodes)
            {
                if (node == this) continue;
                if (World.DoNodesIntersect(newHeights, node.Height)) return false;
            }

            // Check if decreasing height would leave node under water of adjacent water body
            if (!isIncrease)
            {
                foreach (Direction corner1 in HelperFunctions.GetAffectedCorners(mode))
                {
                    foreach (Direction side in HelperFunctions.GetAffectedSides(corner1))
                    {
                        WaterNode adjWater = World.GetAdjacentWaterNode(WorldCoordinates, side);
                        if (adjWater == null) continue;
                        bool ownSideUnderwater = false;
                        bool otherSideUnderwater = false;
                        foreach (Direction corner2 in HelperFunctions.GetAffectedCorners(side))
                        {
                            if (newHeights[corner2] < adjWater.WaterBody.ShoreHeight) ownSideUnderwater = true;
                            if (adjWater.GroundNode.Height[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) otherSideUnderwater = true;

                            if (ownSideUnderwater && adjWater.GroundNode.Height[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) return false;
                            if (newHeights[corner2] < adjWater.WaterBody.ShoreHeight && otherSideUnderwater) return false;
                        }
                    }
                }
            }

            return true;
        }
        public void ChangeHeight(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> preChange = new Dictionary<Direction, int>();
            foreach (Direction dir in Height.Keys) preChange[dir] = Height[dir];

            Height = GetNewHeights(mode, isIncrease);

            // Don't apply change if resulting shape is not valid
            if (!World.IsValidNodeHeight(Height))
            {
                foreach (Direction dir in HelperFunctions.GetCorners()) Height[dir] = preChange[dir];
            }
            else UseAlternativeVariant = isIncrease;

            RecalculateShape();
        }
        private Dictionary<Direction, int> GetNewHeights(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> newHeights = new Dictionary<Direction, int>();
            foreach (Direction dir in Height.Keys) newHeights[dir] = Height[dir];

            // Calculate min and max height of affected corners
            int minHeight = World.MAX_HEIGHT;
            int maxHeight = 0;
            foreach (Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (Height[dir] < minHeight) minHeight = Height[dir];
                if (Height[dir] > maxHeight) maxHeight = Height[dir];
            }

            // Calculate new heights
            foreach (Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (newHeights[dir] == minHeight && isIncrease) newHeights[dir] += 1;
                if (newHeights[dir] == maxHeight && !isIncrease) newHeights[dir] -= 1;
            }

            return newHeights;
        }
        public void SetHeight(Dictionary<Direction, int> newHeights)
        {
            if (World.IsValidNodeHeight(newHeights))
            {
                foreach (Direction dir in HelperFunctions.GetCorners())
                    Height[dir] = newHeights[dir];

                RecalculateShape();
            }
        }

        #endregion

        #region Getters

        public override Surface GetSurface() => Surface;
        public override SurfaceProperties GetSurfaceProperties() => Surface.Properties;
        public override int GetSubType() => (int)Surface.Id;

        #endregion
    }
}
