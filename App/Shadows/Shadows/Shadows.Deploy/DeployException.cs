using System;
using System.Runtime.Serialization;

namespace Shadows.Deploy
{
    [Serializable]
    internal class DeployException : Exception
    {
        public DeployException()
        {
        }

        public DeployException(string message) : base(message)
        {
        }

        public DeployException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeployException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
