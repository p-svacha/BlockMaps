using BlockmapFramework;
using BlockmapFramework.Profiling;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// Contains functions to turn a world into a CTF map by creating all relevant zones, creating the flags, characters, etc.
    /// </summary>
    public static class CtfMapGenerator
    {
        // Flag spawn
        private const int MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE = 15; // The flag will spawn at least this amount of nodes away from any map edge
        private const float MAX_FLAG_MAP_EDGE_OFFSET_RELATIVE = 0.15f; // The flag will spawn at maximum this far away from the x-axis map edge, in % of map size

        // Character spawn
        private const int CHARACTER_SPAWN_VARIATION_AROUND_FLAG = 10; // When placed around flag
        private const float MIN_CHARACTER_SPAWN_X_RELATIVE = 0f; // How much backwards characters can spawn when spawned along full map width
        private const float MAX_CHARACTER_SPAWN_X_RELATIVE = 0.4f; // How much forwards characters can spawn when spawned along full map width
        private const int REQUIRED_SPAWN_ROAMING_AREA = 100; // Spawned characters need this many nodes to move around near them to be a valid spawn

        public const float NEUTRAL_ZONE_SIZE = 0.1f; // size of neutral zone strip in %
        public const int JAIL_ZONE_RADIUS = 3;
        public const int JAIL_ZONE_MIN_FLAG_DISTANCE = 9; // minimum distance from jail zone center to flag
        public const int JAIL_ZONE_MAX_FLAG_DISTANCE = 11; // maximum distance from jail zone center to flag
        public const float FLAG_ZONE_RADIUS = 7.5f;  // Amount of tiles around flag that can't be entered by own team

        /// <summary>
        /// List containing all EntityDefs of the characters that each player gets.
        /// </summary>
        private static List<EntityDef> CharacterRoster = new List<EntityDef>()
        {
            EntityDefOf.Human1,
            EntityDefOf.Human2,
            EntityDefOf.Human3,
            EntityDefOf.Human4,
            EntityDefOf.Human5,
            EntityDefOf.Human6,
            EntityDefOf.Dog1,
            EntityDefOf.Dog2,
        };

        private static World World;
        private static CharacterSpawnType SpawnType;
        private static Actor P1Actor;
        private static Actor P2Actor;
        private static List<Entity> CreatedEntities;

        /// <summary>
        /// Adds all CTF world objects (zones, flags, characters, etc.) to an existing, fully initialized world.
        /// <br/>When all objects are created and the vision of the placed characters is done being recalculated, a callback can be called.
        /// </summary>
        public static void CreateCtfMap(World world, CharacterSpawnType spawnType, System.Action onDoneCallback)
        {
            if (!world.IsInitialized) throw new System.Exception("World needs to be initialized");

            World = world;
            SpawnType = spawnType;

            CreatedEntities = new List<Entity>();
            P1Actor = world.GetActor(1);
            P2Actor = world.GetActor(2);

            CreateMapZones();
            CreatePlayerBases();

            // Recalculate vision
            world.UpdateEntityVisionDelayed(CreatedEntities, onDoneCallback);
        }

        /// <summary>
        /// Generates the bases for both players
        /// </summary>
        private static void CreatePlayerBases()
        {
            CreatePlayerSpawn(P1Actor, isPlayer1: true);
            CreatePlayerSpawn(P2Actor, isPlayer1: false);
        }

        private static int GetRandomFlagDistanceFromMapEdge()
        {
            int minValue = MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE;
            int maxValue = (int)(World.NumNodesPerSide * MAX_FLAG_MAP_EDGE_OFFSET_RELATIVE);
            maxValue = Mathf.Max(minValue, maxValue);
            Debug.Log($"Flag will spawn between {minValue} and {maxValue} nodes from map edge.");
            return Random.Range(minValue, maxValue + 1);
        }

        /// <summary>
        /// Spawns the flag, characters and jail zone for a player.
        /// </summary>
        private static void CreatePlayerSpawn(Actor player, bool isPlayer1)
        {
            int flagSpawnOffset = GetRandomFlagDistanceFromMapEdge();
            int spawnX = isPlayer1 ? flagSpawnOffset : World.NumNodesPerSide - flagSpawnOffset - 1;

            Direction faceDirection = isPlayer1 ? Direction.E : Direction.W;

            // Position
            int spawnY = Random.Range(MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE, World.NumNodesPerSide - MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE);
            Vector2Int spawnAreaCenter = new Vector2Int(spawnX, spawnY);

            // Flag
            Entity spawnedFlag = EntityManager.SpawnEntityAround(World, EntityDefOf.Flag, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSide());
            int numAttempts = 0;
            while(spawnedFlag == null && numAttempts++ < 50) // Keep searching if first position wasn't valid (i.e. occupied by a tree)
            {
                spawnY = Random.Range(MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE, World.NumNodesPerSide - MIN_FLAG_MAP_EDGE_OFFSET_ABSOLUTE);
                spawnAreaCenter = new Vector2Int(spawnX, spawnY);
                spawnedFlag = EntityManager.SpawnEntityAround(World, EntityDefOf.Flag, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSide());
            }
            CreatedEntities.Add(spawnedFlag);
            
            // Jail zone
            HashSet<Vector2Int> jailZoneCoords = new HashSet<Vector2Int>();
            int flagDistanceX, flagDistanceY;
            numAttempts = 0;
            do
            {
                flagDistanceX = Random.Range(JAIL_ZONE_MIN_FLAG_DISTANCE, JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                flagDistanceY = Random.Range(JAIL_ZONE_MIN_FLAG_DISTANCE, JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                if (Random.value < 0.5f) flagDistanceX *= -1;
                if (Random.value < 0.5f) flagDistanceY *= -1;
            }
            while (numAttempts++ < 10 && (spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX < World.MinX + JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX > World.MaxX - JAIL_ZONE_RADIUS - 1 ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY < World.MinY + JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY > World.MaxY - JAIL_ZONE_RADIUS - 1));

            Vector2Int jailZoneCenter = spawnedFlag.OriginNode.WorldCoordinates + new Vector2Int(flagDistanceX, flagDistanceY);
            for (int x = -(JAIL_ZONE_RADIUS - 1); x < JAIL_ZONE_RADIUS; x++)
            {
                for (int y = -(JAIL_ZONE_RADIUS - 1); y < JAIL_ZONE_RADIUS; y++)
                {
                    jailZoneCoords.Add(jailZoneCenter + new Vector2Int(x, y));
                }
            }
            World.AddZone(jailZoneCoords, player, providesVision: false, ZoneVisibility.VisibleForOwner);

            // Flag zone
            HashSet<Vector2Int> flagZoneCoords = new HashSet<Vector2Int>();
            for(int x = -(int)(FLAG_ZONE_RADIUS + 1); x <= FLAG_ZONE_RADIUS + 1; x++)
            {
                for (int y = -(int)(FLAG_ZONE_RADIUS + 1); y <= FLAG_ZONE_RADIUS + 1; y++)
                {
                    Vector2Int offsetCoords = new Vector2Int(x, y);
                    float distance = offsetCoords.magnitude;
                    if(distance <= FLAG_ZONE_RADIUS)
                    {
                        flagZoneCoords.Add(spawnedFlag.OriginNode.WorldCoordinates + offsetCoords);
                    }
                }
            }
            Zone flagZone = World.AddZone(flagZoneCoords, player, providesVision: true, ZoneVisibility.VisibleForOwner);

            // Character roster
            foreach(EntityDef characterDef in CharacterRoster)
            {
                if (SpawnType == CharacterSpawnType.AroundFlag)
                {
                    Entity spawnedCharacter = EntityManager.SpawnEntityAround(World, characterDef, player, spawnAreaCenter, CHARACTER_SPAWN_VARIATION_AROUND_FLAG, faceDirection, requiredRoamingArea: REQUIRED_SPAWN_ROAMING_AREA, forbiddenNodes: flagZone.Nodes);
                    if (spawnedCharacter == null) throw new System.Exception($"Failed to spawn character");
                    CreatedEntities.Add(spawnedCharacter);
                }
                else if (SpawnType == CharacterSpawnType.SpreadAlongFullMapWidth)
                {
                    int minXOffset = (int)(World.NumNodesPerSide * MIN_CHARACTER_SPAWN_X_RELATIVE);
                    int maxXOffset = (int)(World.NumNodesPerSide * MAX_CHARACTER_SPAWN_X_RELATIVE);
                    int minX = isPlayer1 ? minXOffset : World.NumNodesPerSide - maxXOffset - 1;
                    int maxX = isPlayer1 ? maxXOffset : World.NumNodesPerSide - minXOffset - 1;
                    int minY = 0;
                    int maxY = World.NumNodesPerSide - 1;

                    Entity spawnedCharacter = EntityManager.SpawnEntityWithin(World, characterDef, player, faceDirection, minX, maxX, minY, maxY, requiredRoamingArea: REQUIRED_SPAWN_ROAMING_AREA, forbiddenNodes: flagZone.Nodes);
                    if (spawnedCharacter == null) throw new System.Exception($"Failed to spawn character");
                    CreatedEntities.Add(spawnedCharacter);
                }
            }
        }
        /// <summary>
        /// Creates both player zones and the neutral zone in between.
        /// </summary>
        private static void CreateMapZones()
        {
            int neutralZoneSize = (int)(World.NumNodesPerSide * NEUTRAL_ZONE_SIZE);
            int playerZoneSize = (World.NumNodesPerSide / 2) - (neutralZoneSize / 2);
            HashSet<Vector2Int> ownZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> neutralZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> opponentZoneNodes = new HashSet<Vector2Int>();
            foreach (BlockmapNode node in World.GetAllGroundNodes())
            {
                if (node.WorldCoordinates.x < playerZoneSize) ownZoneNodes.Add(node.WorldCoordinates);
                else if (node.WorldCoordinates.x < playerZoneSize + neutralZoneSize) neutralZoneNodes.Add(node.WorldCoordinates);
                else opponentZoneNodes.Add(node.WorldCoordinates);
            }
            World.AddZone(ownZoneNodes, P1Actor, providesVision: false, ZoneVisibility.VisibleForEveryone); // id = 0: Blue player territory
            World.AddZone(neutralZoneNodes, World.Gaia, providesVision: false, ZoneVisibility.VisibleForEveryone); // id = 1: Neutral territory
            World.AddZone(opponentZoneNodes, P2Actor, providesVision: false, ZoneVisibility.VisibleForEveryone);// id = 2: Red player territory
        }
    }

    public enum CharacterSpawnType
    {
        /// <summary>
        /// All the characters of a player spawn somewhere around the flag.
        /// </summary>
        [Description("Around flag")]
        AroundFlag,

        /// <summary>
        /// All the characters of a player spawn completely spread out along the full y axis of the world.
        /// </summary>
        [Description("Along full map width")]
        SpreadAlongFullMapWidth
    }
}
