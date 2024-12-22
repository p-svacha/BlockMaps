using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    [System.Serializable]
    public class NetworkAction
    {
        public string SenderId;
        public string ActionType;

        public NetworkAction(string actionType)
        {
            ActionType = actionType;
        }

        // This will NOT be serialized into JSON. It's purely local state.
        [System.NonSerialized]
        public bool IsSentBySelf;
    }
}
