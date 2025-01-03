using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public enum MatchState
    {
        Loading_GeneratingWorld,
        Loading_InitializingWorld,
        Loading_CreatingCtfObjects,
        MatchReadyToStart, // only used in multiplayer
        CountdownBeforePlayerTurn, // only used in multiplayer
        PlayerTurn,
        WaitingForOtherPlayerTurn, // only used in multiplayer
        NpcTurn,
        GameFinished
    }
}
