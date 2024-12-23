using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkMessage_InitializeMultiplayerMatch : NetworkMessage
    {
        public int MapSize;
        public int MapSeed;
        public string Player1ClientId;
        public string Player2ClientId;

        public NetworkMessage_InitializeMultiplayerMatch(int size, int seed, string p1Id, string p2Id) : base("InitializeMultiplayerMatch")
        {
            MapSize = size;
            MapSeed = seed;
            Player1ClientId = p1Id;
            Player2ClientId = p2Id;
        }
    }
}
