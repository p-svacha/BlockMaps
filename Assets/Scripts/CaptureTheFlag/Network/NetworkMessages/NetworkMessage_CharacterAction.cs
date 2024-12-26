using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    /// <summary>
    /// Base class for network messages that are used to inform the server that a character has issued a CharacterAction on the given tick.
    /// </summary>
    public class NetworkMessage_CharacterAction : NetworkMessage
    {
        public int Tick;
        public int CharacterId;
        public int TargetId;

        public NetworkMessage_CharacterAction(string messageType, int characterId, int targetId = 0) : base(messageType)
        {
            CharacterId = characterId;
            TargetId = targetId;
        }
    }
}
