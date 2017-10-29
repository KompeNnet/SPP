namespace ThreadPool
{
    public interface IFuture<T>
    {
        T Get();
        bool IsDone();
    }
}
