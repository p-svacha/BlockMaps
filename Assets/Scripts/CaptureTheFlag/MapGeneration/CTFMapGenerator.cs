using BlockmapFramework;
using BlockmapFramework.Profiling;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// Base map generator that all CTF map generator should use. Provides general CTF game functionality.
    /// </summary>
    public abstract class CTFMapGenerator : WorldGenerator
    {
        // Rules
        private const int SPAWN_MAP_EDGE_OFFSET = 10;
        private const int SPAWN_VARIATION = 5;
        private const int NUM_HUMANS_PER_PLAYER = 6;
        private const int NUM_DOGS_PER_PLAYER = 2;

        // Generation
        private int CurrentGenerationStep;
        protected List<System.Action> GenerationSteps;
        
        private Actor LocalPlayer;
        private Actor Opponent;

        protected override void OnGenerationStart()
        {
            CurrentGenerationStep = 0;
            LocalPlayer = World.GetActor(1);
            Opponent = World.GetActor(2);
        }
        protected override void OnUpdate()
        {
            if (CurrentGenerationStep == GenerationSteps.Count)
            {
                FinalizeGeneration();
                return;
            }

            Debug.Log("Starting World Generation Step: " + GenerationSteps[CurrentGenerationStep].Method.Name);
            Profiler.Begin(GenerationSteps[CurrentGenerationStep].Method.Name);
            GenerationSteps[CurrentGenerationStep].Invoke();
            Profiler.End(GenerationSteps[CurrentGenerationStep].Method.Name);
            CurrentGenerationStep++;
        }

        /// <summary>
        /// Generates the bases for both players
        /// </summary>
        protected void CreatePlayerBases()
        {
            int p1X = SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(LocalPlayer, p1X, Direction.E);

            int p2X = WorldSize - SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(Opponent, p2X, Direction.W);
        }
        /// <summary>
        /// Spawns the flag, characters and jail zone for a player.
        /// </summary>
        private void CreatePlayerSpawn(Actor player, int spawnX, Direction faceDirection)
        {
            // Position
            int spawnY = Random.Range(SPAWN_MAP_EDGE_OFFSET, WorldSize - SPAWN_MAP_EDGE_OFFSET);
            Vector2Int spawnAreaCenter = new Vector2Int(spawnX, spawnY);

            // Flag
            Entity spawnedFlag = SpawnEntityOnGroundAround(EntityDefOf.Flag, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSide());
            int numAttempts = 0;
            while(spawnedFlag == null && numAttempts++ < 50) // Keep searching if first position wasn't valid (i.e. occupied by a tree)
            {
                spawnY = Random.Range(SPAWN_MAP_EDGE_OFFSET, WorldSize - SPAWN_MAP_EDGE_OFFSET);
                spawnAreaCenter = new Vector2Int(spawnX, spawnY);
                spawnedFlag = SpawnEntityOnGroundAround(EntityDefOf.Flag, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSide());
            }
            
            // Jail zone
            HashSet<Vector2Int> jailZoneCoords = new HashSet<Vector2Int>();
            int flagDistanceX, flagDistanceY;
            numAttempts = 0;
            do
            {
                flagDistanceX = Random.Range(CtfMatch.JAIL_ZONE_MIN_FLAG_DISTANCE, CtfMatch.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                flagDistanceY = Random.Range(CtfMatch.JAIL_ZONE_MIN_FLAG_DISTANCE, CtfMatch.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                if (Random.value < 0.5f) flagDistanceX *= -1;
                if (Random.value < 0.5f) flagDistanceY *= -1;
            }
            while (numAttempts++ < 10 && (spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX < World.MinX + CtfMatch.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX > World.MaxX - CtfMatch.JAIL_ZONE_RADIUS - 1 ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY < World.MinY + CtfMatch.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY > World.MaxY - CtfMatch.JAIL_ZONE_RADIUS - 1));

            Vector2Int jailZoneCenter = spawnedFlag.OriginNode.WorldCoordinates + new Vector2Int(flagDistanceX, flagDistanceY);
            for (int x = -(CtfMatch.JAIL_ZONE_RADIUS - 1); x < CtfMatch.JAIL_ZONE_RADIUS; x++)
            {
                for (int y = -(CtfMatch.JAIL_ZONE_RADIUS - 1); y < CtfMatch.JAIL_ZONE_RADIUS; y++)
                {
                    jailZoneCoords.Add(jailZoneCenter + new Vector2Int(x, y));
                }
            }
            World.AddZone(jailZoneCoords, player, providesVision: false, ZoneVisibility.VisibleForOwner);

            // Flag zone
            HashSet<Vector2Int> flagZoneCoords = new HashSet<Vector2Int>();
            for(int x = -(int)(CtfMatch.FLAG_ZONE_RADIUS + 1); x <= CtfMatch.FLAG_ZONE_RADIUS + 1; x++)
            {
                for (int y = -(int)(CtfMatch.FLAG_ZONE_RADIUS + 1); y <= CtfMatch.FLAG_ZONE_RADIUS + 1; y++)
                {
                    Vector2Int offsetCoords = new Vector2Int(x, y);
                    float distance = offsetCoords.magnitude;
                    if(distance <= CtfMatch.FLAG_ZONE_RADIUS)
                    {
                        flagZoneCoords.Add(spawnedFlag.OriginNode.WorldCoordinates + offsetCoords);
                    }
                }
            }
            Zone flagZone = World.AddZone(flagZoneCoords, player, providesVision: true, ZoneVisibility.VisibleForOwner);

            // Humans
            int humansSpawned = 0;
            numAttempts = 0;
            while(humansSpawned < NUM_HUMANS_PER_PLAYER && numAttempts++ < 10)
            {
                Entity spawnedCharacter = SpawnEntityOnGroundAround(EntityDefOf.Human, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes);
                if(spawnedCharacter != null) humansSpawned++;
            }

            // Dogs
            int dogsSpawned = 0;
            numAttempts = 0;
            while (dogsSpawned < NUM_DOGS_PER_PLAYER && numAttempts++ < 10)
            {
                Entity spawnedCharacter = SpawnEntityOnGroundAround(EntityDefOf.Dog, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes);
                if (spawnedCharacter != null) dogsSpawned++;
            }
        }


        /// <summary>
        /// Creates both player zones and the neutral zone in between.
        /// </summary>
        protected void CreateMapZones()
        {
            int neutralZoneSize = (int)(WorldSize * CtfMatch.NEUTRAL_ZONE_SIZE);
            int playerZoneSize = (WorldSize / 2) - (neutralZoneSize / 2);
            HashSet<Vector2Int> ownZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> neutralZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> opponentZoneNodes = new HashSet<Vector2Int>();
            foreach (BlockmapNode node in World.GetAllGroundNodes())
            {
                if (node.WorldCoordinates.x < playerZoneSize) ownZoneNodes.Add(node.WorldCoordinates);
                else if (node.WorldCoordinates.x < playerZoneSize + neutralZoneSize) neutralZoneNodes.Add(node.WorldCoordinates);
                else opponentZoneNodes.Add(node.WorldCoordinates);
            }
            World.AddZone(ownZoneNodes, LocalPlayer, providesVision: false, ZoneVisibility.VisibleForOwner); // id = 0: Blue player territory
            World.AddZone(neutralZoneNodes, World.Gaia, providesVision: false, ZoneVisibility.VisibleForEveryone); // id = 1: Neutral territory
            World.AddZone(opponentZoneNodes, Opponent, providesVision: false, ZoneVisibility.VisibleForOwner);// id = 2: Red player territory
        }
    }
}
