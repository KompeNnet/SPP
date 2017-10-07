namespace ThreadPool.PoolAttributes
{
    class PoolProperties
    {
        public int maxThreadCount;
        public int minThreadCount;
        public int threadCountStatic;
        public int busyThreads = 0;
        public bool isBusy;
        public bool isPaused;
    }
}
