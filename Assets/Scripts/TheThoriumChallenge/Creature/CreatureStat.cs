using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    /// <summary>
    /// Creature stats are the attributes of creatures that are relevant during the simulation for movement, vision, abilities, inflicting and receiving damage, and more.
    /// </summary>
    public class CreatureStat : Stat
    {
        new public CreatureStatDef Def => (CreatureStatDef)base.Def;
        public Creature Creature => (Creature)Entity;
        protected override float BaseValue => Creature.SpeciesDef.Stats[BaseValueSpeciesStat];

        // Helper
        private SpeciesStatDef BaseValueSpeciesStat;

        public override void Initialize(StatDef def, Entity entity)
        {
            base.Initialize(def, entity);

            BaseValueSpeciesStat = DefDatabase<SpeciesStatDef>.AllDefs.First(x => x.DefName == Def.DefName);
        }

        public override float GetValue()
        {
            // Base value from species
            float value = BaseValue;

            // Level scaling
            if (Def.ScalesWithLevel) value *= Creature.Level;

            // Type checks
            if (Def.Type == StatType.Int) value = (int)(value);
            if (Def.Type == StatType.Binary)
            {
                if (value != 0 && value != 1) throw new System.Exception($"Invalid value for stat with Def {Def.DefName}. Stat type is binary (0/1) but value is {value}.");
            }

            // Return
            return value;
        }

        public string GetBreakdownString()
        {
            // Label
            string text = Def.Description;
            if (text != "") text += "\n\n";

            // Base value
            float baseValue = Def.ScalesWithLevel ? BaseValue * Creature.Level : BaseValue;
            text += $"Base Value:  {GetValueText(baseValue)}";

            //From species
            text += $"\n\tFrom species: {GetValueText(BaseValue)}";

            // Level scaling
            if (Def.ScalesWithLevel) text += $"\n\tMultiplied by Level: x{GetValueText(BaseValue)}";

            // Final value
            text += $"\n\nFinal Value: {GetValueText()}";

            return text;
        }
    }
}
