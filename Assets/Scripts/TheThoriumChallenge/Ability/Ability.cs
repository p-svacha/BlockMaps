using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class Ability
    {
        public abstract string Label { get; }
        public abstract string Description { get; }
        public abstract int Cost { get; }
    }
}
