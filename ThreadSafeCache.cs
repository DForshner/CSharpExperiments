using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Thread safe caching comparing:
// - Mutable dictionary with a lock(mutex) - Best for few threads with many writers.
// - Mutable dictionary with a reader/writer lock - Best for many readers few writers.
// - Immutable dictionary with no locking - Best for large numbers of threads. 

namespace ThreadSafeCache
{
    public interface ICache<TKey, TValue>
    {
        TValue Get(TKey key, Func<TValue> func);
    }

    /// <summary>
    /// Only one thread can read from the cache at at time.
    /// </summary>
    public class Cache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

        public TValue Get(TKey key, Func<TValue> func)
        {
            TValue val;
            bool found;

            lock (_cache)
            {
                found = _cache.TryGetValue(key, out val);
            }

            if (!found)
            {
                lock (_cache)
                {
                    // Double check that a previous thread that held the lock did not already insert the key.
                    if (_cache.TryGetValue(key, out val))
                        return val;

                    val = func();

                    _cache[key] = val;
                }
            }

            return val;
        }
    }

    /// <summary>
    /// Many threads can read as long as one thread is writing.  
    /// This works best when there are many readers and few writers.
    /// </summary>
    public class ReaderWriterCache<TKey, TValue> : ICache<TKey, TValue>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly IDictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

        public TValue Get(TKey key, Func<TValue> func)
        {
            TValue val;

            _lock.EnterReadLock();

            var found = _cache.TryGetValue(key, out val);
            _lock.ExitReadLock();

            if (!found)
            {
                _lock.EnterWriteLock();

                // Double check that a previous thread that held the lock did not already insert the key.
                if (_cache.TryGetValue(key, out val))
                {
                    _lock.ExitWriteLock();
                    return val;
                }

                _cache[key] = func();
                _lock.ExitWriteLock();
            }

            return val;
        }
    }

    public interface IPersistentDictionary<TKey, TValue>
    {
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Creates a new copy of the dictionary that includes the new key/value pair.
        /// </summary>
        IPersistentDictionary<TKey,TValue> Add(TKey key, TValue value);
    }

    /// <summary>
    /// A dictionary that doesn't change.
    /// </summary>
    public class PersistentDictionary<TKey,TValue> : IPersistentDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        public PersistentDictionary()
        {
            this.dictionary = new Dictionary<TKey, TValue>();
        }

        public PersistentDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public IPersistentDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var newDictionary = new Dictionary<TKey, TValue>(dictionary);
            newDictionary.Add(key, value);

            return new PersistentDictionary<TKey, TValue>(newDictionary);
        }
    }

    /// <summary>
    /// No locks required for cache hit.  Even when there are cache misses other threads
    /// can still be reading any cache hits can proceed normally.
    /// </summary>
    public class ImmutableCache<TKey, TValue> : ICache<TKey, TValue>
    {
        private IPersistentDictionary<TKey, TValue> _cache = new PersistentDictionary<TKey, TValue>();

        public TValue Get(TKey key, Func<TValue> func)
        {
            TValue val;

            if (!_cache.TryGetValue(key, out val))
            {
                lock (_cache)
                {
                    // Double check that a previous thread that held the lock did not already insert the key.
                    if (_cache.TryGetValue(key, out val))
                        return val;

                    val = func();

                    // C# memory model guarantees that a write to an object reference field will complete atomically
                    // so we don't have to lock to set a single variable. 
                    _cache = _cache.Add(key, val);
                }
            }

            return val;
        }
    }

    public class ThreadSafeCacheDemo
    {
        private const int MAX_THREADS = 500;
        private const int MAX_ITERATIONS = 10;

        // Smaller value gives more keys which gives more work.
        private const int WORK_STEP = 800000; 

        private const Int32 LARGEST_31BIT_PRIME = 2147483647; 

        private static int GCD(int a, int b)
        {
            int r, count = 0;
        
            while( b != 0 )
            {
                r = a % b;
                a = b;
                b = r;

                count++;
            }
         
            //System.Console.WriteLine("GCD Cycles: " + count.ToString());

            return a;
        }

        public class MyWorker
        {
            ICache<int, int> cache;

            public MyWorker(ICache<int, int> cache)
            {
                this.cache = cache;
            }

            public void DoWork()
            {
                //System.Console.WriteLine(Thread.CurrentThread.Name + " thread start.");

                for (int i = 1; i <= MAX_ITERATIONS; i++)
                {
                    for (UInt32 j = 1; (j <= LARGEST_31BIT_PRIME && j <= Int32.MaxValue); j += WORK_STEP)
                    {
                        var key = (Int32)j;
                        cache.Get(key, () => { return GCD(key, LARGEST_31BIT_PRIME); });
                    }
                }

                //System.Console.WriteLine(Thread.CurrentThread.Name + " thread complete.");
            }

        }

        static void Main(string[] args)
        {
            var threads = new List<Thread>();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            MyWorker worker = new MyWorker(new Cache<int, int>());
            Console.WriteLine("Starting " + MAX_THREADS + " threads with a mutable dictionary and a lock(mutex).");
            for (int i = 0; i < MAX_THREADS; i++)
            {
                Thread thread = new Thread(worker.DoWork);
                thread.IsBackground = true;
                thread.Name = string.Format("GCD worker thread using mutable cache {0}", i);
                threads.Add(thread);
                thread.Start();
            }
            var mutableSetup = sw.ElapsedMilliseconds;

            // Wait for all threads to finish
            foreach (var thread in threads)
                thread.Join();
            var mutableDone = sw.ElapsedMilliseconds;
            Console.WriteLine("Threads complete.");
            threads.Clear();

            worker = new MyWorker(new ReaderWriterCache<int, int>());
            Console.WriteLine("Starting " + MAX_THREADS + " threads with a mutable dictionary and a reader/writer lock.");
            for (int i = 0; i < MAX_THREADS; i++)
            {
                Thread thread = new Thread(worker.DoWork);
                thread.IsBackground = true;
                thread.Name = string.Format("GCD worker thread using reader writer cache {0}", i);
                threads.Add(thread);
                thread.Start();
            }
            var readerWriterSetup = sw.ElapsedMilliseconds;

            // Wait for all threads to finish
            foreach (var thread in threads)
                thread.Join();
            var readerWriterDone = sw.ElapsedMilliseconds;
            Console.WriteLine("Threads complete.");
            threads.Clear();

            worker = new MyWorker(new ImmutableCache<int, int>());
            Console.WriteLine("Starting " + MAX_THREADS + " threads with an immutable dictionary and no locking.");
            for (int i = 0; i < MAX_THREADS; i++)
            {
                Thread thread = new Thread(worker.DoWork);
                thread.IsBackground = true;
                thread.Name = string.Format("GCD worker thread using immutable cache {0}", i);
                threads.Add(thread);
                thread.Start();
            }
            var immutableSetup = sw.ElapsedMilliseconds;

            // Wait for all threads to finish
            foreach (var thread in threads)
                thread.Join();
            var immutableDone = sw.ElapsedMilliseconds;
            System.Console.WriteLine("Threads complete.");
            threads.Clear();

            System.Console.WriteLine("\t Mutable+Lock \t Mutable+ReadWrite \t Immutable+NoLock");
            System.Console.WriteLine("Setup:");
            System.Console.WriteLine("\t" + mutableSetup + " \t " + (readerWriterSetup - mutableDone) + " \t " + (immutableSetup - readerWriterDone));
            System.Console.WriteLine("Work:");
            System.Console.WriteLine("\t" + (mutableDone - mutableSetup) + " \t " + (readerWriterDone - readerWriterSetup) + " \t " + (immutableDone - immutableSetup));
            System.Console.WriteLine("Total:");
            System.Console.WriteLine("\t" + mutableDone + " \t " + (readerWriterDone - mutableDone) + " \t " + (immutableDone - readerWriterDone));
        }
    }
}