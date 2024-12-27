using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    /// <summary>
    /// NetworkClients send and receive NetworkMessages from the server. One NetworkClient acts as the host.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }

        public string ServerIP = "127.0.0.1";
        public int ServerPort = 7777;

        public string ClientId { get; private set; }

        public bool IsHost { get; private set; }
        private TcpClient Client;
        private NetworkStream Stream;

        public bool IsInGameOrLobby;

        public CtfGame Game;
        public CtfMatch Match { get; set; }
        private System.Action OnConnectionCallback;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetAsHost()
        {
            IsHost = true;
        }

        /// <summary>
        /// Connect to the specified server IP and port.
        /// </summary>
        public void ConnectToServer(System.Action callback)
        {
            try
            {
                OnConnectionCallback = callback;
                Client = new TcpClient();
                Client.BeginConnect(ServerIP, ServerPort, OnConnect, null);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to connect to server: " + e.Message);
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                Client.EndConnect(ar);
                Stream = Client.GetStream();
                Debug.Log("[Client] Connected to server.");

                // Get local endpoint as our "client ID"
                if (Client.Client.LocalEndPoint != null)
                {
                    ClientId = Client.Client.LocalEndPoint.ToString();
                }
                else
                {
                    ClientId = Guid.NewGuid().ToString();
                }

                OnConnectionCallback?.Invoke();

                ReceiveData();
            }
            catch (Exception e)
            {
                Debug.LogError("[Client] Connection error: " + e.Message);
            }
        }

        /// <summary>
        /// Send a NetworkMessage to the server, which will the be broadcast to all clients.
        /// </summary>
        public void SendMessage(NetworkMessage message)
        {
            if (Client == null || !Client.Connected)
            {
                Debug.LogWarning("[Client] Not connected to a server!");
                return;
            }

            try
            {
                // 1) Create the wrapper and store type info + subclass JSON
                var wrapper = new NetworkMessageWrapper
                {
                    TypeName = message.GetType().AssemblyQualifiedName,
                    Json = JsonUtility.ToJson(message)
                };

                // 2) Serialize the wrapper into JSON
                string finalJson = JsonUtility.ToJson(wrapper);

                // 3) Convert to bytes
                byte[] data = Encoding.UTF8.GetBytes(finalJson);
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                // 4) Write the length prefix, then the data
                Stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                Stream.Write(data, 0, data.Length);

                Debug.Log($"[Client] Sent NetworkMessage of type '{message.MessageType}' via wrapper {wrapper.TypeName}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Client] SendMessage failed: " + e.Message);
            }
        }

        /// <summary>
        /// Continually read data from the server.
        /// </summary>
        private void ReceiveData()
        {
            byte[] lengthBuffer = new byte[4];

            Stream.BeginRead(lengthBuffer, 0, 4, ar =>
            {
                try
                {
                    int bytesRead = Stream.EndRead(ar);
                    if (bytesRead <= 0)
                    {
                        Debug.Log("Disconnected from server.");
                        Client.Close();
                        return;
                    }

                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] dataBuffer = new byte[dataLength];

                    // Read the actual data
                    Stream.BeginRead(dataBuffer, 0, dataLength, ar2 =>
                    {
                        int bytesReadData = Stream.EndRead(ar2);
                        if (bytesReadData <= 0)
                        {
                            Debug.Log("Disconnected from server.");
                            Client.Close();
                            return;
                        }

                        string receivedJson = Encoding.UTF8.GetString(dataBuffer);
                        ReceiveNetworkMessage(receivedJson);

                        // Keep reading
                        ReceiveData();
                    }, null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("ReceiveData error: " + e.Message);
                }
            }, null);
        }

        private void ReceiveNetworkMessage(string finalJson)
        {
            // 1) Deserialize the wrapper
            NetworkMessageWrapper wrapper = JsonUtility.FromJson<NetworkMessageWrapper>(finalJson);

            // 2) Look up the actual System.Type from the stored typeName
            Type actualType = Type.GetType(wrapper.TypeName);
            if (actualType == null)
            {
                Debug.LogWarning($"Could not find type {wrapper.TypeName}!");
                return;
            }

            // 3) Deserialize *again*, this time into the actual subclass
            NetworkMessage message = (NetworkMessage)JsonUtility.FromJson(wrapper.Json, actualType);
            message.IsSentBySelf = (message.SenderId == ClientId);

            // 4) Now 'message' includes all subclass fields
            Debug.Log($"[Client] Received message '{message.MessageType}' from server.\nFull data:{finalJson}");
            try
            {
                switch (message.MessageType)
                {
                    case "ConnectedClientsInfo":
                        var clientInfosMessage = (NetworkMessage_ConnectedClientsInfo)message;
                        List<ClientInfo> clientInfos = new List<ClientInfo>();
                        for(int i = 0; i < clientInfosMessage.ClientIds.Length; i++)
                            clientInfos.Add(new ClientInfo(clientInfosMessage.ClientIds[i], clientInfosMessage.ClientDisplayNames[i]));
                        Game.OnNewClientConnected(clientInfos, isSelf: clientInfosMessage.NewlyConnectedClientId == ClientId);
                        break;

                    case "LobbyState":
                        var lobbyStateMessage = (NetworkMessage_LobbyState)message;
                        Game.SetUpdatedLobbyInfo(new CtfMatchLobby(Game.ServerClientInfos, lobbyStateMessage));
                        break;

                    case "InitializeMultiplayerMatch":
                        Game.SetMultiplayerMatchAsReady();
                        break;

                    default:
                        Match.OnNetworkMessageReceived(message);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[Client] Error in handling message '{message.MessageType}': {e.Message}");
            }
        }
    }
}
