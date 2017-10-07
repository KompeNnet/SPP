using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThreadPool.PoolAttributes;

namespace ThreadPool
{
    public class ThreadPool : IDisposable
    {
        private PoolProperties properties;
        private PoolEvents events;
        private PoolControlThreads controlThreads;

        public List<Thread> threadList;
        private List<Task> taskQueue = new List<Task>();

        public ThreadPool(int threadCountStatic)
        {
            properties.threadCountStatic = Math.Abs(threadCountStatic);
            SetPoolData(properties.threadCountStatic, properties.threadCountStatic);
            for (int i = 0; i < properties.threadCountStatic; i++)
            { StartNewThread(i); }
        }

        public ThreadPool(int minThreadCount, int maxThreadCount)
        {
            properties.minThreadCount = Math.Abs(minThreadCount);
            properties.maxThreadCount = Math.Abs(maxThreadCount);
            SetPoolData(properties.minThreadCount, properties.maxThreadCount);
            for (int i = 0; i < properties.maxThreadCount; i++)
            {
                if (i <= properties.minThreadCount)
                {
                    StartNewThread(i);
                    properties.busyThreads++;
                }
            }
            controlThreads.PoolControlThread = new Thread(DynamicPool);
            controlThreads.PoolControlThread.Start();
        }

        private void SetPoolData(int threadCount, int collectionCount)
        {
            events.pauseEvent = new ManualResetEvent(false);
            SetSchedule();
            SetThreadList(threadCount);
            SetEventCollection(threadCount);
        }

        private void SetSchedule()
        {
            events.scheduleEvent = new ManualResetEvent(false);
            controlThreads.scheduleThread = new Thread(ThreadStart) { IsBackground = true };
            controlThreads.scheduleThread.Start();
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
                if (events.eventCollection.ElementAt(thread.ManagedThreadId).Value.WaitOne(0) == false)
                {
                    events.eventCollection.ElementAt(thread.ManagedThreadId).Value.Set();
                    return;
                }
            }
        }

        private void ThreadTaskExecute()
        {
            while (true)
            {
                events.eventCollection.ElementAt(Thread.CurrentThread.ManagedThreadId).Value.WaitOne();
                Task task = GetTask();
                if (task != null) { ExecuteTask(task); }
            }
        }

        private void ExecuteTask(Task task)
        {
            try { task.Execute(); } catch { }
            DeleteTask(task);
            if (properties.isPaused) { events.pauseEvent.Set(); }
            events.eventCollection.ElementAt(Thread.CurrentThread.ManagedThreadId).Value.Reset();
        }

        private Task GetTask()
        {
            lock (taskQueue)
            {
                try
                {
                    IEnumerable<Task> notDone = taskQueue.Where(t => t.IsWaiting);
                    if (notDone.Count() > 0) { return notDone.First(); }
                }
                catch { }
                return null;
            }
        }

        private void DeleteTask(Task task)
        {
            lock (taskQueue)
            { taskQueue.Remove(task); }
            if (taskQueue.Where(t => t.IsWaiting).Count() > 0)
            { events.scheduleEvent.Set(); }
        }

        private void DynamicPool()
        {
            //TODO
        }

        ~ThreadPool()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (properties.isBusy)
            {
                controlThreads.PoolControlThread.Abort();
                events.pauseEvent.Dispose();
                foreach (Thread t in threadList)
                {
                    t.Abort();
                    events.eventCollection.ElementAt(t.ManagedThreadId).Value.Dispose();
                }
                properties.isBusy = false;
            }
        }
    }
}
