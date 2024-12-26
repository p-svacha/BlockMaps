using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public enum MatchState
    {
        GeneratingWorld,
        InitializingWorld,
        MatchReadyToStart, // only used in multiplayer
        PlayerTurn,
        WaitingForOtherPlayerTurn, // only used in multiplayer
        NpcTurn,
        GameFinished
    }
}
