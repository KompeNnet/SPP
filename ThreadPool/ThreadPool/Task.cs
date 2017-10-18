using System;

namespace ThreadPool
{
    class Task<T>
    {
        public bool IsWaiting { get; private set; } = true;
        private Func<T> Act { get; set; }

        public Task(Func<T> act)
        {
            Act = act;
        }

        public T Execute()
        {
            lock (this)
            {
                IsWaiting = false;
                return Act();
            }
        }
    }
}
