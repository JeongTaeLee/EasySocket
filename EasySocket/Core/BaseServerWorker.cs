namespace EasySocket.Core
{
    public class BaseServerWorker : IServerWorker
    {
        
        public virtual void Start()
        {
                
        }

        protected virtual void OnAcceptedSocket()
        {

        }
    }
}