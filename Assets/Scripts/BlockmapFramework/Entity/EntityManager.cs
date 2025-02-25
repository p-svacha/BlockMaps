using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
	/// <summary>
	/// Contains some general functions regarding entities and their creation.
	/// </summary>
	public static class EntityManager
	{
        /// <summary>
        /// Creates a new entity from a Def.
        /// </summary>
        public static Entity MakeEntity(EntityDef def)
		{
			Entity obj = (Entity)System.Activator.CreateInstance(def.EntityClass);
			return obj;
		}

        public static HashSet<BlockmapNode> GetOccupiedNodes(EntitySpawnProperties spawnProps) => GetOccupiedNodes(spawnProps.ResolvedDef, spawnProps.World, spawnProps.ResolvedTargetNode, spawnProps.ResolvedRotation, spawnProps.ResolvedMirrored, spawnProps.CustomHeight);
        /// <summary>
        /// Returns all nodes that would be occupied by an EntityDef when placed on the given originNode with the given properties.
        /// <br/> Returns null if entity can't be placed on that node.
        /// </summary>
        public static HashSet<BlockmapNode> GetOccupiedNodes(EntityDef def, World world, BlockmapNode originNode, Direction rotation, bool isMirrored, int customHeight = 0)
        {
            HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>() { originNode };

            Vector3Int dimensions = GetTranslatedDimensions(def, rotation, customHeight);
            if (dimensions.x == 1 && dimensions.z == 1) return nodes; // Shortcut for 1x1 entities

            // For each x, try to connect all the way up and see if everything is connected
            BlockmapNode yBaseNode = originNode;
            BlockmapNode cornerNodeNW = null;

            for (int x = 0; x < dimensions.x; x++)
            {
                // Try going east
                if (x > 0)
                {
                    Vector2Int eastCoordinates = HelperFunctions.GetCoordinatesInDirection(yBaseNode.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(yBaseNode, x, Direction.E));
                    if (eastNode == null) return null;

                    yBaseNode = eastNode;
                    nodes.Add(yBaseNode);
                }

                BlockmapNode yNode = yBaseNode;
                for (int y = 0; y < dimensions.z - 1; y++)
                {
                    // Try going north
                    Vector2Int northCoordinates = HelperFunctions.GetCoordinatesInDirection(yNode.WorldCoordinates, Direction.N);
                    List<BlockmapNode> candidateNodesNorth = world.GetNodes(northCoordinates);
                    BlockmapNode northNode = candidateNodesNorth.FirstOrDefault(x => world.DoAdjacentHeightsMatch(yNode, x, Direction.N));
                    if (northNode == null) return null;

                    yNode = northNode;
                    nodes.Add(yNode);
                    if (x == 0 && y == dimensions.z - 2) cornerNodeNW = yNode;
                }
            }

            // Now we have all nodes of the footprint
            // Also check if NW -> NE is fully connected to make sure its valid
            if (dimensions.z > 1)
            {
                for (int i = 0; i < dimensions.x - 1; i++)
                {
                    Vector2Int eastCoordinates = HelperFunctions.GetCoordinatesInDirection(cornerNodeNW.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(cornerNodeNW, x, Direction.E));

                    if (eastNode == null) return null;
                    else cornerNodeNW = eastNode;
                }
            }

            // Now that we now the footprint is fully connected, remove nodes where the the overwritten height is 0
            List<BlockmapNode> nodesToRemove = new List<BlockmapNode>();
            foreach(BlockmapNode node in nodes)
            {
                Vector2Int localCoordinates = node.WorldCoordinates - originNode.WorldCoordinates;
                localCoordinates = EntityManager.GetTranslatedPosition(localCoordinates, new Vector2Int(def.Dimensions.x, def.Dimensions.z), rotation, isMirrored);
                if (def.OverrideHeights.TryGetValue(localCoordinates, out int overrideHeight) && overrideHeight == 0)
                {
                    nodesToRemove.Add(node);
                }
            }
            foreach (BlockmapNode nodeToRemove in nodesToRemove) nodes.Remove(nodeToRemove);

            // Done
            return nodes;
        }

        /// <summary>
        /// Returns the world position that an entity of a specific EntityDef would have when placed on the given originNode with the given properties.
        /// </summary>
        public static Vector3 GetWorldPosition(EntityDef def, World world, BlockmapNode originNode, Direction rotation, int entityHeight, bool isMirrored)
        {
            // Validate
            if (def.RenderProperties.PositionType == PositionType.Custom) throw new System.Exception("PositionType is set to custom. This function handles the default positioning types.");

            // Take 2d center of entity as x/z position
            Vector3Int dimensions = GetTranslatedDimensions(def, rotation);
            Vector2 basePosition = originNode.WorldCoordinates + new Vector2(dimensions.x * 0.5f, dimensions.z * 0.5f);

            // Identify which nodes would be occupied
            HashSet<BlockmapNode> occupiedNodes = GetOccupiedNodes(def, world, originNode, rotation, isMirrored);

            // If placement is invalid, just set the placement node as altitude
            if (occupiedNodes == null) return new Vector3(basePosition.x, originNode.BaseWorldAltitude, basePosition.y);

            // Calculate y position based on PositionType
            float y = 0;
            if (def.RenderProperties.PositionType == PositionType.LowestPoint)
            {
                y = occupiedNodes.Min(n => n.BaseWorldAltitude);
            }
            if (def.RenderProperties.PositionType == PositionType.CenterPoint)
            {
                // Only works properly for 1x1 entities
                y = occupiedNodes.First().MeshCenterWorldPosition.y;
            }
            if (def.RenderProperties.PositionType == PositionType.HighestPoint)
            {
                y = occupiedNodes.Max(n => n.MaxWorldAltitude);
            }

            // Move position halfway below water surface if required
            bool isInWater = occupiedNodes.Any(n => n is WaterNode || (n is GroundNode ground && ground.WaterNode != null && ground.IsCenterUnderWater));
            if (isInWater && def.WaterBehaviour == WaterBehaviour.HalfBelowWaterSurface)
            {
                y -= (entityHeight * World.NodeHeight) / 2;
            }

            // Final position
            return new Vector3(basePosition.x, y, basePosition.y);
        }

        /// <summary>
        /// Returns the dimensions of an EntityDef given the rotation.
        /// </summary>
        public static Vector3Int GetTranslatedDimensions(EntityDef def, Direction rotation, int customHeight = 0)
        {
            if (def.Dimensions.x == def.Dimensions.z) return new Vector3Int(def.Dimensions.x, def.Dimensions.y, def.Dimensions.z);

            Vector3Int sourceDimensions = def.VariableHeight ? new Vector3Int(def.Dimensions.x, customHeight, def.Dimensions.z) : new Vector3Int(def.Dimensions.x, def.Dimensions.y, def.Dimensions.z);
            return GetTranslatedDimensions(sourceDimensions, rotation);
        }

        /// <summary>
        /// Returns the translated dimensions given the rotation.
        /// </summary>
        public static Vector3Int GetTranslatedDimensions(Vector3Int sourceDimensions, Direction rotation)
        {
            if (rotation == Direction.N || rotation == Direction.S) return new Vector3Int(sourceDimensions.x, sourceDimensions.y, sourceDimensions.z);
            if (rotation == Direction.E || rotation == Direction.W) return new Vector3Int(sourceDimensions.z, sourceDimensions.y, sourceDimensions.x);
            throw new System.Exception(rotation.ToString() + " is not a valid rotation");
        }

        /// <summary>
        /// Returns the translated dimensions given the rotation.
        /// </summary>
        public static Vector2Int GetTranslatedDimensions2d(Vector2Int sourceDimensions, Direction rotation)
        {
            if (rotation == Direction.N || rotation == Direction.S) return sourceDimensions;
            if (rotation == Direction.E || rotation == Direction.W) return new Vector2Int(sourceDimensions.y, sourceDimensions.x);
            throw new System.Exception(rotation.ToString() + " is not a valid rotation");
        }

        /// <summary>
        /// Returns the translated position of an entity with the specified dimensions, given its rotation (default = N) and if it is mirrored (on the x axis).
        /// </summary>
        public static Vector2Int GetTranslatedPosition(Vector2Int position, Vector2Int dimensions, Direction rotation, bool isMirrored)
        {
            Vector2Int translatedDimensions = GetTranslatedDimensions2d(dimensions, rotation);

            int x = position.x;
            int y = position.y;

            int rotatedX, rotatedY;
            switch (rotation)
            {
                case Direction.N:
                    rotatedX = x;
                    rotatedY = y;
                    break;

                case Direction.W:
                    rotatedX = y;
                    rotatedY = translatedDimensions.x - 1 - x;
                    break;

                case Direction.S:
                    rotatedX = translatedDimensions.x - 1 - x;
                    rotatedY = translatedDimensions.y - 1 - y;
                    break;

                case Direction.E:
                    rotatedX = translatedDimensions.y - 1 - y;
                    rotatedY = x;
                    break;

                default:
                    rotatedX = x;
                    rotatedY = y;
                    break;
            }

            if (isMirrored)
            {
                rotatedX = dimensions.x - 1 - rotatedX;
            }

            // Return the final transformed coordinate
            return new Vector2Int(rotatedX, rotatedY);
        }
    }
}
