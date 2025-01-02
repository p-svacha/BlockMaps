using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    /// <summary>
    /// The job that every character gets assigned to when the match starts.
    /// </summary>
    public class AIJob_InitialJob : AICharacterJob
    {
        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.Initial;
        public override string DevmodeDisplayText => "Initial";

        public AIJob_InitialJob(CtfCharacter c) : base(c) { }

        public override AICharacterJob GetJobForNextAction()
        {
            Log($"Switching from {Id} to a general non-urgent job.");
            return GetNewNonUrgentJob();
        }

        public override CharacterAction GetNextAction()
        {
            return null;
        }
    }
}
