using System;
using System.Collections;
using System.Collections.Generic;
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
        public List<TcpClient> ConnectedClients = new List<TcpClient>();

        private void Awake()
        {
            // Make this a singleton for easy global access
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Start hosting a server on the given port,
        /// and also connect a local client so the host is in the connectedClients list.
        /// </summary>
        public void StartServerAndConnectAsHost()
        {
            StartServer();

            // After starting the server, connect as localhost
            // This ensures the host is treated just like any other client.
            if (NetworkClient.Instance != null)
            {
                NetworkClient.Instance.ServerIP = "127.0.0.1";
                NetworkClient.Instance.ServerPort = Port;
                NetworkClient.Instance.ConnectToServer();
            }
            else
            {
                Debug.LogWarning("No NetworkClient found in scene. Host won't be treated as client!");
            }
        }

        /// <summary>
        /// Starts the server listening for incoming clients.
        /// </summary>
        private void StartServer()
        {
            try
            {
                ServerListener = new TcpListener(IPAddress.Any, Port);
                ServerListener.Start();
                Debug.Log("Server started. Listening on port " + Port);

                // Begin listening asynchronously
                ServerListener.BeginAcceptTcpClient(OnClientConnect, null);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to start server: " + e.Message);
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
                ConnectedClients.Add(newClient);
                Debug.Log("Client connected: " + newClient.Client.RemoteEndPoint);

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
                        NetworkActionWrapper incomingWrapper = JsonUtility.FromJson<NetworkActionWrapper>(finalJson);
                        if (incomingWrapper == null)
                        {
                            Debug.LogWarning("Failed to parse NetworkActionWrapper!");
                            return;
                        }

                        // (3) Look up the real System.Type
                        System.Type realType = System.Type.GetType(incomingWrapper.TypeName);
                        if (realType == null)
                        {
                            Debug.LogWarning($"Unknown type: {incomingWrapper.TypeName}");
                            return;
                        }

                        // (4) Deserialize the *actual subclass* from wrapper.Json
                        NetworkAction realAction = (NetworkAction)JsonUtility.FromJson(incomingWrapper.Json, realType);
                        if (realAction == null)
                        {
                            Debug.LogWarning("Failed to deserialize realAction from wrapper.Json");
                            return;
                        }

                        // Append the sender ID
                        string senderId = (client.Client.RemoteEndPoint != null)
                            ? client.Client.RemoteEndPoint.ToString()
                            : "UnknownSender";
                        realAction.SenderId = senderId;

                        // (5) Broadcast to all clients (including the sender)
                        BroadcastAction(realAction);

                        // (6) Keep listening
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
        /// Broadcast the given action to all connected clients.
        /// </summary>
        private void BroadcastAction(NetworkAction action)
        {
            // 1) Build a new wrapper for the actual subclass
            var wrapper = new NetworkActionWrapper
            {
                // The real runtime type => e.g. "CaptureTheFlag.Networking.NetworkAction_StartMatch"
                TypeName = action.GetType().AssemblyQualifiedName,

                // Subclass JSON => includes all fields (MapSeed, MapSize, etc.)
                Json = JsonUtility.ToJson(action)
            };

            // 2) Serialize the wrapper itself
            string finalJson = JsonUtility.ToJson(wrapper);

            // 3) Convert to bytes and broadcast
            byte[] data = System.Text.Encoding.UTF8.GetBytes(finalJson);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            foreach (var client in ConnectedClients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("BroadcastAction failed for client: " + e.Message);
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up
            if (ServerListener != null)
            {
                ServerListener.Stop();
            }
            foreach (var client in ConnectedClients)
            {
                client.Close();
            }
            ConnectedClients.Clear();
        }
    }
}
