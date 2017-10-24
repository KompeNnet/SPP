using System;

namespace ThreadPool
{
    public class Task<T> : IFuture<T>
    {
        private Func<T> Act { get; set; }
        private bool Done { get; set; } = false;
        private T Result { get; set; }

        public Task(Func<T> act)
        {
            Act = act;
        }

        internal void Execute()
        {
            lock (this)
            {
                Result = Act();
                Done = true;
            }
        }

        public T Get()
        {
            if (Done) return Result;
            return default(T);
        }

        public bool IsDone()
        {
            return Done;
        }
    }
}
