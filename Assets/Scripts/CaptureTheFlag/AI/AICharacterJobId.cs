using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public enum AICharacterJobId
    {
        Error,
        Initial,
        SearchOpponentFlag,
        CaptureOpponentFlag,
        ChaseAndTagOpponent,
        PatrolDefendFlag,
        Flee,
        LingerInNeutral,
        SearchOpponentInOwnTerritory,
        ExploreOwnTerritory
    }
}
