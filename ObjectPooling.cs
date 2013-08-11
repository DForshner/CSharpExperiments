using System;
using System.Collections.Generic;
using System.Linq;

// Object Pooling Pattern
// Use an object pool to re-use unused objects instead of allocating and de-allocating them.
// This prevents heap fragmentation and expensive GC compactions.
//
// Visual Studio 2012, .NET 4.5

namespace ObjectPooling
{
    /// <summary>
    /// An object pool behaves like a factory.
    /// </summary>
    public class ObjectPool<T> where T : new()
    {
        /// <summary>
        /// Stack that holds previously created objects.
        /// Stack provides O(1) insert/remove.
        /// </summary>
        private Stack<T> items = new Stack<T>();

        private Object synclock = new Object();

        /// <summary>
        /// Creates a new object if one isn't available or re-uses
        /// a previously created object.
        /// </summary>
        public T Get()
        {
            lock (synclock)
            {
                if (items.Count == 0)
                    return new T();
                else
                    return items.Pop();
            }
        }

        public void Free(T item)
        {
            lock (synclock)
            {
                items.Push(item);
            }
        }
    }

    public class Message
    {
        public Message()
        {
            this.Contents = new string[100];
        }

        public string[] Contents { get; set; }
    }

    public class Program
    {
        private static ObjectPool<Message> pool = new ObjectPool<Message>();
        private const int RUNS = 10000000;

        public static void Main()
        {
            Int64 totalChars = 0;

            for (var i = 0; i < RUNS; i++)
            {
                var message = GetDataUsingMessagePool();
                totalChars += ProcessDataUsingMessagePool(message);
            }

            var gc0 = GC.CollectionCount(0);
            var gc1 = GC.CollectionCount(1);
            Console.WriteLine("GC collections when using Object Pool {0}, {1}", gc0, gc1);

            // Force collection
            GC.Collect(2);

            for (var i = 0; i < RUNS; i++)
            {
                var message = GetData();
                totalChars += ProcessData(message);
            }
            Console.WriteLine("GC collections when just creating new objects {0}, {1}", GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1);
        }

        /// <summary>
        /// Simulate Getting data
        /// </summary>
        private static Message GetDataUsingMessagePool()
        {
            var message = pool.Get();
            message.Contents[0] = "Payload";
            return message;
        }

        /// <summary>
        /// Simulate processing data and returning a result.
        /// </summary>
        private static Int64 ProcessDataUsingMessagePool(Message message)
        {
            var count = message.Contents[0].Count();

            message.Contents[0] = String.Empty;
            pool.Free(message);
            return count;
        }

        private static Message GetData()
        {
            var message = new Message();
            message.Contents[0] = "Payload";
            return message;
        }

        private static Int64 ProcessData(Message message)
        {
            return message.Contents[0].Count();
        } 
    }

}
