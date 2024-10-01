using XFramework;

public class TestEventArgs : IEventArgs
{
    public static readonly int Id = typeof(TestEventArgs).GetHashCode();
    public string Message;

    public TestEventArgs(string message)
    {
        Message = message;
    }
}