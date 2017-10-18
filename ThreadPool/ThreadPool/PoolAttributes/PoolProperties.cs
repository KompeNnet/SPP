namespace ThreadPool.PoolAttributes
{
    class PoolProperties
    {
        public int MaxThreadCount { get; set; } = 0;
        public int MinThreadCount { get; set; }
        public int ThreadCountStatic { get; set; }
        public int busyThreads = 0;
        public bool IsBusy { get; set; }
        public bool IsPaused { get; set; }
        public object lockConstruct;
    }
}
