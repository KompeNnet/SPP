using System.Threading;

namespace ThreadPool.Impl
{
    public class Result<T> : IFuture<T>
    {
        internal string Id { get; private set; }
        private T result;
        private bool done;
        internal ManualResetEvent doneEvent;

        public Result(string id)
        {
            Id = id;
            doneEvent = new ManualResetEvent(false);
            done = false;
        }

        internal void SetResult(T result)
        {
            this.result = result;
            doneEvent.Set();
        }

        public T Get()
        {
            doneEvent.WaitOne();
            return result;
        }

        public bool IsDone()
        {
            return done;
        }
    }
}
