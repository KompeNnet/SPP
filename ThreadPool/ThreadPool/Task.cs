using System;

namespace ThreadPool
{
    class Task
    {
        public bool IsWaiting { get; private set; } = true;
        private Func<dynamic> Act { get; set; }

        public Task(Func<dynamic> act)
        {
            Act = act;
        }

        public dynamic Execute()
        {
            lock (this)
            {
                IsWaiting = false;
                return Act();
            }
        }
    }
}
