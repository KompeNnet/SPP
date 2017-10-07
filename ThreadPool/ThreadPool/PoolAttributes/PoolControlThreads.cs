using System.Threading;

namespace ThreadPool.PoolAttributes
{
    class PoolControlThreads
    {
        public Thread PoolControlThread { get; set; }
        public Thread ScheduleThread { get; set; }
    }
}
