using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TtcLevelGenerator_Forest : WorldGenerator
    {
        public override string Label => "TTC - Forest";
        public override string Description => throw new NotImplementedException();
        public override bool StartAsVoid => false;

        protected override List<Action> GetGenerationSteps()
        {
            return new List<Action>()
            {
                PlaceTrees,
                PlaceCreatures
            };
        }

        private void PlaceTrees()
        {
        }

        private void PlaceCreatures()
        {
            EntitySpawner.TrySpawnEntity(new EntitySpawnProperties(World)
            {
                Def = CreatureDefOf.Needlegrub,
                PositionProperties = new EntitySpawnPositionProperties_WithinArea(0, World.NumNodesPerSide, 0, World.NumNodesPerSide),
                Actor = World.GetActor(1),
            });
        }
    }
}
