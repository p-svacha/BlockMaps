using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkMessage_InitializeMultiplayerMatch : NetworkMessage
    {
        public int WorldGeneratorIndex;
        public int WorldSize;
        public int Seed;
        public string Player1ClientId;
        public string Player2ClientId;

        public NetworkMessage_InitializeMultiplayerMatch(int worldGeneratorIndex, int worldSize, int seed, string p1Id, string p2Id) : base("InitializeMultiplayerMatch")
        {
            WorldGeneratorIndex = worldGeneratorIndex;
            WorldSize = worldSize;
            Seed = seed;
            Player1ClientId = p1Id;
            Player2ClientId = p2Id;
        }
    }
}
