using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkAction_MoveCharacter : NetworkAction
    {
        public int CharacterId;
        public int TargetNodeId;
        public int Tick;

        public NetworkAction_MoveCharacter(int characterId, int targetNodeId, int tick) : base("MoveCharacter")
        {
            CharacterId = characterId;
            TargetNodeId = targetNodeId;
            Tick = tick;
        }
    }
}
