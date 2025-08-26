using XFramework;

public class TestEvent : IEvent
{
    public static readonly int ID = typeof(TestEvent).GetHashCode();
    public string Message;

    public static TestEvent Create(string message)
    {
        TestEvent evt = Game.CachePool.Spawn<TestEvent>();
        evt.Message = message;
        return evt;
    }

    public void Clear()
    {
    }
}