using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// A specific stat of a specific character.
    /// </summary>
    public class Stat
    {
        public StatDef Def { get; private set; }
        public float BaseValue { get; private set; }

        public Stat(StatDef def, float baseValue)
        {
            if (def.Type != StatType.Binary && baseValue > def.MaxValue) throw new System.Exception($"Can't create stat with def {def.DefName} and base value {baseValue} because it is greater than the max value of that stat ({def.MaxValue}).");

            Def = def;
            BaseValue = baseValue;
        }

        /// <summary>
        /// The current value of this stat.
        /// </summary>
        public float GetValue()
        {
            float value = BaseValue;

            if (Def.Type == StatType.Int) value = Mathf.RoundToInt(value);
            if (Def.Type == StatType.Binary)
            {
                if (value != 0 && value != 1) throw new System.Exception($"Invalid value for stat with Def {Def.DefName}. Stat type is binary (0/1) but value is {value}.");
            }

            return value;
        }
    }
}
