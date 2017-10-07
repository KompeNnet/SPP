﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThreadPool.PoolAttributes;

namespace ThreadPool
{
    public class ThreadPool : IDisposable
    {
        private PoolProperties properties = new PoolProperties();
        private PoolEvents events = new PoolEvents();
        private PoolControlThreads controlThreads = new PoolControlThreads();

        private List<Thread> threadList;
        private List<Task> taskQueue = new List<Task>();

        public ThreadPool(int ThreadCountStatic)
        {
            properties.ThreadCountStatic = Math.Abs(ThreadCountStatic);
            SetPoolData(properties.ThreadCountStatic, properties.ThreadCountStatic);
            for (int i = 0; i < properties.ThreadCountStatic; i++)
            { StartNewThread(i); }
        }

        public ThreadPool(int minThreadCount, int maxThreadCount)
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

        public void Dispose()
        {
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

        public bool Execute(Action action)
        {
            lock (properties.lockConstruct)
            {
                if (action == null || properties.IsPaused) { return false; }
                AddTask(new Task(action));
                return true;
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

        private void AddTask(Task task)
        {
            lock (taskQueue) { taskQueue.Add(task); }
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
                Task task = GetTask();
                if (task != null) { ExecuteTask(task); }
            }
        }

        private void ExecuteTask(Task task)
        {
            try { task.Execute(); } finally { DeleteTask(task); }
            if (properties.IsPaused) { events.pauseEvent.Set(); }
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
            lock (taskQueue) { taskQueue.Remove(task); }
            if (taskQueue.Where(t => t.IsWaiting).Count() > 0)
            { events.scheduleEvent.Set(); }
        }

        private void DynamicPool()
        {
            int interval = properties.MaxThreadCount - properties.MinThreadCount;
            IEnumerable<Task> notDoneTasks = taskQueue.Where(t => t.IsWaiting);
            int threadsAdded = 0;
            while (true)
            {
                notDoneTasks = taskQueue.Where(t => t.IsWaiting);
                if (notDoneTasks.Count() != 0) { threadsAdded = IncreaseThreadAmount(threadsAdded, notDoneTasks); }
                if (properties.busyThreads < notDoneTasks.Count()) { threadsAdded = ReduceThreadAmount(threadsAdded); }
            }
        }

        private int IncreaseThreadAmount(int added, IEnumerable<Task> notDone)
        {
            while (added != notDone.Count() && added != properties.MaxThreadCount)
            {
                threadList.ElementAt(properties.busyThreads).Start();
                properties.busyThreads++;
                added++;
            }
            return added;
        }

        private int ReduceThreadAmount(int added)
        {
            while (added != properties.MinThreadCount)
            {
                threadList.ElementAt(properties.busyThreads--).Abort();
                added--;
            }
            return added;
        }

        ~ThreadPool()
        {
            Dispose(false);
        }
    }
}