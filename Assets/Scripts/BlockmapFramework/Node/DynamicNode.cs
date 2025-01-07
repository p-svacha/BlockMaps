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
        public DynamicNode() { }
        public DynamicNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceDef surfaceDef) : base(world, chunk, id, localCoordinates, height, surfaceDef) { }

        #region Actions

        public bool CanChangeShape(Direction mode)
        {
            // Entities: Can't change height in any entity is on the node (includes ladders/doors)
            if (Entities.Count > 0) return false;

            return true;
        }

        public virtual bool CanChangeShape(Direction mode, bool isIncrease)
        {
            // General checks no matter if up/down
            if (!CanChangeShape(mode)) return false;

            // Calculate new heights
            Dictionary<Direction, int> newAltitude = GetNewHeights(mode, isIncrease);
            if (!IsValidShape(newAltitude)) return false;
            int newBaseHeight = newAltitude.Values.Min();
            int newMaxHeight = newAltitude.Values.Max();

            // Check if all heights are within allowed values
            if (newAltitude.Values.Any(x => x < 0)) return false;
            if (newAltitude.Values.Any(x => x > World.MAX_ALTITUDE)) return false;

            // Check that is not passing through another node (meaning that one needs to be clearly above the other)
            List<BlockmapNode> nodesOnCoordinate = World.GetNodes(WorldCoordinates);
            foreach (BlockmapNode node in nodesOnCoordinate)
            {
                if (node == this) continue;
                if (!World.IsAbove(newAltitude, node.Altitude) && !World.IsAbove(node.Altitude, newAltitude)) return false;
            }

            // Check if no more than 2 corners would overlap with another node
            foreach (BlockmapNode node in nodesOnCoordinate)
            {
                if (node == this) continue;
                if (World.GetNumOverlappingCorners(newAltitude, node.Altitude) > 2) return false;
            }

            // Check if increasing height would lead to a fence intersecting with a node above
            if (isIncrease)
            {
                foreach (Fence fence in Fences.Values)
                {
                    Dictionary<Direction, int> newFenceHeights = fence.GetMaxHeights(newAltitude);
                    foreach(BlockmapNode nodeAbove in nodesOnCoordinate)
                    {
                        if (nodeAbove == this) continue;
                        if (nodeAbove.BaseAltitude >= newBaseHeight)
                        {
                            foreach(Direction corner in newFenceHeights.Keys)
                            {
                                if (nodeAbove.Altitude[corner] < newFenceHeights[corner]) return false;
                            }
                        }
                    }
                }
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
                            if (newAltitude[corner2] < adjWater.WaterBody.ShoreHeight) ownSideUnderwater = true;
                            if (adjWater.GroundNode.Altitude[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) otherSideUnderwater = true;

                            if (ownSideUnderwater && adjWater.GroundNode.Altitude[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) return false;
                            if (newAltitude[corner2] < adjWater.WaterBody.ShoreHeight && otherSideUnderwater) return false;
                        }
                    }
                }
            }

            // Check if decreasing height would intersect with a fence below
            if(!isIncrease)
            {
                foreach (BlockmapNode nodeBelow in World.GetNodes(WorldCoordinates, 0, BaseAltitude - 1))
                {
                    foreach(Fence fence in nodeBelow.Fences.Values)
                    {
                        Dictionary<Direction, int> fenceHeights = fence.MaxHeights;
                        foreach(Direction corner in fenceHeights.Keys)
                        {
                            if (fenceHeights[corner] > newAltitude[corner]) return false;
                        }
                    }
                }
            }

            return true;
        }
        public void ChangeShape(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> preChange = new Dictionary<Direction, int>();
            foreach (Direction dir in Altitude.Keys) preChange[dir] = Altitude[dir];

            Altitude = GetNewHeights(mode, isIncrease);

            // Don't apply change if resulting shape is not valid
            if (!IsValidShape(Altitude))
            {
                foreach (Direction dir in HelperFunctions.GetCorners()) Altitude[dir] = preChange[dir];
            }
            else LastHeightChangeWasIncrease = isIncrease;

            RecalculateShape();
        }

        /// <summary>
        /// Sets the height of a single corner.
        /// </summary>
        public void SetAltitude(Direction corner, int altitude)
        {
            Dictionary<Direction, int> newHeights = new Dictionary<Direction, int>();
            foreach(Direction dir in HelperFunctions.GetCorners())
            {
                if (dir == corner) newHeights[dir] = altitude;
                else newHeights[dir] = Altitude[dir];
            }

            if(IsValidShape(newHeights))
            {
                if (Altitude[corner] < newHeights[corner]) LastHeightChangeWasIncrease = true;
                Altitude = newHeights;
                RecalculateShape();
            }
        }

        private Dictionary<Direction, int> GetNewHeights(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> newHeights = new Dictionary<Direction, int>();
            foreach (Direction dir in Altitude.Keys) newHeights[dir] = Altitude[dir];

            // Calculate min and max height of affected corners
            int minHeight = World.MAX_ALTITUDE;
            int maxHeight = 0;
            foreach (Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (Altitude[dir] < minHeight) minHeight = Altitude[dir];
                if (Altitude[dir] > maxHeight) maxHeight = Altitude[dir];
            }

            // Calculate new heights
            foreach (Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (newHeights[dir] == minHeight && isIncrease) newHeights[dir] += 1;
                if (newHeights[dir] == maxHeight && !isIncrease) newHeights[dir] -= 1;
            }

            return newHeights;
        }

        /// <summary>
        /// Sets the height of all corners
        /// </summary>
        public void SetAltitude(Dictionary<Direction, int> newAltitude)
        {
            if (IsValidShape(newAltitude))
            {
                foreach (Direction dir in HelperFunctions.GetCorners())
                    Altitude[dir] = newAltitude[dir];

                RecalculateShape();
            }
        }

        /// <summary>
        /// Adds the given altitude to all corners
        /// </summary>
        public void AddAltitude(Dictionary<Direction, int> toAddAltitudes)
        {
            Dictionary<Direction, int> newAltitude = new Dictionary<Direction, int>()
            {
                { Direction.SW, Altitude[Direction.SW] + toAddAltitudes[Direction.SW] },
                { Direction.SE, Altitude[Direction.SE] + toAddAltitudes[Direction.SE] },
                { Direction.NE, Altitude[Direction.NE] + toAddAltitudes[Direction.NE] },
                { Direction.NW, Altitude[Direction.NW] + toAddAltitudes[Direction.NW] },
            };
            SetAltitude(newAltitude);
        }

        /// <summary>
        /// Sets the node to be flat on the given altitude.
        /// </summary>
        public void SetAltitude(int altitude)
        {
            SetAltitude(HelperFunctions.GetFlatHeights(altitude));
        }

        private bool IsValidShape(Dictionary<Direction, int> altitude)
        {
            if (altitude.Values.Any(x => x < World.MAP_EDGE_ALTITUDE)) return false;
            if (altitude.Values.Any(x => x > World.MAX_ALTITUDE)) return false;

            // Not allowed that the altitude change between 2 corners is greater than World.MAX_NODE_STEEPNESS
            if (Mathf.Abs(altitude[Direction.SE] - altitude[Direction.SW]) > World.MAX_NODE_STEEPNESS ||
            Mathf.Abs(altitude[Direction.SW] - altitude[Direction.NW]) > World.MAX_NODE_STEEPNESS ||
            Mathf.Abs(altitude[Direction.NW] - altitude[Direction.NE]) > World.MAX_NODE_STEEPNESS ||
            Mathf.Abs(altitude[Direction.NE] - altitude[Direction.SE]) > World.MAX_NODE_STEEPNESS)
                return false;

            return true;
        }

        #endregion


        public override void RecalculateMeshCenterWorldPosition()
        {
            MeshCenterWorldPosition = new Vector3(WorldCoordinates.x + 0.5f, GetWorldMeshAltitude(new Vector2(0.5f, 0.5f)), WorldCoordinates.y + 0.5f);
        }
    }
}
