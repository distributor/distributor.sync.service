using System;

namespace Upsmile.Sync.Initiator
{
    public class SyncEventArgument:EventArgs
    {
        public object Result { get; set; }
        public Exception Exception { get; set; }

    }
}