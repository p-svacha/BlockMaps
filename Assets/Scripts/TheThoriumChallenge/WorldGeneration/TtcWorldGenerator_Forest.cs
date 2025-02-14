using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TtcWorldGenerator_Forest : TtcWorldGenerator
    {
        public override string Label => "TTC - Forest";
        public override string Description => throw new NotImplementedException();
        public override bool StartAsVoid => false;
        public override Biome Biome => Biome.Forest;

        protected override List<Action> GetGenerationSteps()
        {
            return new List<Action>()
            {
                PlaceTrees,
                PlacePlayerCreatures,
                PlaceHostileCreatures,
            };
        }

        private void PlaceTrees()
        {
        }

        private void PlaceHostileCreatures()
        {
            Creature creature = EntitySpawner.TrySpawnEntity(new EntitySpawnProperties(World)
            {
                Def = SpeciesDefOf.Squishgrub,
                PositionProperties = new EntitySpawnPositionProperties_WithinArea(0, World.NumNodesPerSide, 0, World.NumNodesPerSide),
                Actor = World.GetActor(2),
            }) as Creature;
            if(creature != null)
            {
                creature.InitializeCreature(level: 1, isPlayerControlled: false);
            }
        }
    }
}
