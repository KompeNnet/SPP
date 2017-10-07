using System.Collections.Generic;
using System.Threading;

namespace ThreadPool.PoolAttributes
{
    class PoolEvents
    {
        public ManualResetEvent pauseEvent;
        public ManualResetEvent scheduleEvent;
        public Dictionary<int, ManualResetEvent> eventCollection;
    }
}
