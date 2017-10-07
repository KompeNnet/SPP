namespace ThreadPool.PoolAttributes
{
    class PoolProperties
    {
        public int maxThreadCount;
        public int minThreadCount;
        public int threadCountStatic;
        public int currentBusyThreads = 0;
        public bool isBusy;
    }
}
