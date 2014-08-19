using System;
using System.Collections.Generic;
using System.Linq;

namespace WintermintClient
{
    internal class RollingList<T>
    {
        private readonly Queue<T> items;

        private readonly int limit;

        public RollingList(int limit)
        {
            this.limit = limit;
            this.items = new Queue<T>(limit);
        }

        public int Count(Func<T, bool> predicate)
        {
            return this.items.Count<T>(predicate);
        }

        public void Push(T value)
        {
            while (this.items.Count >= this.limit)
            {
                this.items.Dequeue();
            }
            this.items.Enqueue(value);
        }
    }
}