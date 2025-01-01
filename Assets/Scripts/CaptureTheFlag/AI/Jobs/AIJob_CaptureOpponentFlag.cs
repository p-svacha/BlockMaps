using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_CaptureOpponentFlag : AICharacterJob
    {
        private NavigationPath TargetPath;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.CaptureOpponentFlag;
        public override string DevmodeDisplayText => "Capturing Flag";

        public AIJob_CaptureOpponentFlag(CtfCharacter c) : base(c)
        {
            RecalculateTargetPath();
        }

        public override void OnNextActionRequested()
        {
            // Recalculate target path if no longer valid
            bool isTargetPathStillValid = (TargetPath != null && TargetPath.CanPass(Character));
            if (!isTargetPathStillValid) RecalculateTargetPath();
        }

        private void RecalculateTargetPath()
        {
            TargetPath = GetPath(Opponent.Flag.OriginNode);
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we can tag an opponent directly this turn, do that
            if (CanTagCharacterDirectly(out CtfCharacter target0))
            {
                Log($"Switching from {Id} to ChaseToTagOpponent because we can reach {target0.LabelCap} directly this turn.");
                return new AIJob_ChaseToTagOpponent(Character, target0);
            }

            // Check if we should flee
            if (ShouldFlee())
            {
                Log($"Switching from {Id} to Flee because we need to flee.");
                return new AIJob_Flee(Character);
            }

            // Keep capturing
            return this;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(Opponent.Flag.OriginNode, TargetPath);
        }
    }
}