using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Action surface of a world. Defines creation, registration, mutation,
    /// and teardown operations for nodes, entities, water, walls/fences/doors/ladders,
    /// vision/exploration, rooms/zones, and inventories.
    /// </summary>
    public interface IWorld
    {
        // --- Nodes & Chunks ---

        /// <summary>
        /// Registers a node in the world and its chunk.
        /// </summary>
        /// <param name="node">Node to register.</param>
        /// <param name="registerInWorld">If true, adds to the global Nodes registry.</param>
        void RegisterNode(BlockmapNode node, bool registerInWorld = true);

        /// <summary>
        /// Deregisters a node from the world and its chunk. Removes attached structures/entities (fences, ladders).
        /// </summary>
        /// <param name="node">Node to deregister.</param>
        void DeregisterNode(BlockmapNode node);

        /// <summary>
        /// Registers a chunk in the world.
        /// </summary>
        /// <param name="chunk">Chunk to register.</param>
        void RegisterChunk(Chunk chunk);

        // --- Actors & Exploration/Vision ---

        /// <summary>
        /// Creates and registers a new actor.
        /// </summary>
        /// <param name="name">Actor display name.</param>
        /// <param name="color">Actor color.</param>
        /// <returns>The created actor.</returns>
        Actor AddActor(string name, Color color);

        /// <summary>
        /// Clears exploration/last-seen info for this actor and recomputes vision.
        /// </summary>
        /// <param name="actor">Actor to reset.</param>
        void ResetExploration(Actor actor);

        /// <summary>
        /// Marks all nodes/walls/entities as explored for the actor and recomputes vision.
        /// </summary>
        /// <param name="actor">Actor to reveal for.</param>
        void ExploreEverything(Actor actor);

        // --- Dynamic nodes: shape/surface/void ---

        /// <summary>
        /// Checks whether a dynamic node can change shape along a direction.
        /// </summary>
        /// <param name="node">Dynamic node.</param>
        /// <param name="mode">Shape change direction.</param>
        /// <param name="isIncrease">True to increase, false to decrease.</param>
        /// <returns>True if the change is valid.</returns>
        bool CanChangeShape(DynamicNode node, Direction mode, bool isIncrease);

        /// <summary>
        /// Applies a shape change to a dynamic node and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Dynamic node.</param>
        /// <param name="mode">Shape change direction.</param>
        /// <param name="isIncrease">True to increase, false to decrease.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void ChangeShape(DynamicNode node, Direction mode, bool isIncrease, bool updateWorld);

        /// <summary>
        /// Sets the surface of a dynamic node and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Dynamic node.</param>
        /// <param name="surface">Surface definition.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void SetSurface(DynamicNode node, SurfaceDef surface, bool updateWorld);

        /// <summary>
        /// Converts a ground node to void and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Ground node.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void SetGroundNodeAsVoid(GroundNode node, bool updateWorld);

        /// <summary>
        /// Reverts a ground node from void at a given altitude and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Ground node.</param>
        /// <param name="altitude">Base altitude for the restored ground.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void UnsetGroundNodeAsVoid(GroundNode node, int altitude, bool updateWorld);

        // --- Air nodes ---

        /// <summary>
        /// Checks if an air node can be built at coordinates/altitude considering overlaps, water, fences, and headroom.
        /// </summary>
        /// <param name="worldCoordinates">World XZ coordinates.</param>
        /// <param name="altitude">Altitude (Y).</param>
        /// <returns>True if placement is valid.</returns>
        bool CanBuildAirNode(Vector2Int worldCoordinates, int altitude);

        /// <summary>
        /// Builds an air node and optionally updates nearby systems.
        /// </summary>
        /// <param name="worldCoordinates">World XZ coordinates.</param>
        /// <param name="altitude">Altitude (Y).</param>
        /// <param name="surfaceDef">Surface definition for the node.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        /// <returns>The created air node, or null if invalid.</returns>
        AirNode BuildAirNode(Vector2Int worldCoordinates, int altitude, SurfaceDef surfaceDef, bool updateWorld);

        /// <summary>
        /// Checks whether an air node can be removed (e.g., not occupied).
        /// </summary>
        /// <param name="node">Air node.</param>
        /// <returns>True if removal is allowed.</returns>
        bool CanRemoveAirNode(AirNode node);

        /// <summary>
        /// Removes an air node and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Air node to remove.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void RemoveAirNode(AirNode node, bool updateWorld);

        // --- Entities (spawn/register/remove/ghosts) ---

        /// <summary>
        /// Spawns an entity using resolved spawn properties.
        /// </summary>
        /// <param name="spawnProps">Resolved spawn properties.</param>
        /// <returns>The spawned entity.</returns>
        Entity SpawnEntity(EntitySpawnProperties spawnProps);

        /// <summary>
        /// Spawns an entity on a node with placement/actor/rotation options and optional pre-initialization hook.
        /// </summary>
        /// <param name="def">Entity definition.</param>
        /// <param name="node">Origin node.</param>
        /// <param name="rotation">Facing direction.</param>
        /// <param name="isMirrored">If true, mirror placement.</param>
        /// <param name="actor">Owning actor.</param>
        /// <param name="updateWorld">If true, updates world systems around occupied area.</param>
        /// <param name="height">Custom placement height or -1 for default.</param>
        /// <param name="preInit">Optional hook executed before Init().</param>
        /// <param name="variantIndex">Variant index for visuals/data.</param>
        /// <returns>The spawned entity.</returns>
        Entity SpawnEntity(EntityDef def, BlockmapNode node, Direction rotation, bool isMirrored, Actor actor, bool updateWorld, int height = -1, Action<Entity> preInit = null, int variantIndex = 0);

        /// <summary>
        /// Marks an entity for removal at the end of the current tick.
        /// </summary>
        /// <param name="e">Entity to remove.</param>
        void MarkEntityToBeRemovedThisTick(Entity e);

        /// <summary>
        /// Removes an entity immediately, updates visibility/ghosts, and optionally updates nearby systems.
        /// </summary>
        /// <param name="entityToRemove">Entity to remove.</param>
        /// <param name="updateWorld">If true, updates world systems around occupied area.</param>
        void RemoveEntity(Entity entityToRemove, bool updateWorld);

        /// <summary>
        /// Removes all entities placed on a node.
        /// </summary>
        /// <param name="node">Node whose entities will be removed.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void RemoveEntities(BlockmapNode node, bool updateWorld);

        /// <summary>
        /// Registers an entity in world/actor/chunk registries and optionally updates nearby systems.
        /// </summary>
        /// <param name="entity">Entity to register.</param>
        /// <param name="updateWorld">If true, updates world systems around occupied area.</param>
        /// <param name="registerInWorld">If true, adds to the global Entities registry.</param>
        void RegisterEntity(Entity entity, bool updateWorld, bool registerInWorld = true);

        /// <summary>
        /// Deregisters an entity from world/actor/chunk registries.
        /// </summary>
        /// <param name="entity">Entity to deregister.</param>
        void DeregisterEntity(Entity entity);

        /// <summary>
        /// Creates a ghost marker representing a removed entity's last known info for an actor.
        /// </summary>
        /// <param name="removedEntity">Original entity.</param>
        /// <param name="actor">Actor to receive the ghost marker.</param>
        /// <param name="lastSeenInfo">Last seen data.</param>
        void CreateGhostMarker(Entity removedEntity, Actor actor, LastSeenInfo lastSeenInfo);

        /// <summary>
        /// Removes all ghost markers for an actor that touch any of the given nodes.
        /// </summary>
        /// <param name="actor">Actor whose ghost markers are pruned.</param>
        /// <param name="nodes">Nodes to test for overlap.</param>
        void RemoveGhostMarkers(Actor actor, HashSet<BlockmapNode> nodes);

        // --- Water ---

        /// <summary>
        /// Simulates whether a water body could be added starting at a node with a maximum depth.
        /// </summary>
        /// <param name="node">Seed ground node.</param>
        /// <param name="maxDepth">Maximum allowed depth below shore.</param>
        /// <returns>A candidate water body (data only) or null if invalid.</returns>
        WaterBody CanAddWater(GroundNode node, int maxDepth);

        /// <summary>
        /// Converts a computed water-body dataset into actual water nodes and optionally updates nearby systems.
        /// </summary>
        /// <param name="data">Water body data (covered nodes, shore height).</param>
        /// <param name="updateWorld">If true, updates world systems around the water area.</param>
        void AddWaterBody(WaterBody data, bool updateWorld);

        /// <summary>
        /// Flood-fills a water body from a source node at a given shore height and optionally updates nearby systems.
        /// </summary>
        /// <param name="sourceNode">Seed ground node.</param>
        /// <param name="shoreHeight">Shore altitude.</param>
        /// <param name="updateWorld">If true, updates world systems around the water area.</param>
        void AddWaterBody(GroundNode sourceNode, int shoreHeight, bool updateWorld);

        /// <summary>
        /// Removes a water body, deregistering water nodes and clearing references. Optionally updates nearby systems.
        /// </summary>
        /// <param name="water">Water body to remove.</param>
        /// <param name="updateWorld">If true, updates world systems around the water area.</param>
        void RemoveWaterBody(WaterBody water, bool updateWorld);

        // --- Fences ---

        /// <summary>
        /// Checks whether a fence of a given definition can be built on a node side at a height.
        /// </summary>
        /// <param name="def">Fence definition.</param>
        /// <param name="node">Target node.</param>
        /// <param name="side">Node side (direction / corner).</param>
        /// <param name="height">Fence height.</param>
        /// <returns>True if build is valid.</returns>
        bool CanBuildFence(FenceDef def, BlockmapNode node, Direction side, int height);

        /// <summary>
        /// Builds a fence on a node side and optionally updates nearby systems.
        /// </summary>
        /// <param name="def">Fence definition.</param>
        /// <param name="node">Target node.</param>
        /// <param name="side">Node side.</param>
        /// <param name="height">Fence height (clamped to def).</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void BuildFence(FenceDef def, BlockmapNode node, Direction side, int height, bool updateWorld);

        /// <summary>
        /// Registers a fence in world, chunk, and node registries.
        /// </summary>
        /// <param name="fence">Fence to register.</param>
        /// <param name="registerInWorld">If true, adds to the global Fences registry.</param>
        void RegisterFence(Fence fence, bool registerInWorld = true);

        /// <summary>
        /// Removes a fence and optionally updates nearby systems.
        /// </summary>
        /// <param name="fence">Fence to remove.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        void RemoveFence(Fence fence, bool updateWorld);

        /// <summary>
        /// Deregisters a fence from world, chunk, and node registries.
        /// </summary>
        /// <param name="fence">Fence to deregister.</param>
        void DeregisterFence(Fence fence);

        // --- Walls ---

        /// <summary>
        /// Checks whether a wall can be built at global cell coordinates on a given side, considering overlaps.
        /// </summary>
        /// <param name="globalCellCoordinates">Global XYZ cell coordinates.</param>
        /// <param name="side">Wall side at the cell.</param>
        /// <returns>True if build is valid.</returns>
        bool CanBuildWall(Vector3Int globalCellCoordinates, Direction side);

        /// <summary>
        /// Builds a vertical stack of walls (height) from a base cell, optionally updates nearby systems.
        /// </summary>
        /// <param name="globalSourceCellCoordinates">Base global cell coordinates.</param>
        /// <param name="side">Wall side at the cells.</param>
        /// <param name="shape">Wall shape definition.</param>
        /// <param name="material">Wall material definition.</param>
        /// <param name="mirrored">If true, mirror geometry along side.</param>
        /// <param name="height">Number of cells high.</param>
        /// <param name="updateWorld">If true, updates world systems around the column.</param>
        /// <returns>List of created walls.</returns>
        List<Wall> BuildWalls(Vector3Int globalSourceCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool mirrored, int height, bool updateWorld);

        /// <summary>
        /// Builds a single wall at a cell and optionally updates nearby systems.
        /// </summary>
        /// <param name="globalCellCoordinates">Global cell coordinates.</param>
        /// <param name="side">Wall side at the cell.</param>
        /// <param name="shape">Wall shape definition.</param>
        /// <param name="material">Wall material definition.</param>
        /// <param name="updateWorld">If true, updates world systems around the cell.</param>
        /// <param name="mirrored">If true, mirror geometry along side.</param>
        /// <returns>The created wall.</returns>
        Wall BuildWall(Vector3Int globalCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool updateWorld, bool mirrored = false);

        /// <summary>
        /// Registers a wall in world/chunk registries and links opposite wall if present.
        /// </summary>
        /// <param name="wall">Wall to register.</param>
        /// <param name="registerInWorld">If true, adds to the global Walls registry.</param>
        void RegisterWall(Wall wall, bool registerInWorld = true);

        /// <summary>
        /// Removes a wall and optionally updates nearby systems.
        /// </summary>
        /// <param name="wall">Wall to remove.</param>
        /// <param name="updateWorld">If true, updates world systems around the cell.</param>
        void RemoveWall(Wall wall, bool updateWorld);

        /// <summary>
        /// Deregisters a wall from world/chunk registries and clears opposite links.
        /// </summary>
        /// <param name="wall">Wall to deregister.</param>
        void DeregisterWall(Wall wall);

        // --- Ladders ---

        /// <summary>
        /// Computes valid ladder targets reachable from a source node/side.
        /// </summary>
        /// <param name="source">Source node.</param>
        /// <param name="side">Source side.</param>
        /// <returns>Ordered list of valid target nodes.</returns>
        List<BlockmapNode> GetPossibleLadderTargetNodes(BlockmapNode source, Direction side);

        /// <summary>
        /// Checks whether a ladder can be built from a node/side to a target node.
        /// </summary>
        /// <param name="from">Source node.</param>
        /// <param name="side">Source side.</param>
        /// <param name="to">Target node.</param>
        /// <returns>True if build is valid.</returns>
        bool CanBuildLadder(BlockmapNode from, Direction side, BlockmapNode to);

        /// <summary>
        /// Builds a ladder entity between nodes and optionally updates nearby systems.
        /// </summary>
        /// <param name="from">Source node.</param>
        /// <param name="to">Target node.</param>
        /// <param name="side">Source side.</param>
        /// <param name="updateWorld">If true, updates world systems around affected area.</param>
        void BuildLadder(BlockmapNode from, BlockmapNode to, Direction side, bool updateWorld);

        // --- Doors ---

        /// <summary>
        /// Checks whether a door can be built on a node side with a given height.
        /// </summary>
        /// <param name="node">Target node.</param>
        /// <param name="side">Node side.</param>
        /// <param name="height">Door height.</param>
        /// <returns>True if build is valid.</returns>
        bool CanBuildDoor(BlockmapNode node, Direction side, int height);

        /// <summary>
        /// Builds a door on a node side and optionally updates nearby systems.
        /// </summary>
        /// <param name="node">Target node.</param>
        /// <param name="side">Node side.</param>
        /// <param name="height">Door height.</param>
        /// <param name="isMirrored">If true, mirror geometry along side.</param>
        /// <param name="updateWorld">If true, updates world systems around the node.</param>
        /// <returns>The created door entity.</returns>
        Door BuildDoor(BlockmapNode node, Direction side, int height, bool isMirrored, bool updateWorld);

        // --- Zones & Rooms ---

        /// <summary>
        /// Creates and registers a zone, optionally providing vision and a visibility policy.
        /// </summary>
        /// <param name="coordinates">World coordinates (set) covered by the zone.</param>
        /// <param name="actor">Owning actor.</param>
        /// <param name="providesVision">If true, zone grants vision.</param>
        /// <param name="visibility">Zone visibility behavior.</param>
        /// <returns>The created zone.</returns>
        Zone AddZone(HashSet<Vector2Int> coordinates, Actor actor, bool providesVision, ZoneVisibility visibility);

        /// <summary>
        /// Creates and registers a room with nodes and interior walls.
        /// </summary>
        /// <param name="label">Room label.</param>
        /// <param name="nodes">Member nodes.</param>
        /// <param name="interiorWalls">Interior walls.</param>
        /// <returns>The created room.</returns>
        Room AddRoom(string label, List<BlockmapNode> nodes, List<Wall> interiorWalls);

        // --- Inventory ---

        /// <summary>
        /// Adds an entity to another entity's inventory and optionally updates nearby systems.
        /// </summary>
        /// <param name="item">Item entity to add.</param>
        /// <param name="holder">Inventory holder.</param>
        /// <param name="updateWorld">If true, updates world systems at the item's previous node.</param>
        void AddToInventory(Entity item, Entity holder, bool updateWorld);

        /// <summary>
        /// Drops an entity from inventory onto a node and optionally updates nearby systems.
        /// </summary>
        /// <param name="item">Item entity to drop.</param>
        /// <param name="newOriginNode">Target node for the drop.</param>
        /// <param name="updateWorld">If true, updates world systems around the target node.</param>
        void DropFromInventory(Entity item, BlockmapNode newOriginNode, bool updateWorld);
    }
}
