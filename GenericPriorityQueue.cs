using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GenericPriorityQueue 
{
    /// <summary>
    /// Generic priority queue that dequeues items in order of their
    /// associated priority.  Items with the same priority level are 
    /// dequeued in FIFO order. 
    /// </summary>
    public class PriorityQueue<TPriority, TItem> : IEnumerable<TItem>
    {
        private readonly SortedDictionary<TPriority, Queue<TItem>> queues;

        /// <param name="priorityComparer">Comparer that determines which priority is more important.</param>
        public PriorityQueue(IComparer<TPriority> priorityComparer)
        {
            this.queues = new SortedDictionary<TPriority, Queue<TItem>>(priorityComparer);
        }

        public PriorityQueue() : this(Comparer<TPriority>.Default) { }

        public bool HasItems
        {
            get { return queues.Any(); }
        }

        public int Count
        {
            get { return queues.Sum(x => x.Value.Count); }
        }

        /// <summary>
        /// Adds new item to queue.
        /// </summary>
        public void Enqueue(TPriority priority, TItem item)
        {
            // If priority level doesn't already exist create it.
            if (!this.queues.ContainsKey(priority))
                this.queues.Add(priority, new Queue<TItem>());

            this.queues[priority].Enqueue(item);
        }

        /// <summary>
        /// Removes highest priority item from queue.  Throws exception if priority queue is empty.
        /// </summary>
        public TItem Dequeue()
        {
            if (!this.queues.Any())
                return DequeueHighestPriorityItem();
            else
                throw new InvalidOperationException("Queue is empty.");

        }

        private TItem DequeueHighestPriorityItem()
        {
            // Remove item from highest available priority queue.
            var highestPriorityQueue = queues.First();
            var highestItem = highestPriorityQueue.Value.Dequeue();

            // If priority level is empty remove the queue.
            if (!highestPriorityQueue.Value.Any())
                this.queues.Remove(highestPriorityQueue.Key);

            return highestItem;
        }

        /// <summary>
        /// Returns the highest priority item without removing it from the queue.
        /// </summary>
        public TItem Peek()
        {
            var highestPriorityQueue = queues.First();
            return highestPriorityQueue.Value.Peek();
        }

        public System.Collections.Generic.IEnumerator<TItem> GetEnumerator()
        {
            while (HasItems)
                yield return DequeueHighestPriorityItem(); 
        }

        // Explicit for IEnumerable because weakly typed collections are Bad
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var queue = new PriorityQueue<byte, string>();

            queue.Enqueue(6, "H");
            queue.Enqueue(4, "E");
            queue.Enqueue(3, "D");
            queue.Enqueue(2, "B");
            queue.Enqueue(4, "F");
            queue.Enqueue(2, "C");
            queue.Enqueue(4, "G");
            queue.Enqueue(1, "A");

            var sb = new StringBuilder();
            foreach (var item in queue)
                sb.Append(item);
            var result = sb.ToString();

            Debug.Assert(result == "ABCDEFGH");
            Console.WriteLine("Result: {0}", result);

            Console.WriteLine("Press [Enter Key] to exit.");
            Console.ReadLine();
        }
    }
}
