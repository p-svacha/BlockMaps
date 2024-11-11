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

            // If we can tag an opponent this turn, do that
            if (Player.CanTagCharacterDirectly(Character, out Character target0))
            {
                forcedNewJob = new AIJob_TagOpponent(Character, target0);
                return true;
            }

            // If we should flee, do that
            if (Player.ShouldFlee(Character))
            {
                forcedNewJob = new AIJob_Flee(Character);
                return true;
            }

            return false;
        }

        public override CharacterAction GetNextAction()
        {
            return GetMovementTo(Character.Opponent.Flag.OriginNode);
        }
    }
}