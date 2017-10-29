using System;

namespace ThreadPool.Impl
{
    public class Task<T>
    {
        internal string Id { get; private set; }
        private Func<T> func;

        public Task(Func<T> func)
        {
            Id = Guid.NewGuid().ToString();
            this.func = func;
        }

        public T Execute()
        {
            return func();
        }
    }
}
