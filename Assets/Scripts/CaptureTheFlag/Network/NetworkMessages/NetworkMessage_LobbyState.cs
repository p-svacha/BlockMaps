using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    /// <summary>
    /// Message that server sends to a client once it receives a "RequestJoinLobby" NetworkMessage from that client.
    /// <br/>Contains information about the lobby the client connects to.
    /// </summary>
    public class NetworkMessage_LobbyState : NetworkMessage
    {
        public string[] ClientIds;
        public int[] MatchSettings;

        public NetworkMessage_LobbyState(string[] ids, int[] matchSettings) : base("LobbyState")
        {
            ClientIds = ids;
            MatchSettings = matchSettings;
        }
    }
}
