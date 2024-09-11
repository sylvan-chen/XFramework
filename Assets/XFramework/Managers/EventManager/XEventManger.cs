using System;
using XFramework;

internal class XEventManger : BaseManager, IEventManager
{
    internal override int Priority => throw new NotImplementedException();

    public void ClearAll()
    {
        throw new NotImplementedException();
    }

    public int EventHandlerCount(int id)
    {
        throw new NotImplementedException();
    }

    public void Publish(object publisher, IEventArgs args)
    {
        throw new NotImplementedException();
    }

    public void Subscribe(int id, EventHandler<IEventArgs> handler)
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe(int id, EventHandler<IEventArgs> handler)
    {
        throw new NotImplementedException();
    }

    internal override void Shutdown()
    {
        throw new NotImplementedException();
    }

    internal override void Update(float logicSeconds, float realSeconds)
    {
        throw new NotImplementedException();
    }
}
