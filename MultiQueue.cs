using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// A Multi-queue that will return the oldest item from a class hierarchy when dequeue() is called.
// If a specific type is requested the oldest instance of that type is returned.  All classes stored
// in the queue must derive from a common ancestor.
//
// Compiled: Visual Studio 2013

namespace MultiQueue 
{
    public class Letter
    {
        private string id;

        public Letter(string id)
        {
            this.id = id;
        }

        public override string ToString() { return id; }
    }

    public class A : Letter { public A(string id) : base(id) { } }
    public class B : Letter { public B(string id) : base(id) { } }
    public class C : Letter { public C(string id) : base(id) { } }

    public class MultiQueue<TBase> : IEnumerable
    {
        private class Item
        {
            public int Order { get; set; }
            public TBase Value { get; set; }
        }

        Dictionary<Type, Queue<Item>> queues = new Dictionary<Type, Queue<Item>>();
        int order = 0;

        public void Push<T>(T itemToInsert) where T : TBase
        {
            var type = typeof(T);
            if (!queues.ContainsKey(type))
                queues.Add(type, new Queue<Item>());

            var item = new Item() { Order = order++, Value = itemToInsert }; 
            queues[type].Enqueue(item);
        }

        public TBase DequeueAny()
        {
            // Find the queue with the oldest item ~ O(k) where k is number of types
            Queue<Item> minQueue = null;
            foreach(var queue in queues)
            {
                if (!queue.Value.Any())
                    continue;

                if (minQueue == null || minQueue.Peek().Order > queue.Value.Peek().Order)
                    minQueue = queue.Value;
            }

            return (minQueue != null) ? minQueue.Dequeue().Value : default(TBase);
        }

        public T Dequeue<T>() where T : TBase
        {
            var type = typeof(T);

            if (!queues.ContainsKey(type))
                return default(T);

            var item = queues[type].Dequeue();
            return (item != null) ? (T)item.Value : default(T);
        }

        public IEnumerator GetEnumerator()
        {
            // Dequeue the min of all types until no more items are left ~O(n*k)
            TBase current = DequeueAny();
            while(current != null)
            {
                yield return current;
                current = DequeueAny();
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var queue = new MultiQueue<Letter>();

            Console.WriteLine("Pop in order.");

            queue.Push(new A("A1"));
            queue.Push(new A("A2"));
            queue.Push(new B("B3"));
            queue.Push(new A("A4"));
            queue.Push(new B("B5"));
            queue.Push(new B("B6"));

            Console.WriteLine(queue.DequeueAny().ToString());
            Console.WriteLine(queue.DequeueAny().ToString());
            Console.WriteLine(queue.DequeueAny().ToString());
            Console.WriteLine(queue.DequeueAny().ToString());
            Console.WriteLine(queue.DequeueAny().ToString());
            Console.WriteLine(queue.DequeueAny().ToString());

            Console.WriteLine("Pop in order from enumerable.");

            queue.Push(new A("A1"));
            queue.Push(new A("A2"));
            queue.Push(new B("B3"));
            queue.Push(new A("A4"));
            queue.Push(new B("B5"));
            queue.Push(new B("B6"));

            foreach (var item in queue)
                Console.WriteLine(item.ToString());

            queue.Push(new A("A1"));
            queue.Push(new A("A2"));
            queue.Push(new B("B3"));
            queue.Push(new A("A4"));
            queue.Push(new B("B5"));
            queue.Push(new B("B6"));

            Console.WriteLine("Pop specific types.");

            Console.WriteLine(queue.Dequeue<B>().ToString());
            Console.WriteLine(queue.Dequeue<B>().ToString());
            Console.WriteLine(queue.Dequeue<A>().ToString());
            Console.WriteLine(queue.Dequeue<A>().ToString());
            Console.WriteLine(queue.Dequeue<A>().ToString());
            Console.WriteLine(queue.Dequeue<B>().ToString());

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);
        }
    }
}