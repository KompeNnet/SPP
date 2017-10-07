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
        private Queue<Task> taskQueue = new Queue<Task>();

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
            //TODO
        }

        private void ThreadTaskExecute()
        {
            //TODO
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
                events.syncEvent.Dispose();
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
