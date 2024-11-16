using BlockmapFramework;
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
        public const string FLAG_ID = "flag";

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
            GenerationSteps[CurrentGenerationStep].Invoke();
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
            Entity flagPrefab = GetEntityPrefab(FLAG_ID);
            Entity spawnedFlag = SpawnEntityOnGroundAround(flagPrefab, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSideDirection());
            int numAttempts = 0;
            while(spawnedFlag == null && numAttempts++ < 50) // Keep searching if first position wasn't valid (i.e. occupied by a tree)
            {
                spawnY = Random.Range(SPAWN_MAP_EDGE_OFFSET, WorldSize - SPAWN_MAP_EDGE_OFFSET);
                spawnAreaCenter = new Vector2Int(spawnX, spawnY);
                spawnedFlag = SpawnEntityOnGroundAround(flagPrefab, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSideDirection());
            }
            
            // Jail zone
            HashSet<Vector2Int> jailZoneCoords = new HashSet<Vector2Int>();
            int flagDistanceX, flagDistanceY;
            do
            {
                flagDistanceX = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                flagDistanceY = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                if (Random.value < 0.5f) flagDistanceX *= -1;
                if (Random.value < 0.5f) flagDistanceY *= -1;
            }
            while (spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX < World.MinX + CTFGame.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX > World.MaxX - CTFGame.JAIL_ZONE_RADIUS - 1 ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY < World.MinY + CTFGame.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY > World.MaxY - CTFGame.JAIL_ZONE_RADIUS - 1);

            Vector2Int jailZoneCenter = spawnedFlag.OriginNode.WorldCoordinates + new Vector2Int(flagDistanceX, flagDistanceY);
            for (int x = -(CTFGame.JAIL_ZONE_RADIUS - 1); x < CTFGame.JAIL_ZONE_RADIUS; x++)
            {
                for (int y = -(CTFGame.JAIL_ZONE_RADIUS - 1); y < CTFGame.JAIL_ZONE_RADIUS; y++)
                {
                    jailZoneCoords.Add(jailZoneCenter + new Vector2Int(x, y));
                }
            }
            World.AddZone(jailZoneCoords, player, providesVision: false, showBorders: true);

            // Flag zone
            HashSet<Vector2Int> flagZoneCoords = new HashSet<Vector2Int>();
            for(int x = -(int)(CTFGame.FLAG_ZONE_RADIUS + 1); x <= CTFGame.FLAG_ZONE_RADIUS + 1; x++)
            {
                for (int y = -(int)(CTFGame.FLAG_ZONE_RADIUS + 1); y <= CTFGame.FLAG_ZONE_RADIUS + 1; y++)
                {
                    Vector2Int offsetCoords = new Vector2Int(x, y);
                    float distance = offsetCoords.magnitude;
                    if(distance <= CTFGame.FLAG_ZONE_RADIUS)
                    {
                        flagZoneCoords.Add(spawnedFlag.OriginNode.WorldCoordinates + offsetCoords);
                    }
                }
            }
            Zone flagZone = World.AddZone(flagZoneCoords, player, providesVision: true, showBorders: true);

            // Humans
            int humansSpawned = 0;
            Entity humanPrefab = GetCharacterPrefab("human");
            while(humansSpawned < NUM_HUMANS_PER_PLAYER)
            {
                Entity spawnedCharacter = SpawnEntityOnGroundAround(humanPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes);
                if(spawnedCharacter != null) humansSpawned++;
            }

            // Dogs
            int dogsSpawned = 0;
            Entity dogPrefab = GetCharacterPrefab("dog");
            while (dogsSpawned < NUM_DOGS_PER_PLAYER)
            {
                Entity spawnedCharacter = SpawnEntityOnGroundAround(dogPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes);
                if (spawnedCharacter != null) dogsSpawned++;
            }
        }


        /// <summary>
        /// Creates both player zones and the neutral zone in between.
        /// </summary>
        protected void CreateMapZones()
        {
            int neutralZoneSize = (int)(World.Dimensions.x * CTFGame.NEUTRAL_ZONE_SIZE);
            int playerZoneSize = (World.Dimensions.x / 2) - (neutralZoneSize / 2);
            HashSet<Vector2Int> ownZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> neutralZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> opponentZoneNodes = new HashSet<Vector2Int>();
            foreach (BlockmapNode node in World.GetAllGroundNodes())
            {
                if (node.WorldCoordinates.x < playerZoneSize) ownZoneNodes.Add(node.WorldCoordinates);
                else if (node.WorldCoordinates.x < playerZoneSize + neutralZoneSize) neutralZoneNodes.Add(node.WorldCoordinates);
                else opponentZoneNodes.Add(node.WorldCoordinates);
            }
            Zone localPlayerZone = World.AddZone(ownZoneNodes, LocalPlayer, providesVision: false, showBorders: true);
            Zone neutralZone = World.AddZone(neutralZoneNodes, World.Gaia, providesVision: false, showBorders: true);
            Zone opponentZone = World.AddZone(opponentZoneNodes, Opponent, providesVision: false, showBorders: true);
        }

        private Entity GetCharacterPrefab(string id)
        {
            string fullPath = "CaptureTheFlag/Characters/" + id;
            Entity prefab = Resources.Load<Entity>(fullPath);
            return prefab;
        }
    }
}
