using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{

    /// <summary>
    /// Message that clients send to the server as soon as the connection is established.
    /// <br/>The server will reply by sending the information of all connected clients to the sender of this message. (NetworkMessage_ConnectedClientsInfo)
    /// </summary>
    [System.Serializable]
    public class NetworkMessage_ClientConnected : NetworkMessage
    {
        public string DisplayName;

        public NetworkMessage_ClientConnected(string displayName) : base("ClientConnected")
        {
            DisplayName = displayName;
        }
    }
}
