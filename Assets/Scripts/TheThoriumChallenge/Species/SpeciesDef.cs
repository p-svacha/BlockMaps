using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class SpeciesDef : EntityDef
    {
        public float MaxHpPerLevel { get; init; }
        public float MovementSpeedModifier { get; init; }
        public int CreatureHeight { get; init; }
        public float BiteStrengthPerLevel { get; init; }

        public Dictionary<SpeciesStatDef, float> Stats { get; private set; }


        public SpeciesDef() { }
        public SpeciesDef(SpeciesDef orig) : base(orig)
        {
            MaxHpPerLevel = orig.MaxHpPerLevel;
            MovementSpeedModifier = orig.MovementSpeedModifier;
        }

        public override void OnLoadingDefsDone()
        {
            Stats = new Dictionary<SpeciesStatDef, float>()
            {
                { SpeciesStatDefOf.MaxHP, MaxHpPerLevel },
                { SpeciesStatDefOf.VisionRange, VisionRange },
                { SpeciesStatDefOf.MovementSpeed, MovementSpeedModifier },
                { SpeciesStatDefOf.BiteStrength, BiteStrengthPerLevel },
            };
        }
    }
}
