using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThreadPool.Attributes;

namespace ThreadPool.Impl
{
    public class Pool<T> : IDisposable, IPool<T>
    {
        private Properties properties;
        private AutoResetEvent newTaskEvent;
        private List<Thread> threadList;
        private Queue<Task<T>> taskQueue = new Queue<Task<T>>();
        private List<Result<T>> resultList = new List<Result<T>>();
        private Thread controlThread;
        private Dictionary<int, Timer> timers;

        public Pool(int threadCount)
        {
            threadList = new List<Thread>();
            properties = new Properties(threadCount);
            for (int i = 0; i < properties.ThreadCount; i++)
            {
                AddNewThread();
            }
        }

        public Pool(int minThread, int maxThread)
        {
            threadList = new List<Thread>();
            properties = new Properties(minThread, maxThread);
            newTaskEvent = new AutoResetEvent(false);
            timers = new Dictionary<int, Timer>();
            for (int i = 0; i < properties.MinThreads; i++)
            {
                timers.Add(AddNewThread().ManagedThreadId, null);
                properties.ThreadCount++;
            }
            controlThread = new Thread(ControlThreadWorker);
            controlThread.Start();
        }

        public IFuture<T> Execute(Task<T> task)
        {
            if (task != null)
            {
                lock (taskQueue)
                {
                    taskQueue.Enqueue(task);
                    newTaskEvent.Set();
                }
                lock (resultList) { resultList.Add(new Result<T>(task.Id)); }
            }
            return null;
        }

        public void Dispose()
        {
            if (!properties.IsDisposed)
            {
                controlThread.Abort();
                newTaskEvent.Dispose();
                foreach (Thread t in threadList)
                {
                    t.Abort();
                }
                try { controlThread.Abort(); } catch { }
                properties.IsDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private void ThreadWorker()
        {
            while (true)
            {
                timers[Thread.CurrentThread.ManagedThreadId] = new Timer(TimerCallback, null, 0, 100);
                newTaskEvent.WaitOne();
                timers[Thread.CurrentThread.ManagedThreadId].Dispose();
                Task<T> task = GetTask();
                if (task != null)
                {
                    Result<T> result;
                    lock (resultList)
                    {
                        result = resultList.Where(r => (r.Id == task.Id)).First();
                        resultList.Remove(result);
                    }
                    result.SetResult(task.Execute());
                }
            }
        }

        private Task<T> GetTask()
        {
            lock (taskQueue)
            {
                return taskQueue.Dequeue();
            }
        }

        private void TimerCallback(object state)
        {
            lock (properties)
            {
                if (properties.ThreadCount > properties.MinThreads)
                {
                    timers[Thread.CurrentThread.ManagedThreadId].Dispose();
                    timers.Remove(Thread.CurrentThread.ManagedThreadId);
                    properties.ThreadCount--;
                    threadList.Remove(Thread.CurrentThread);
                    Thread.CurrentThread.Abort();
                }
            }
        }

        private void ControlThreadWorker()
        {
            if (taskQueue.Count > 0 && properties.ThreadCount < properties.MaxThreads)
            {
                AddNewThread();
                properties.ThreadCount++;
            }
        }

        private Thread AddNewThread()
        {
            Thread thread = new Thread(ThreadWorker) { IsBackground = true };
            threadList.Add(thread);
            thread.Start();
            return thread;
        }

        ~Pool()
        {
            Dispose();
        }
    }
}
