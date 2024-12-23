using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkMessage
    {
        public string SenderId;
        public string MessageType;

        public NetworkMessage(string messageType)
        {
            MessageType = messageType;
        }

        // This will NOT be serialized into JSON. It's purely local state.
        [System.NonSerialized]
        public bool IsSentBySelf;
    }
}
