using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    /// <summary>
    /// Job to chase an opponent character in order to tag them.
    /// </summary>
    public class AIJob_ChaseToTagOpponent : AICharacterJob
    {
        private CtfCharacter Target;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.ChaseAndTagOpponent;
        public override string DevmodeDisplayText => "Tagging opponent (" + Target.LabelCap + ")";

        public AIJob_ChaseToTagOpponent(CtfCharacter c, CtfCharacter target) : base(c)
        {
            Target = target;
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we should stop chasing, find a new job depending on game state
            if (ShouldStopChase())
            {
                switch(Player.Roles[Character])
                {
                    // If we are defender, patrol flag
                    case AIPlayer.AICharacterRole.Defender:
                        Log($"Switching from {Id} to PatrolDefendFlag because we no longer need to chase {Target.LabelCap} and we are a defender.");
                        return new AIJob_PatrolDefendFlag(Character);

                    // If we are attacker, attack
                    case AIPlayer.AICharacterRole.Attacker:
                        if (IsEnemyFlagExplored)
                        {
                            Log($"Switching from {Id} to CaptureOpponentFlag because we no longer need to chase {Target.LabelCap} and we know where enemy flag is.");
                            return new AIJob_CaptureOpponentFlag(Character);
                        }

                        // Else chose a random unexplored node in enemy territory to go to
                        else
                        {
                            Log($"Switching from {Id} to SearchForOpponentFlag because we no longer need to chase {Target.LabelCap} and we don't know where enemy flag is.");
                            return new AIJob_SearchOpponentFlag(Character);
                        }

                    default:
                        throw new System.Exception($"Role {Player.Roles[Character]} not handled.");
                }
            }

            // Continue chase
            return this;
        }

        private bool ShouldStopChase()
        {
            if (Target.IsInJail) return true;
            if (!Target.IsInOpponentTerritory) return true;
            if (!Target.IsVisibleByOpponent) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(Target.OriginNode);
        }
    }
}
