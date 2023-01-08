using System;
using System.Runtime.Serialization;

namespace PowerLib.System.Threading
{
  [Serializable]
  public class LockTimeoutException : TimeoutException
  {
    public LockTimeoutException()
      : base()
    { }

    public LockTimeoutException(string message)
      : base(message)
    { }

    public LockTimeoutException(string message, Exception innerException)
      : base(message, innerException)
    { }

    protected LockTimeoutException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    { }
  }
}
