using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public abstract class Ability
    {
        public AbilityDef Def { get; private set; }
        public Creature Creature { get; private set; }
        public abstract string Label { get; }
        public abstract string Description { get; }
        public abstract int BaseCost { get; }


        private System.Action OnDoneCallback;

        public Ability() { }

        public void Init(AbilityDef def, Creature creature)
        {
            Def = def;
            Creature = creature;
        }

        public abstract HashSet<BlockmapNode> GetPossibleTargets();
        public abstract HashSet<BlockmapNode> GetImpactedNodes(BlockmapNode target);
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

        protected HashSet<BlockmapNode> AdjacentNodes
        {
            get
            {
                HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>();
                foreach (Direction dir in HelperFunctions.GetSides())
                {
                    GroundNode adjNode = Creature.World.GetAdjacentGroundNode(Creature.OriginNode, dir);
                    if (adjNode != null) nodes.Add(adjNode);
                }
                return nodes;
            }
        }

        protected HashSet<BlockmapNode> AdjacentNodesWithEnemies => AdjacentNodes.Where(n => n.Entities.Any(e => e is Creature otherCreature && otherCreature.Actor != Creature.Actor)).ToHashSet();
    }
}
