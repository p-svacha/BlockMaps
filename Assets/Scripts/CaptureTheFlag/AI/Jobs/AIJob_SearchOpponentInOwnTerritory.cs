using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIJob_SearchOpponentInOwnTerritory : AICharacterJob
    {
        private float CHASE_DISTANCE = 15;
        
        private CtfCharacter TargetCharacter;
        private BlockmapNode TargetNode;
        private NavigationPath TargetPath;
        private bool StopSearch;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.SearchOpponentInOwnTerritory;
        public override string DevmodeDisplayText => $"Searching {TargetCharacter.LabelCap}'s last known position: {TargetNode}";

        public AIJob_SearchOpponentInOwnTerritory(CtfCharacter c, CtfCharacter targetCharacter) : base(c)
        {
            TargetCharacter = targetCharacter;
            TargetNode = Player.OpponentPositionsToCheckForDefense[targetCharacter];

            // Remove flag that this position should be checked but this job does it
            Player.UnmarkOpponentCharactersLastPositionToBeChecked(TargetCharacter);

            RecalculateTargetPath();
        }

        public override void OnNextActionRequested()
        {
            // Recalculate target path if no longer valid
            bool isTargetPathStillValid = (TargetPath != null && TargetPath.CanPass(Character));
            if (!isTargetPathStillValid) RecalculateTargetPath();
        }

        public override AICharacterJob GetJobForNextAction()
        {
            // If we can tag an opponent directly this turn, do that
            if (CanTagCharacterDirectly(out CtfCharacter target0))
            {
                Log($"Switching from {Id} to ChaseToTagOpponent because we can reach {target0.LabelCap} directly this turn.");
                return new AIJob_ChaseToTagOpponent(Character, target0);
            }

            // If there is an opponent nearby in our own territory, go chase them
            foreach (CtfCharacter opp in VisibleOpponentCharactersNotInJail)
            {
                if (!opp.IsInOpponentTerritory) continue;
                if (Character.IsInRange(opp.Node, CHASE_DISTANCE, out float cost))
                {
                    Log($"Switching from {Id} to ChaseToTagOpponent because {opp.LabelCap} is nearby (distance = {cost}).");
                    return new AIJob_ChaseToTagOpponent(Character, opp);
                }
            }

            // Find a new job if we should stop search
            if (ShouldStopSearch())
            {
                Log($"Switching from {Id} to a general non-urgent job because we no longer need to search for {TargetCharacter.LabelCap}.");
                return GetNewNonUrgentJob();
            }

            return this;
        }

        public override CharacterAction GetNextAction()
        {
            return GetSingleNodeMovementTo(TargetNode, TargetPath);
        }

        private void RecalculateTargetPath()
        {
            TargetPath = GetPath(TargetNode);
        }

        private bool ShouldStopSearch()
        {
            if (IsOnOrNear(TargetNode)) return true;
            if (TargetCharacter.IsVisible) return true;
            if (TargetCharacter.IsInJail) return true;

            return false;
        }
    }
}
