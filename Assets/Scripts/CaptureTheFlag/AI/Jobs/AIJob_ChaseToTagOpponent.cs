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
        private bool StopChasing;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.ChaseAndTagOpponent;
        public override string DevmodeDisplayText => "Tagging opponent (" + Target.LabelCap + ")";

        public AIJob_ChaseToTagOpponent(CtfCharacter c, CtfCharacter target) : base(c)
        {
            Target = target;
        }

        public override void OnCharacterStartsTurn()
        {
            // Only at the beginning of the turn, stop chasing if opponent is no longer visible
            Log($"Stop chasing {Target.LabelCap} because they moved out of vision.");
            if (!Target.IsVisibleByOpponent) StopChasing = true;
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If the closest target to chase/search is someone else, go chase/search that character
            float currentCostToTarget = GetPathCost(Target.Node);
            if (ShouldChaseOrSearchOpponent(currentCostToTarget, out CtfCharacter target, out AICharacterJobId jobId, out float costToTarget))
            {
                if (target != Target)
                {
                    if (jobId == AICharacterJobId.ChaseAndTagOpponent)
                    {
                        Log($"Switching from {Id} to ChaseToTagOpponent with another target because {target.LabelCap} is closer than current target (current = {currentCostToTarget}, new = {costToTarget}).");
                        return new AIJob_ChaseToTagOpponent(Character, target);
                    }
                    else if (jobId == AICharacterJobId.SearchOpponentInOwnTerritory)
                    {
                        Log($"Switching from {Id} to SearchOpponentInOwnTerritory because {target.LabelCap} was seen closer than current target (current = {currentCostToTarget}, new = {costToTarget}).");
                        return new AIJob_SearchOpponentInOwnTerritory(Character, target);
                    }
                    else throw new System.Exception($"id {jobId} not handled.");
                }
            }

            // If we should stop chasing, find a new job depending on game state
            if (StopChasing || ShouldStopChase())
            {
                Log($"Switching from {Id} to a general non-urgent job because we no longer need to chase {Target.LabelCap}.");
                return GetNewNonUrgentJob();
            }

            // Continue chase
            return this;
        }

        private bool ShouldStopChase()
        {
            if (Target.IsInJail) return true;
            if (!Target.IsInOpponentTerritory) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(Target.OriginNode);
        }
    }
}
