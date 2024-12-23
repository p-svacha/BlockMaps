using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkMessage_MoveCharacter : NetworkMessage_CharacterAction
    {
        public int TargetNodeId;

        public NetworkMessage_MoveCharacter(int characterId, int targetNodeId) : base("CharacterAction_MoveCharacter", characterId)
        {
            TargetNodeId = targetNodeId;
        }
    }
}
