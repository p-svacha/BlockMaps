using System;
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
			Entity obj = (Entity)Activator.CreateInstance(def.EntityClass);
			return obj;
		}

        /// <summary>
        /// Returns all nodes that would be occupied by an EntityDef when placed on the given originNode with the given properties.
        /// <br/> Returns null if entity can't be placed on that node.
        /// </summary>
        public static HashSet<BlockmapNode> GetOccupiedNodes(EntityDef def, World world, BlockmapNode originNode, Direction rotation, int customHeight = 0)
        {
            HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>() { originNode };

            Vector3Int dimensions = TranslatedDimensions(def, rotation, customHeight);

            // For each x, try to connect all the way up and see if everything is connected
            BlockmapNode yBaseNode = originNode;
            BlockmapNode cornerNodeNW = null;

            for (int x = 0; x < dimensions.x; x++)
            {
                // Try going east
                if (x > 0)
                {
                    Vector2Int eastCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(yBaseNode.WorldCoordinates, Direction.E);
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
                    Vector2Int northCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(yNode.WorldCoordinates, Direction.N);
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
                    Vector2Int eastCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(cornerNodeNW.WorldCoordinates, Direction.E);
                    List<BlockmapNode> candidateNodesEast = world.GetNodes(eastCoordinates);
                    BlockmapNode eastNode = candidateNodesEast.FirstOrDefault(x => world.DoAdjacentHeightsMatch(cornerNodeNW, x, Direction.E));

                    if (eastNode == null) return null;
                    else cornerNodeNW = eastNode;
                }
            }

            return nodes;
        }

        /// <summary>
        /// Returns the world position that an entity of a specific EntityDef would have when placed on the given originNode with the given properties.
        /// <br/>By default this is in the center of the entity in the x and z axis and on the bottom in the y axis.
        /// </summary>
        public static Vector3 GetWorldPosition(EntityDef def, World world, BlockmapNode originNode, Direction rotation, bool isMirrored = false)
        {
            // Take origin node as base position
            Vector3Int dimensions = TranslatedDimensions(def, rotation);
            Vector2 basePosition = originNode.WorldCoordinates + new Vector2(dimensions.x * 0.5f, dimensions.z * 0.5f);

            // For y, take the lowest node center out of all occupied nodes
            HashSet<BlockmapNode> occupiedNodes = GetOccupiedNodes(def, world, originNode, rotation);
            float y;
            if (occupiedNodes == null) y = originNode.GetWorldMeshAltitude(new Vector2(0.5f, 0.5f));
            else y = occupiedNodes.Min(x => x.GetWorldMeshAltitude(new Vector2(0.5f, 0.5f)));

            // Final position
            return new Vector3(basePosition.x, y, basePosition.y);
        }

        /// <summary>
        /// Returns the dimensions of an EntityDef given the rotation.
        /// </summary>
        public static Vector3Int TranslatedDimensions(EntityDef def, Direction rotation, int customHeight = 0)
        {
            Vector3Int sourceDimensions = def.VariableHeight ? new Vector3Int(def.Dimensions.x, customHeight, def.Dimensions.z) : def.Dimensions;

            if (rotation == Direction.N || rotation == Direction.S) return sourceDimensions;
            if (rotation == Direction.E || rotation == Direction.W) return new Vector3Int(sourceDimensions.z, sourceDimensions.y, sourceDimensions.x);
            throw new System.Exception(rotation.ToString() + " is not a valid rotation");
        }
    }
}
