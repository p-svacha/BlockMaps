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
            switch (Player.Roles[Character])
            {
                case AIPlayer.AICharacterRole.Defender:
                    Log($"Switching from InitialJob to PatrolDefendFlag because we are a defender.");
                    return new AIJob_PatrolDefendFlag(Character);

                case AIPlayer.AICharacterRole.Attacker:
                    Log($"Switching from InitialJob to SearchForOpponentFlag because we are an attacker.");
                    return new AIJob_SearchOpponentFlag(Character);

                default:
                    throw new System.Exception($"Role {Player.Roles[Character]} not handled.");
            }
        }

        public override CharacterAction GetNextAction()
        {
            return null;
        }
    }
}
