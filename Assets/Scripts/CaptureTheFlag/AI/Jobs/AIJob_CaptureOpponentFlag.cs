using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_CaptureOpponentFlag : AICharacterJob
    {
        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.CaptureOpponentFlag;
        public override string DevmodeDisplayText => "Capturing Flag";

        public AIJob_CaptureOpponentFlag(Character c) : base(c) { }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;
            return false;
        }

        public override CharacterAction GetNextAction()
        {
            if (Character.PossibleMoves.Count <= 4) return null;
            return GetMovementDirectlyTo(Character.Opponent.Flag.OriginNode);
        }
    }
}