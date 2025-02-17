using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TtcStageGenerator_Forest : TtcStageGenerator
    {
        public override string Label => "TTC - Forest";
        public override string Description => throw new System.NotImplementedException();
        public override bool StartAsVoid => false;
        public override Biome Biome => Biome.Forest;

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>()
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
            for(int i = 0; i < 10; i++)
            {
                TryPlaceRandomHostileCreature();
            }
        }

        private void TryPlaceRandomHostileCreature()
        {
            Creature creature = EntitySpawner.TrySpawnEntity(new EntitySpawnProperties(World)
            {
                Def = SpeciesDefOf.Squishgrub,
                PositionProperties = new EntitySpawnPositionProperties_WithinArea(0, World.NumNodesPerSide, 0, World.NumNodesPerSide),
                Actor = World.GetActor(2),
            }) as Creature;
            if (creature != null)
            {
                creature.InitializeCreature(level: Random.Range(10, 21), isPlayerControlled: false);
            }
        }
    }
}
