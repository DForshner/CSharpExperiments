using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Buffer pooling that reuses unused objects instead of allocating and de-allocating them.
// This prevents heap fragmentation and expensive GC compactions.
//
// Compiled: Visual Studio 2013

namespace BufferPool
{
    /// <summary>
    /// Responsible for allocating and managing the buffer pools.
    /// </summary>
    public class BufferPoolManager<T> where T : new()
    {
		private Dictionary<string, BufferPool<T>> buffers = new Dictionary<string, BufferPool<T>>();
		private Object synclock = new Object();

		public BufferPool<T> Create(string name, int initialCapacity, int bufferSize)
		{
            var pool = new BufferPool<T>(name, initialCapacity, bufferSize, ResizeNotify);

            lock (synclock)
                buffers.Add(name, pool);

            return pool;
        }

        public void ResizeNotify(BufferPool<T> buffer)
        {
            var future = buffer.CurrentCapacity + buffer.InitialCapacity;
            Console.WriteLine(String.Format("Resizing {0} from {1} to {2}", buffer.Name, buffer.CurrentCapacity, future));
        }

        public void Destroy(BufferPool<T> buffer)
        {
            lock (synclock)
                buffers.Remove(buffer.Name);
        }

        public void DisplayInfo()
        {
            lock (synclock)
            {
                Console.WriteLine("Snapshot of current buffers:");
                foreach (var b in buffers.Values)
                    Console.WriteLine(String.Format("Name: {0} Free: {1} Capacity: {2} Initial Capacity: {3} Resizes: {4}", 
                        b.Name, b.FreeBuffers, b.CurrentCapacity, b.InitialCapacity, b.Resizes));
            }
        }
    }

    /// <summary>
    /// Object pool of buffers.
    /// Implemented using a stack so "warm" buffers which are 
    /// most likely to still be in CPU cache have priority.
    /// </summary>
	public class BufferPool<T> where T : new()
	{
        public string Name { get; private set; }
		public int InitialCapacity {get; private set; }
        public int BufferSize { get; private set; }
		public int Resizes {get; private set; }

		private Stack<T[]> freeBuffers;
		private Object synclock = new Object();
        private Action<BufferPool<T>> resizeNotify;

        public BufferPool(string name, int initialCapacity, int bufferSize, Action<BufferPool<T>> resizeNotify)
		{
            this.Name = name;
			this.InitialCapacity = initialCapacity;
			this.BufferSize = bufferSize;
            this.Resizes = -1; // Will be zero after initial call to AddCapacity();
            this.resizeNotify = resizeNotify;

			freeBuffers = new Stack<T[]>(initialCapacity);
            AddCapacity();
		}

        public T[] Get()
        {
            lock (synclock)
            {
                if (freeBuffers.Any())
                    return freeBuffers.Pop();

                AddCapacity();

                return freeBuffers.Pop();
            }
        }

        private void AddCapacity()
        {
            if (resizeNotify != null)
                resizeNotify(this);

            ++Resizes;

            for (int i = 0; i < InitialCapacity; ++i)
                freeBuffers.Push(new T[BufferSize]);
        }

        public int CurrentCapacity { get { return this.InitialCapacity * (this.Resizes + 1); } }

        public int FreeBuffers { get { return freeBuffers.Count; } }

        public void Free(T[] buffer)
        {
            if (buffer == null)
                return;

            Debug.Assert(buffer.Count() == this.BufferSize, "Expected buffer being freed to be correct size.");

            lock (synclock)
                freeBuffers.Push(buffer);
        }
	}

    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Creating buffer pools");
            var bufferManager = new BufferPoolManager<byte>();
            var bufferPoolA = bufferManager.Create("Buffer A", 2, 128);
            var bufferPoolB = bufferManager.Create("Buffer B", 5, 256);
            bufferManager.DisplayInfo();

            Console.WriteLine("Getting buffers from pools");
            var pileA = new List<Byte[]>();
            var pileB = new List<Byte[]>();
            for (var i = 0; i <= 6; ++i)
            {
                pileA.Add(bufferPoolA.Get());
                pileB.Add(bufferPoolB.Get());
            }
            bufferManager.DisplayInfo();

            Console.WriteLine("Destroying buffer pool A and freeing all buffers from B");
            bufferManager.Destroy(bufferPoolA);
            foreach (var b in pileB)
                bufferPoolB.Free(b);
            bufferManager.DisplayInfo();
        }
    }
}
