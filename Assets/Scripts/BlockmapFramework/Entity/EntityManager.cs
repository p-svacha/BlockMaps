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

        /// <summary>
        /// Returns all nodes that would be occupied by an EntityDef when placed on the given originNode with the given properties.
        /// <br/> Returns null if entity can't be placed on that node.
        /// </summary>
        public static HashSet<BlockmapNode> GetOccupiedNodes(EntityDef def, World world, BlockmapNode originNode, Direction rotation, bool isMirrored, int customHeight = 0)
        {
            HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>() { originNode };

            Vector3Int dimensions = GetTranslatedDimensions(def, rotation, customHeight);

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
        /// <br/>By default this is in the center of the entity in the x and z axis and on the bottom in the y axis.
        /// </summary>
        public static Vector3 GetWorldPosition(EntityDef def, World world, BlockmapNode originNode, Direction rotation, int entityHeight, bool isMirrored)
        {
            // Take 2d center of entity as x/z position
            Vector3Int dimensions = GetTranslatedDimensions(def, rotation);
            Vector2 basePosition = originNode.WorldCoordinates + new Vector2(dimensions.x * 0.5f, dimensions.z * 0.5f);

            // Identify which nodes would be occupied
            HashSet<BlockmapNode> occupiedNodes = GetOccupiedNodes(def, world, originNode, rotation, isMirrored);

            // If placement is invalid, just set the placement node as altitude
            if (occupiedNodes == null) return new Vector3(basePosition.x, originNode.BaseWorldAltitude, basePosition.y);

            // Else calculate the exact y position
            float y = 0;
            bool isInWater = false;

            // For moving characters (always 1x1, just take the center world position of the node)
            if (def.HasCompProperties<CompProperties_Movement>())
            {
                y = occupiedNodes.First().MeshCenterWorldPosition.y;
                isInWater = occupiedNodes.First() is WaterNode;
            }

            // For static objects the lowest node of all occupied nodes.
            else
            {
                float lowestY = occupiedNodes.Min(n => n.BaseWorldAltitude);
                List<BlockmapNode> lowestYNodes = occupiedNodes.Where(n => n.BaseWorldAltitude == lowestY).ToList();
                y = lowestY;
                isInWater = lowestYNodes.Any(n => n is WaterNode || (n is GroundNode ground && ground.WaterNode != null && ground.IsCenterUnderWater));
            }

            // Move position halfway below water surface if required
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
            Vector3Int sourceDimensions = def.VariableHeight ? new Vector3Int(def.Dimensions.x, customHeight, def.Dimensions.z) : def.Dimensions;
            return GetTranslatedDimensions(sourceDimensions, rotation);
        }

        /// <summary>
        /// Returns the translated dimensions given the rotation.
        /// </summary>
        public static Vector3Int GetTranslatedDimensions(Vector3Int sourceDimensions, Direction rotation)
        {
            if (rotation == Direction.N || rotation == Direction.S) return sourceDimensions;
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

        #region Spawning entities

        /// <summary>
        /// Spawns an entity on a random node near the given point and returns the entity instance.
        /// </summary>
        public static Entity SpawnEntityAround(World world, EntityDef def, Actor player, Vector2Int worldCoordinates, float standard_deviation, Direction forcedRotation = Direction.None, int maxAttempts = 1, int requiredRoamingArea = -1, List<BlockmapNode> forbiddenNodes = null, string variantName = "", bool randomMirror = false, List<string> forbiddenTags = null)
        {
            if (standard_deviation == 0f) maxAttempts = 1;
            int numAttempts = 0;

            while (numAttempts++ < maxAttempts) // Keep searching until we find a suitable position
            {
                Vector2Int targetCoordinates = HelperFunctions.GetRandomNearPosition(worldCoordinates, standard_deviation);
                Direction rotation = forcedRotation == Direction.None ? HelperFunctions.GetRandomSide() : forcedRotation;
                bool isMirrored = randomMirror ? Random.value < 0.5f : false;

                Entity spawnedEntity = TrySpawnEntity(world, def, player, targetCoordinates, rotation, isMirrored, variantName, requiredRoamingArea, forbiddenNodes, forbiddenTags);
                if (spawnedEntity != null) return spawnedEntity;
            }

            Debug.Log($"Could not spawn {def.DefName} around {worldCoordinates} after {maxAttempts} attempts.");
            return null;
        }

        /// <summary>
        /// Spawns an entity within a given area in the world and returns the entity instance.
        /// </summary>
        public static Entity SpawnEntityWithin(World world, EntityDef def, Actor player, int minX, int maxX, int minY, int maxY, Direction forcedRotation = Direction.None, int maxAttempts = 1, int requiredRoamingArea = -1, List<BlockmapNode> forbiddenNodes = null, string variantName = "", bool randomMirror = false, List<string> forbiddenTags = null)
        {
            int numAttempts = 0;
            while (numAttempts++ < maxAttempts) // Keep searching until we find a suitable position
            {
                Vector2Int targetCoordinates = new Vector2Int(Random.Range(minX, maxX + 1), Random.Range(minY, maxY + 1));
                Direction rotation = forcedRotation == Direction.None ? HelperFunctions.GetRandomSide() : forcedRotation;
                bool isMirrored = randomMirror ? Random.value < 0.5f : false;

                Entity spawnedEntity = TrySpawnEntity(world, def, player, targetCoordinates, rotation, isMirrored, variantName, requiredRoamingArea, forbiddenNodes, forbiddenTags);
                if (spawnedEntity != null) return spawnedEntity;
            }

            Debug.Log($"Could not spawn {def.Label} within x:{minX}-{maxX}, y:{minY}-{maxY} after {maxAttempts} attempts.");
            return null;
        }

        /// <summary>
        /// Tries spawning an entity on a random node on the given coordinates with all the given restrictions.
        /// <br/>Returns the entity instance if successful or null of not successful.
        private static Entity TrySpawnEntity(World world, EntityDef def, Actor player, Vector2Int worldCoordinates, Direction rotation, bool isMirrored, string variantName = "", int requiredRoamingArea = -1, List<BlockmapNode> forbiddenNodes = null, List<string> forbiddenTags = null)
        {
            if (!world.IsInWorld(worldCoordinates)) return null;

            BlockmapNode targetNode = world.GetNodes(worldCoordinates).RandomElement();
            if (forbiddenNodes != null && forbiddenNodes.Contains(targetNode)) return null;
            if (forbiddenTags != null && targetNode.Tags.Any(t => forbiddenTags.Contains(t)))
            {
                Debug.Log($"Not spawning {def.LabelCap} on {targetNode} because it has a forbidden tag");
                return null;
            }
            if (!world.CanSpawnEntity(def, targetNode, rotation, isMirrored, forceHeadspaceRecalc: true)) return null;

            int variantIndex = 0;
            if (variantName != "")
            {
                EntityVariant variant = def.RenderProperties.Variants.FirstOrDefault(v => v.VariantName == variantName);
                variantIndex = variant != null ? def.RenderProperties.Variants.IndexOf(variant) : 0;
            }

            Entity spawnedEntity = world.SpawnEntity(def, targetNode, rotation, isMirrored, player, updateWorld: false, variantIndex: variantIndex);

            if (requiredRoamingArea > 0 && !Pathfinder.HasRoamingArea(targetNode, requiredRoamingArea, spawnedEntity, forbiddenNodes))
            {
                Debug.Log($"[EntityManager - SpawnEntityAround] Removing {spawnedEntity.LabelCap} from {targetNode} because it didn't have enough roaming space there.");
                world.RemoveEntity(spawnedEntity, updateWorld: false);
                return null;
            }

            return spawnedEntity;
        }

        #endregion
    }
}
