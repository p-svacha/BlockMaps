using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Networking
{
    [System.Serializable]
    public class NetworkAction
    {
        public string SenderId;
        public string ActionType;

        // This will NOT be serialized into JSON. It's purely local state.
        [System.NonSerialized]
        private bool _isSelf;

        public bool IsSentBySelf
        {
            get { return _isSelf; }
            set { _isSelf = value; }
        }
    }
}
