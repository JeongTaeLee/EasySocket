using System.Collections.Generic;
using EasySocket.Behaviors;
using EasySocket.Listeners;

namespace EasySocket.Workers
{
    public class BaseServerWorker : IServerWorker
    {
        public IReadOnlyList<IListener> listeners { get; private set; } = null;

        public void SetServerBehavior(IServerBehavior behavior)
        {
     
        }
    }
}