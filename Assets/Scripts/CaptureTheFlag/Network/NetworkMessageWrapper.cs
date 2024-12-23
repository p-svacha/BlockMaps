namespace CaptureTheFlag.Network
{
    /// <summary>
    /// The object that is sent through the network. Wraps the message and message type so that subclasses of NetworkMessage can be handled easier.
    /// </summary>
    [System.Serializable]
    public class NetworkMessageWrapper
    {
        public string TypeName;
        public string Json;
    }
}