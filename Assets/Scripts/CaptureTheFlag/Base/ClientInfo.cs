using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace CaptureTheFlag
{
    public class ClientInfo
    {
        public TcpClient TcpClient { get; private set; }
        public string ClientId { get; private set; }
        public string DisplayName { get; private set; }

        public ClientInfo(TcpClient client, string displayName)
        {
            TcpClient = client;
            ClientId = (client.Client.RemoteEndPoint != null) ? client.Client.RemoteEndPoint.ToString() : "UnknownSender";
            DisplayName = displayName;
        }

        public ClientInfo(string clientId, string displayName)
        {
            ClientId = clientId;
            DisplayName = displayName;
        }

        public ClientInfo(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
