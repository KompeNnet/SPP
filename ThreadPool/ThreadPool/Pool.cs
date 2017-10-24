using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThreadPool.PoolAttributes;

namespace ThreadPool
{
    public class Pool<T> : IDisposable
    {
        private PoolProperties properties = new PoolProperties();
        private PoolEvents events = new PoolEvents();
        private PoolControlThreads controlThreads = new PoolControlThreads();

        private List<Thread> threadList;
        private Queue<Task<T>> taskQueue = new Queue<Task<T>>();

        private Timer timer;

        public Pool(int ThreadCountStatic)
        {
            properties.ThreadCountStatic = ThreadCountStatic;
            SetPoolData(properties.ThreadCountStatic, properties.ThreadCountStatic);
            for (int i = 0; i < properties.ThreadCountStatic; i++)
            { StartNewThread(i); }
        }

        public Pool(int minThreadCount, int maxThreadCount)
        {
            SetLimits(minThreadCount, maxThreadCount);
            SetPoolData(properties.MinThreadCount, properties.MaxThreadCount);
            for (int i = 0; i < properties.MinThreadCount; i++)
            {
                StartNewThread(i);
                properties.busyThreads++;
            }
            controlThreads.PoolControlThread = new Thread(DynamicPool);
            controlThreads.PoolControlThread.Start();
        }

        public void Execute(Task<T> task)
        {
            lock (properties.lockConstruct)
            {
                if (task != null && !properties.IsPaused) AddTask(task);
            }
        }

        public void Stop()
        {
            lock (properties.lockConstruct) { properties.IsPaused = true; }
            while (taskQueue.Count > 0)
            {
                events.pauseEvent.WaitOne();
                events.pauseEvent.Reset();
            }
            Dispose(true);
        }

        public void Dispose()
        {
            if (properties.MaxThreadCount == 0)
                timer.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposingNeeded)
        {
            if (properties.IsBusy)
            {
                if (isDisposingNeeded)
                {
                    controlThreads.PoolControlThread.Abort();
                    events.pauseEvent.Dispose();
                    foreach (Thread t in threadList)
                    {
                        t.Abort();
                        events.eventCollection.ElementAt(t.ManagedThreadId).Value.Dispose();
                    }
                }
                properties.IsBusy = false;
            }
        }

        private void SetLimits(int min, int max)
        {
            if (Math.Abs(min) < Math.Abs(max))
            {
                properties.MinThreadCount = Math.Abs(min);
                properties.MaxThreadCount = Math.Abs(max);
            }
            else { throw new ArgumentException(); }
        }

        private void AddTask(Task<T> task)
        {
            lock (taskQueue) { taskQueue.Enqueue(task); }
            events.scheduleEvent.Set();
        }

        private void SetPoolData(int threadCount, int collectionCount)
        {
            properties.lockConstruct = new object();
            events.pauseEvent = new ManualResetEvent(false);
            SetSchedule();
            SetThreadList(threadCount);
            SetEventCollection(threadCount);
        }

        private void SetSchedule()
        {
            events.scheduleEvent = new ManualResetEvent(false);
            controlThreads.ScheduleThread = new Thread(ThreadStart) { IsBackground = true };
            controlThreads.ScheduleThread.Start();
        }

        private void SetThreadList(int threadCount)
        {
            threadList = new Thread[threadCount].ToList();
        }

        private void SetEventCollection(int collectionCount)
        {
            events.eventCollection = new Dictionary<int, ManualResetEvent>(collectionCount);
        }

        private void StartNewThread(int i)
        {
            threadList.Add(new Thread(ThreadTaskExecute) { IsBackground = true });
            events.eventCollection.Add(threadList.ElementAt(i).ManagedThreadId, new ManualResetEvent(false));
            threadList.ElementAt(i).Start();
        }

        private void ThreadStart()
        {
            while (true)
            {
                events.scheduleEvent.WaitOne();
                lock (threadList) { FindFreeThread(); }
                events.scheduleEvent.Reset();
            }
        }

        private void FindFreeThread()
        {
            foreach (Thread thread in threadList)
            {
                ManualResetEvent currentEvent = events.eventCollection.ElementAt(thread.ManagedThreadId).Value;
                if (currentEvent.WaitOne(0) == false)
                {
                    currentEvent.Set();
                    return;
                }
            }
        }

        private void ThreadTaskExecute()
        {
            while (true)
            {
                events.eventCollection.ElementAt(Thread.CurrentThread.ManagedThreadId).Value.WaitOne();
                Task<T> task = GetTask();
                if (task != null) { ExecuteTask(task); }
            }
        }

        private void ExecuteTask(Task<T> task)
        {
            try { task.Execute(); } catch { }
            if (properties.IsPaused) { events.pauseEvent.Set(); }
            events.eventCollection.ElementAt(Thread.CurrentThread.ManagedThreadId).Value.Reset();
        }

        private Task<T> GetTask()
        {
            lock (taskQueue)
            {
                try
                {
                    if (taskQueue.Count > 0)
                    {
                        if (taskQueue.Count > 1) events.scheduleEvent.Set();
                        return taskQueue.Dequeue();
                    }
                }
                catch { }
                return null;
            }
        }

        private void DynamicPool()
        {
            timer = new Timer(TimerCallBack, null, 0, 100);
        }

        private void TimerCallBack(object state)
        {
            if (properties.busyThreads < taskQueue.Count()) { IncreaseThreadAmount(); }
            if (properties.busyThreads > taskQueue.Count()) { ReduceThreadAmount(); }
        }

        private void IncreaseThreadAmount()
        {
            while (properties.busyThreads != taskQueue.Count() && properties.busyThreads < properties.MaxThreadCount)
            {
                threadList.ElementAt(properties.busyThreads).Start();
                properties.busyThreads++;
            }
        }

        private void ReduceThreadAmount()
        {
            while (properties.busyThreads > properties.MinThreadCount && properties.busyThreads < taskQueue.Count())
            {
                threadList.ElementAt(properties.busyThreads--).Abort();
                properties.busyThreads--;
            }
        }

        ~Pool()
        {
            Dispose(false);
        }
    }
}
