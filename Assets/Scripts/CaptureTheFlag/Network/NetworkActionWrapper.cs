namespace CaptureTheFlag.Network
{
    /// <summary>
    /// The object that is sent through the network. Wraps the action and action type so that polymorphic actions are handled easier.
    /// </summary>
    [System.Serializable]
    public class NetworkActionWrapper
    {
        public string TypeName;
        public string Json;
    }
}