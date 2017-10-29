namespace ThreadPool.Attributes
{
    class Properties
    {
        public int MinThreads { get; set; }
        public int MaxThreads { get; set; }
        public int ThreadCount { get; set; }
        public object lockObj;
        public bool IsDisposed { get; set; }

        public Properties()
        {

        }

        public Properties(int threadCount)
        {
            IsDisposed = false;
            lockObj = new object();
            ThreadCount = threadCount;
        }

        public Properties(int minThreads, int maxThreads)
        {
            IsDisposed = false;
            lockObj = new object();
            ThreadCount = 0;
            MinThreads = minThreads;
            MaxThreads = maxThreads;
        }
    }
}
