using System;
using EasySocket.Workers;

namespace EasySocket
{
    public sealed class EasySocketService
    {
        public EasySocketConfig config { get; private set; } = null;

        public EasySocketService(EasySocketConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config)); 
            }

            this.config = config;
        }

        public void Start()
        {
            
        }
    }
}