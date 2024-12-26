using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfMatchLobby
    {
        public List<ClientInfo> Clients;
        public MatchSettings Settings;

        public CtfMatchLobby(ClientInfo host)
        {
            Clients = new List<ClientInfo>() { host };
            Settings = new MatchSettings();
        }
        public CtfMatchLobby(List<ClientInfo> allConnectedClients, NetworkMessage_LobbyState message)
        {
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
