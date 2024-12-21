using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_Idle : AICharacterJob
    {
        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.Idle;
        public override string DevmodeDisplayText => "Idle";

        public AIJob_Idle(CTFCharacter c) : base(c) { }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;
            return true;
        }

        public override CharacterAction GetNextAction()
        {
            return null;
        }
    }
}
