using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class Ability
    {
        public Creature Creature { get; private set; }
        public abstract string Label { get; }
        public abstract string Description { get; }
        public abstract int BaseCost { get; }


        private System.Action OnDoneCallback;

        public Ability() { }

        public void Init(Creature creature)
        {
            Creature = creature;
        }

        public abstract List<BlockmapNode> GetPossibleTargets();
        public abstract int GetCost(BlockmapNode target);
        public abstract void OnPerform(BlockmapNode target);
        public void Perform(BlockmapNode target, System.Action onDoneCallback)
        {
            OnDoneCallback = onDoneCallback;
            OnPerform(target);
        }

        protected void Finish()
        {
            OnDoneCallback.Invoke();
        }

        protected GroundNode OriginNode => (GroundNode)Creature.OriginNode;
    }
}
