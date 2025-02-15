using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A measurable attribute that defines a characters performance or efficiency in various activities, tasks, or states.
    /// <br/>Stats numerical values calculated purely at runtime based on the state of various other systems (i.e. skills).
    /// </summary>
    public class Stat
    {
        /// <summary>
        /// The stats component this stat is attached to.
        /// </summary>
        private Comp_Stats Comp;

        /// <summary>
        /// The definition of this stat.
        /// </summary>
        public StatDef Def { get; private set; }

        /// <summary>
        /// The entity this stat is attached to.
        /// </summary>
        public Entity Entity { get; private set; }

        public Stat(Comp_Stats comp, StatDef def, Entity entity)
        {
            Comp = comp;
            Def = def;
            Entity = entity;
        }

        /// <summary>
        /// The current value of this stat.
        /// </summary>
        public virtual float GetValue()
        {
            float value = GetBaseValue();

            // Apply stat parts
            foreach(StatPart statPart in Def.StatParts)
            {
                statPart.TransformValue(Entity, this, ref value);
            }

            if (Def.Type == StatType.Int) value = (int)(value);
            if (Def.Type == StatType.Binary)
            {
                if (value != 0 && value != 1) throw new System.Exception($"Invalid value for stat with Def {Def.DefName}. Stat type is binary (0/1) but value is {value}.");
            }

            return value;
        }

        protected float GetBaseValue()
        {
            // Check if the EntityDef has overridden the base value for this StatDef
            if (Comp.Props.StatBases.TryGetValue(Def, out float overriddenBaseStat)) return overriddenBaseStat;

            // If not, just return the base value as defined in the StatDef
            return Def.BaseValue;
        }

        public string GetValueText() => GetValueText(GetValue());
        public string GetValueText(float value)
        {
            switch (Def.Type)
            {
                case StatType.Float:
                    return value.ToString("0.##");

                case StatType.Int:
                    return value.ToString();

                case StatType.Binary:
                    return value == 1 ? "Yes" : "No";

                case StatType.Percent:
                    return value.ToString("P0");
            }
            throw new System.Exception($"Type {Def.Type} not handled.");
        }

        public string GetBreakdownString()
        {
            // Label
            string text = Def.LabelCap;

            // Description
            if (Def.Description != "")
            {
                text += "\n\n" + Def.Description;
            }

            // Base value
            text += $"\n\nBase Value: {GetValueText(GetBaseValue())}";

            // Stat parts
            if (Def.StatParts.Count > 0) text += "\n";
            foreach (StatPart statPart in Def.StatParts)
            {
                text += $"\n{statPart.ExplanationString(Entity, this)}";
            }

            // Final value
            text += $"\n\nFinal Value: {GetValueText(GetValue())}";

            return text;
        }
    }
}
