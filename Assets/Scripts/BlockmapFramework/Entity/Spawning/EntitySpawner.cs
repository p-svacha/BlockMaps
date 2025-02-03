using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Collection of static functions used to spawn entities in the world.
    /// </summary>
    public static class EntitySpawner
    {
        /// <summary>
        /// Checks if an entity can be spawned given on a node given the provided spawn properties.
        /// </summary>
        public static bool CanSpawnEntity(EntitySpawnProperties spawnProps, bool inWorldGen, out string failReason)
        {
            failReason = "";

            // Validate spawn properties
            if (spawnProps == null)
            {
                failReason = "Spawn properties are null.";
                return false;
            }
            if (!spawnProps.IsValidated)
            {
                failReason = "Spawn properties are not validated.";
                return false;
            }
            if (!spawnProps.IsResolved)
            {
                failReason = "Spawn properties are not resolved.";
                return false;
            }

            // Get which nodes would be occupied when spawning the entity
            HashSet<BlockmapNode> occupiedNodes = EntityManager.GetOccupiedNodes(spawnProps);

            // Terrain below entity is not fully connected and therefore occupiedNodes is null
            if (occupiedNodes == null)
            {
                failReason = $"Terrain below entity is not fully connected.";
                return false;
            }

            // Get exact world space position where the entity wrapper GameObject would be placed
            Vector3 placePos = spawnProps.ResolvedDef.RenderProperties.GetWorldPositionFunction(spawnProps.ResolvedDef, spawnProps.World, spawnProps.ResolvedTargetNode, spawnProps.ResolvedRotation, spawnProps.ResolvedHeight, spawnProps.ResolvedMirrored);

            // Get min and max altitude this entity would cover when placed
            int minAltitude = Mathf.FloorToInt(placePos.y);
            int maxAltitude = minAltitude + spawnProps.ResolvedHeight - 1;

            // Perform some checks for all nodes that would be occupied when placing the entity on the given node
            foreach (BlockmapNode occupiedNode in occupiedNodes)
            {
                // Recalculate passability (only if called during world generation, else this has been done)
                if (inWorldGen) occupiedNode.RecalculatePassability();

                // Check if the place position is under water
                if (occupiedNode is GroundNode groundNode && groundNode.IsCenterUnderWater)
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) is under water.";
                    return false;
                }

                // Check if the place position is on water
                if (occupiedNode is WaterNode && spawnProps.ResolvedDef.WaterBehaviour == WaterBehaviour.Forbidden)
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) is water, but the EntityDef doesn't allow it to be placed on water.";
                    return false;
                }

                // Check if entity can stand here
                int headSpace = occupiedNode.MaxPassableHeight[Direction.None];
                if (minAltitude + headSpace <= maxAltitude)
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) does not have enough headspace for the entity. Headspace: {headSpace}, Height: {spawnProps.ResolvedHeight}.";
                    return false;
                }

                // Check if node already has an entity
                if (!spawnProps.AllowCollisionWithOtherEntities && occupiedNode.Entities.Count > 0)
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) does already contain another entity. Collisions are set to not be allowed.";
                    return false;
                }

                // Check if flat
                if (spawnProps.ResolvedDef.RequiresFlatTerrain && !occupiedNode.IsFlat())
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) is not flat.";
                    return false;
                }

                // Node is forbidden
                if (spawnProps.ForbiddenNodes != null && spawnProps.ForbiddenNodes.Contains(occupiedNode))
                {
                    failReason = $"A node that would be occupied ({occupiedNode}) is forbidden.";
                    return false;
                }

                // Node has forbidden tag
                if (spawnProps.ForbiddenNodeTags != null)
                {
                    foreach (string tag in occupiedNode.Tags)
                    {
                        if (spawnProps.ForbiddenNodeTags.Contains(tag))
                        {
                            failReason = $"A node that would be occupied ({occupiedNode}) contains forbidden tag '{tag}'.";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tries spawning an entity give the provided spawn properties.
        /// </summary>
        /// <returns>The spawned entity if successful. Null of not successful.</returns>
        public static Entity TrySpawnEntity(EntitySpawnProperties spawnProps, bool inWorldGen = true, int maxAttempts = 1, bool updateWorld = false, bool debug = false)
        {
            // Validate spawn properties
            if (!spawnProps.Validate(inWorldGen, out string propValidationFailReason))
            {
                if (debug) Log($"Invalid entity spawn properties: {propValidationFailReason}");
                return null;
            }

            // Try to spawn with different resolved attributes until max attempts are reached.
            int numAttempts = 0;
            while (numAttempts++ < maxAttempts)
            {
                spawnProps.Resolve();

                // Check if resolve found a target node
                if(spawnProps.ResolvedTargetNode == null)
                {
                    if (debug) Log($"Failed to spawn entity {spawnProps.ResolvedDef.DefName} in attempt #{numAttempts}: SpawnPositionProperties didn't find a valid ResolvedTargetNode in Resolve().");
                    continue;
                }

                // >>> Post-spawn-checks: If failed, just keep trying with new attributes
                if (!CanSpawnEntity(spawnProps, inWorldGen, out string canSpawnFailReason))
                {
                    if (debug) Log($"Failed to spawn entity {spawnProps.ResolvedDef.DefName} on {spawnProps.ResolvedTargetNode} in attempt #{numAttempts}: {canSpawnFailReason}");
                    continue;
                }

                // CanSpawn successful => spawn entity
                Entity spawnedEntity = spawnProps.World.SpawnEntity(spawnProps);

                // >>> Post-spawn-checks: If failed, remove the entity again and keep trying with new attributes

                // Check if required roaming area is fulfilled
                if (spawnProps.RequiredRoamingArea > 0 && !Pathfinder.HasRoamingArea(spawnProps.ResolvedTargetNode, spawnProps.RequiredRoamingArea, (MovingEntity)spawnedEntity, spawnProps.ForbiddenNodes))
                {
                    if (debug) Log($"Failed to spawn entity {spawnProps.ResolvedDef.DefName} on {spawnProps.ResolvedTargetNode} in attempt #{numAttempts}: Required roaming area not fulfilled");
                    spawnProps.World.RemoveEntity(spawnedEntity, updateWorld: false);
                    continue;
                }

                // >>> All checks successful. Entity is successfully placed
                if (spawnedEntity == null) throw new System.Exception("All checks successfully passed through but entity still did not get spawned in world.SpawnEntity. This should never happen.");

                // Update world
                if (updateWorld) spawnProps.World.UpdateWorldSystems(new Parcel(spawnedEntity.OriginNode.WorldCoordinates, new Vector2Int(spawnedEntity.GetTranslatedDimensions().x, spawnedEntity.GetTranslatedDimensions().z)));

                // Return
                return spawnedEntity;
            }

            // Reached max attempts
            if (debug) Log($"Could not spawn {spawnProps.ResolvedDef.DefName} after {maxAttempts} attempts.");
            return null;
        }

        #region Private
        private static void Log(string message)
        {
            Debug.Log($"[EntitySpawner] {message}");
        }

        #endregion
    }
}
