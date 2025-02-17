using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class DamageInfo
    {
        public Creature Source { get; private set; }
        public Creature Target { get; private set; }
        public DamageTypeDef Type { get; private set; }
        public float Amount { get; private set; }

        public DamageInfo(Creature source, Creature target, DamageTypeDef type, float amount)
        {
            Source = source;
            Target = target;
            Type = type;
            Amount = amount;
        }
    }
}
