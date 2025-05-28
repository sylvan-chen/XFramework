using XFramework;

public class TestEvent : IEvent
{
    public static readonly int Id = typeof(TestEvent).GetHashCode();
    public string Message;

    public static TestEvent Create(string message)
    {
        TestEvent evt = Global.CachePool.Spawn<TestEvent>();
        evt.Message = message;
        return evt;
    }

    public void Clear()
    {
    }
}