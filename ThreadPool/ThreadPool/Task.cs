using System;

namespace ThreadPool
{
    class Task
    {
        public bool IsWaiting { get; private set; } = true;
        private Action Act { get; set; }

        public Task(Action act)
        {
            Act = act;
        }

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
