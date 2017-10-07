using System;

namespace ThreadPool
{
    class Task
    {
        public bool IsWaiting { get; private set; } = true;
        public Action Act { private get; set; }

        public void Execute()
        {
            lock (this)
            {
                IsWaiting = false;
                Act();
            }
        }
    }
}
