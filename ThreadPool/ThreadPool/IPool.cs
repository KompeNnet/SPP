using ThreadPool.Impl;

namespace ThreadPool
{
    public interface IPool<T>
    {
        IFuture<T> Execute(Task<T> task);
        void Dispose();
    }
}
