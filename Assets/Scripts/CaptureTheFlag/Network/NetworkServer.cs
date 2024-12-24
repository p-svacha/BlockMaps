using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace CaptureTheFlag.Network
{
    public class NetworkServer : MonoBehaviour
    {
        public static NetworkServer Instance { get; private set; }

        public int Port = 7777;
        private TcpListener ServerListener;

        /// <summary>
        /// Dictionary holding all information of connected clients that have confirmed their connection by sending a NetworkMessage_ClientConnected.
        /// </summary>
        public Dictionary<TcpClient, ClientInfo> ConnectedClients = new Dictionary<TcpClient, ClientInfo>();

        private void Awake()
        {
            // Make this a singleton for easy global access
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Starts the server listening for incoming clients.
        /// </summary>
        public void StartServer()
        {
            try
            {
                ServerListener = new TcpListener(IPAddress.Any, Port);
                ServerListener.Start();
                Debug.Log("[Server] Server started. Listening on port " + Port);

                // Begin listening asynchronously
                ServerListener.BeginAcceptTcpClient(OnClientConnect, null);
            }
            catch (Exception e)
            {
                Debug.LogError("[Server] Failed to start server: " + e.Message);
            }
        }

        /// <summary>
        /// Callback for when a new client connects.
        /// </summary>
        private void OnClientConnect(IAsyncResult ar)
        {
            try
            {
                TcpClient newClient = ServerListener.EndAcceptTcpClient(ar);
                Debug.Log("[Server] Client connected: " + newClient.Client.RemoteEndPoint);

                // Start receiving data from this client
                ReceiveDataFromClient(newClient);

                // Continue listening for more clients
                ServerListener.BeginAcceptTcpClient(OnClientConnect, null);
            }
            catch (Exception e)
            {
                Debug.LogError("Error accepting client: " + e.Message);
            }
        }

        private void ReceiveDataFromClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            // Read length
            byte[] lengthBuffer = new byte[4];
            stream.BeginRead(lengthBuffer, 0, lengthBuffer.Length, ar =>
            {
                try
                {
                    int bytesRead = stream.EndRead(ar);
                    if (bytesRead <= 0)
                    {
                        Debug.Log("Client disconnected.");
                        ConnectedClients.Remove(client);
                        return;
                    }

                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] dataBuffer = new byte[dataLength];

                    // Read the actual message
                    stream.BeginRead(dataBuffer, 0, dataLength, ar2 =>
                    {
                        int bytesReadData = stream.EndRead(ar2);
                        if (bytesReadData <= 0)
                        {
                            Debug.Log("Client disconnected while reading data.");
                            ConnectedClients.Remove(client);
                            return;
                        }

                        // (1) Convert bytes to string
                        string finalJson = System.Text.Encoding.UTF8.GetString(dataBuffer);
                        Debug.Log($"[Server] Received data from client with id {client.Client.RemoteEndPoint}: {finalJson}");

                        // (2) Deserialize the "wrapper"
                        NetworkMessageWrapper incomingWrapper = JsonUtility.FromJson<NetworkMessageWrapper>(finalJson);
                        if (incomingWrapper == null)
                        {
                            Debug.LogWarning("[Server] Failed to parse NetworkMessageWrapper!");
                            return;
                        }

                        // (3) Look up the real System.Type
                        System.Type realType = System.Type.GetType(incomingWrapper.TypeName);
                        if (realType == null)
                        {
                            Debug.LogWarning($"[Server] Unknown type: {incomingWrapper.TypeName}");
                            return;
                        }

                        // (4) Deserialize the *actual subclass* from wrapper.Json
                        NetworkMessage realMessage = (NetworkMessage)JsonUtility.FromJson(incomingWrapper.Json, realType);
                        if (realMessage == null)
                        {
                            Debug.LogWarning("[Server] Failed to deserialize realMessage from wrapper.Json");
                            return;
                        }

                        // Append sender id
                        realMessage.SenderId = client.Client.RemoteEndPoint.ToString();

                        // If it's a client connect message:
                        //  - Register it as a connected client
                        //  - Boradcast the info of all currently connected clients to all clients
                        if(realMessage.MessageType == "ClientConnected")
                        {
                            NetworkMessage_ClientConnected connectMessage = (NetworkMessage_ClientConnected)realMessage;
                            Debug.Log($"[Server] Client with id {connectMessage.SenderId} has confirmed connection with display name {connectMessage.DisplayName}");
                            ConnectedClients.Add(client, new ClientInfo(client, connectMessage.DisplayName));
                            BroadcastMessageToAllClients(new NetworkMessage_ConnectedClientsInfo(ConnectedClients.Values.Select(c => c.ClientId).ToArray(), ConnectedClients.Values.Select(c => c.DisplayName).ToArray(), connectMessage.SenderId));
                        }

                        // Else broadcast/forwards the received message to all clients (including the sender)
                        else BroadcastMessageToAllClients(realMessage);

                        // Keep listening
                        ReceiveDataFromClient(client);

                    }, null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("ReceiveDataFromClient failed: " + e.Message);
                    ConnectedClients.Remove(client);
                }
            }, null);
        }

        /// <summary>
        /// Broadcast the given message to all connected clients.
        /// </summary>
        private void BroadcastMessageToAllClients(NetworkMessage message)
        {
            ConvertNetworkMessageToBytes(message, out byte[] data, out byte[] lengthPrefix);

            foreach (var client in ConnectedClients.Keys)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("BroadcastMessage failed for client: " + e.Message);
                }
            }
        }

        private void SendMessageToClient(TcpClient client, NetworkMessage message)
        {
            ConvertNetworkMessageToBytes(message, out byte[] data, out byte[] lengthPrefix);
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Debug.LogWarning("BroadcastMessage failed for client: " + e.Message);
            }
        }

        private void ConvertNetworkMessageToBytes(NetworkMessage message, out byte[] data, out byte[] lengthPrefix)
        {
            // 1) Build a new wrapper for the actual subclass
            var wrapper = new NetworkMessageWrapper
            {
                TypeName = message.GetType().AssemblyQualifiedName,
                Json = JsonUtility.ToJson(message)
            };

            // 2) Serialize the wrapper itself
            string finalJson = JsonUtility.ToJson(wrapper);

            // 3) Convert to bytes and broadcast
            data = System.Text.Encoding.UTF8.GetBytes(finalJson);
            lengthPrefix = BitConverter.GetBytes(data.Length);
        }

        private void OnDestroy()
        {
            // Clean up
            if (ServerListener != null)
            {
                ServerListener.Stop();
            }
            foreach (var client in ConnectedClients.Keys)
            {
                client.Close();
            }
            ConnectedClients.Clear();
        }
    }
}
