using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Networking
{
    [System.Serializable]
    public class NetworkAction_StartMatch : NetworkAction
    {
        public int MapSize;
        public int MapSeed;

        public NetworkAction_StartMatch(int size, int seed)
        {
            ActionType = "StartMatch";
            MapSize = size;
            MapSeed = seed;
        }
    }
}
