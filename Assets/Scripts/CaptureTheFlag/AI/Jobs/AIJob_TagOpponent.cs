using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_TagOpponent : AICharacterJob
    {
        private Character Target;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.TagOpponent;
        public override string DevmodeDisplayText => "Tagging opponent (" + Target.Name + ")";

        public AIJob_TagOpponent(Character c, Character target) : base(c)
        {
            Target = target;
        }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            if (Target.IsInJail) return true;
            if (!Target.IsInOpponentZone) return true;
            if (!Target.IsVisibleByOpponent) return true;

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            if (Character.PossibleMoves.Count <= 4) return null;
            return GetMovementDirectlyTo(Target.Entity.OriginNode);
        }
    }
}
