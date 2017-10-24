namespace ThreadPool
{
    interface IFuture<T>
    {
        T Get();
        bool IsDone();
    }
}
