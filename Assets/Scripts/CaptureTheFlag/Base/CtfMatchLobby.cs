using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfMatchLobby
    {
        public CtfMatchType MatchType;
        public List<ClientInfo> Clients;
        public MatchSettings Settings;

        /// <summary>
        /// New singleplayer lobby
        /// </summary>
        public CtfMatchLobby(string name)
        {
            MatchType = CtfMatchType.Singleplayer;
            Clients = new List<ClientInfo>()
            {
                new ClientInfo(name),
                new ClientInfo("Opponent")
            };
            Settings = new MatchSettings();
        }
        /// <summary>
        /// New multiplayer lobby
        /// </summary>
        public CtfMatchLobby(ClientInfo host)
        {
            MatchType = CtfMatchType.Multiplayer;
            Clients = new List<ClientInfo>() { host };
            Settings = new MatchSettings();
        }
        /// <summary>
        /// Multiplayer lobby with the data from the given network message.
        /// </summary>
        public CtfMatchLobby(List<ClientInfo> allConnectedClients, NetworkMessage_LobbyState message)
        {
            MatchType = CtfMatchType.Multiplayer;
            Clients = new List<ClientInfo>();
            foreach (string id in message.ClientIds) Clients.Add(allConnectedClients.First(x => x.ClientId == id));
            Settings = new MatchSettings(message.MatchSettings);
        }

        public NetworkMessage_LobbyState ToNetworkMessage()
        {
            return new NetworkMessage_LobbyState(Clients.Select(c => c.ClientId).ToArray(), Settings.ToIntArray());
        }
    }
}
