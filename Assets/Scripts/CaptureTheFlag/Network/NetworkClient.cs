using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace CaptureTheFlag.Networking
{
    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }

        public string ServerIP = "127.0.0.1";
        public int ServerPort = 7777;

        public string ClientId { get; private set; }

        private TcpClient Client;
        private NetworkStream Stream;

        public CtfGame Game;
        public CtfMatch Match;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Connect to the specified server IP and port.
        /// </summary>
        public void ConnectToServer()
        {
            try
            {
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
                Debug.Log("Connected to server.");

                // Get local endpoint as our "client ID"
                if (Client.Client.LocalEndPoint != null)
                {
                    ClientId = Client.Client.LocalEndPoint.ToString();
                }
                else
                {
                    ClientId = Guid.NewGuid().ToString();
                }

                ReceiveData();
            }
            catch (Exception e)
            {
                Debug.LogError("Connection error: " + e.Message);
            }
        }

        /// <summary>
        /// Send a NetworkAction to the server, preserving subclass fields.
        /// </summary>
        public void SendAction(NetworkAction action)
        {
            if (Client == null || !Client.Connected)
            {
                Debug.LogWarning("Not connected to a server!");
                return;
            }

            try
            {
                // 1) Create the wrapper and store type info + subclass JSON
                var wrapper = new NetworkActionWrapper
                {
                    TypeName = action.GetType().AssemblyQualifiedName, // e.g. "NetworkAction_StartMatch"
                    Json = JsonUtility.ToJson(action)                  // includes all subclass fields
                };

                // 2) Serialize the wrapper into JSON
                string finalJson = JsonUtility.ToJson(wrapper);

                // 3) Convert to bytes
                byte[] data = Encoding.UTF8.GetBytes(finalJson);
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                // 4) Write the length prefix, then the data
                Stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                Stream.Write(data, 0, data.Length);

                Debug.Log($"Sent NetworkAction of type {action.ActionType} via wrapper {wrapper.TypeName}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("SendAction failed: " + e.Message);
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
                        ReceiveNetworkAction(receivedJson);

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

        private void ReceiveNetworkAction(string finalJson)
        {
            Debug.Log(finalJson);

            // 1) Deserialize the wrapper
            NetworkActionWrapper wrapper = JsonUtility.FromJson<NetworkActionWrapper>(finalJson);

            // 2) Look up the actual System.Type from the stored typeName
            Type actualType = Type.GetType(wrapper.TypeName);
            if (actualType == null)
            {
                Debug.LogWarning($"Could not find type {wrapper.TypeName}!");
                return;
            }

            // 3) Deserialize *again*, this time into the actual subclass
            NetworkAction action = (NetworkAction)JsonUtility.FromJson(wrapper.Json, actualType);
            action.IsSentBySelf = (action.SenderId == ClientId);

            // 4) Now 'action' includes all subclass fields
            Debug.Log($"Received real action type: {action.GetType().Name}");

            try
            {
                switch (action.ActionType)
                {
                    case "StartMatch":
                        var startMatchAction = (NetworkAction_StartMatch)action;
                        Debug.Log($"StartMatch => size={startMatchAction.MapSize}, seed={startMatchAction.MapSeed}");
                        Game.SetMultiplayerMatchAsReady(startMatchAction.MapSize, startMatchAction.MapSeed, playAsBlue: startMatchAction.IsSentBySelf);
                        break;

                    default:
                        Match.OnNetworkActionReceived(action);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }
}
