using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    /// <summary>
    /// Message that server sends to a client once it receives a "NetworkMessage_ClientConnected" from that client.
    /// <br/>Contains information about all currently connect clients and information about which client newly connected.
    /// </summary>
    [System.Serializable]
    public class NetworkMessage_ConnectedClientsInfo : NetworkMessage
    {
        public string[] ClientIds;
        public string[] ClientDisplayNames;
        public string NewlyConnectedClientId;

        public NetworkMessage_ConnectedClientsInfo(string[] ids, string[] names, string newConnectedClient) : base("ConnectedClientsInfo")
        {
            ClientIds = ids;
            ClientDisplayNames = names;
            NewlyConnectedClientId = newConnectedClient;
        }
    }
}
