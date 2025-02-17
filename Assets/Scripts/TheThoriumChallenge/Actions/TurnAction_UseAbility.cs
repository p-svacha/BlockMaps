using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TurnAction_UseAbility : TurnAction
    {
        public BlockmapNode Target { get; private set; }
        public Ability Ability { get; private set; }

        public TurnAction_UseAbility(Creature creature, BlockmapNode target, Ability ability) : base(creature)
        {
            Target = target;
            Ability = ability;
        }

        public override int GetCost()
        {
            return Ability.GetCost(Target);
        }

        protected override void OnPerformAction()
        {
            Debug.Log($"{Creature} is using the ability {Ability.Label}.");
            Ability.Perform(Target, onDoneCallback: EndAction);
        }
    }
}
