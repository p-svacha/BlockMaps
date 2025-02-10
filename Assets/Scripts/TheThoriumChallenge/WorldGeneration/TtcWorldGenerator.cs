using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class TtcWorldGenerator : WorldGenerator
    {
        public abstract Biome Biome { get; }
        protected Vector2Int EntryPoint;
        protected Dictionary<Vector2Int, Biome> ExitPoints;
        protected List<CreatureInfo> PlayerCreatures;

        public void StartLevelGeneration(List<CreatureInfo> playerCreatures, System.Action onDoneCallback)
        {
            PlayerCreatures = playerCreatures;
            StartGeneration(2, WorldGenerator.GetRandomSeed(), onDoneCallback);
        }

        public Level GetLevel(Game game)
        {
            return new Level(game, World, EntryPoint, ExitPoints);
        }

        protected void PlacePlayerCreatures()
        {
            foreach(CreatureInfo info in PlayerCreatures)
            {
                Creature playerCreature = EntitySpawner.TrySpawnEntity(new EntitySpawnProperties(World)
                {
                    Def = info.Def,
                    PositionProperties = new EntitySpawnPositionProperties_AsCloseToNodeAsPossible(World.GetGroundNode(EntryPoint)),
                    Actor = World.GetActor(1),
                }, maxAttempts: 50) as Creature;

                if (playerCreature == null) throw new System.Exception("Failed to spawn player creature.");
                playerCreature.InitializeCreature(info.Level, isPlayerControlled: true);
            }
        }
    }
}
