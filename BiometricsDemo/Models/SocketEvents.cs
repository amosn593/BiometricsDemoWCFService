namespace BiometricsDemo.Models
{
    public class SocketEvents<T>
    {
        public string eventName { get; set; }
        public T data { get; set; }
    }

    public class EventKey
    {
        public string eventKey { get; set; }
    }
}
