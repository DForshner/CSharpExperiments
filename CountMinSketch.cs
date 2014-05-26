using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Count Min Sketch 
//
// TODO: The estimates are off more than I'd like.  Probably need better hash functions. 
//
// Compiled : C# Visual Studio 2013

namespace CountMinSketch
{
    /// <summary>
    /// Count Min Sketch (CM Sketch) is a probabilistic streaming algorithm that can be used to estimate the number of times
    /// (frequency) that a given item has been seen.  By accepting a some error in our measurements
    /// we can save storage space when dealing with large amounts of data.  They are similar to bloom filters in that they can only give
    /// a lower bound on the number of times a value could have seen.
    /// </summary>
    public class CountMinSketch
    {
        #region FIELDS / PROPERTIES

        /// <summary>
        /// Helper class that keeps track of hash functions and their associated counts.
        /// </summary>
        private class HashCounter
        {
            private Func<int, int> HashFunction;
            public int[] Counts {get; private set; }

            public HashCounter(Func<int, int> function, int size)
            {
                this.HashFunction = function;
                this.Counts = new int[size];
            }

            /// <summary>
            /// Increment the counter associated with a given hash key.
            /// </summary>
            public void IncrementCount(int value)
            {
                var hashKey = this.HashFunction(value);
                Debug.Assert(hashKey >= 0 && hashKey < this.Counts.Count());
                this.Counts[hashKey]++;
            }

            /// <summary>
            /// Increment the counter associated with a given hash key.
            /// </summary>
            public int GetCount(int value)
            {
                var hashKey = this.HashFunction(value);
                Debug.Assert(hashKey >= 0 && hashKey < this.Counts.Count());
                return this.Counts[hashKey];
            }
        }

        public int Size { get; private set; }

        public int NumberOfHashes 
        { 
            get {return hashes.Count; }
        }

        private List<HashCounter> hashes;

        #endregion

        #region CONSTRUCTORS

        public CountMinSketch(int size)
        {
            this.Size = size;
            Clear();
        }

        #endregion

        public void Clear()
        {
            // TODO: The hashes should probably be passed into the min sketch class & use a strategy pattern.
            this.hashes = new List<HashCounter>();
            this.hashes.Add(new HashCounter((int value) => { return value.GetHashCode() % Size; }, Size));
            this.hashes.Add(new HashCounter((int value) => { return (value ^ (value + 31) + value) % Size; }, Size));
            this.hashes.Add(new HashCounter((int value) => { return ((Size - 1) + value + 7) % Size; }, Size));
        }

        public void Insert(int value)
        {
            foreach (var hash in this.hashes)
                hash.IncrementCount(value);
        }

        public int GetSketchCount(int value)
        {
            var min = int.MaxValue;
            foreach (var hash in hashes)
                min = Math.Min(min, hash.GetCount(value));

            return min;
        }

        public IEnumerable<int> GetCountArray(int index)
        {
            if (index >= this.NumberOfHashes) { throw new IndexOutOfRangeException(); }

            var hash = this.hashes[index];
            foreach (var count in this.hashes[index].Counts)
                yield return count;
        }
    }

    public static class Program
    {
        /// <summary>
        /// Number of values to insert.
        /// </summary>
        private const int NUMBER_OF_VALUES_TO_INSERT = 1000;

        /// <summary>
        /// Number of counts to store for each hash function.
        /// </summary>
        private const int SKETCH_HASH_ARRAY_SIZE = 100;

        /// <summary>
        /// The maximum value to randomly generate.
        /// </summary>
        private const int MAX_VALUE_SIZE = 200;

        public static void Main()
        {
            var count = new CountMinSketch(SKETCH_HASH_ARRAY_SIZE);

            var actualCounts = new Dictionary<int, int>();
            var rnd = new Random(DateTime.Now.Second * DateTime.Now.Millisecond); // Could be better.
            foreach(var i in Enumerable.Range(0, NUMBER_OF_VALUES_TO_INSERT))
            {
                var value = rnd.Next(MAX_VALUE_SIZE);
                if (actualCounts.ContainsKey(value))
                    actualCounts[value]++;
                else
                    actualCounts.Add(value, 1);

                count.Insert(value);
            }

            DisplayCounts(count);
                
            Console.WriteLine("Val \t Act \t Est \t Error");
            foreach (var key in actualCounts.Keys)
            {
                var actual = actualCounts[key];
                var estimate = count.GetSketchCount(key);
                var error = actual - estimate;

                Debug.Assert(estimate >= actual, "The estimate should always greater than or equal to the actual (Count Min ...).");

                double percent = (actual > 0 ) ? Math.Abs((double)error / (double)actual) : 0D;

                Console.WriteLine("({0}) \t {1} \t {2} \t {3} ({4}%)", key, actual, estimate, error, Math.Round(percent * 100, 1));
            }

            Console.WriteLine("Press [Enter Key] to exit.");
            Console.ReadLine();
        }

        private static void DisplayCounts(CountMinSketch count)
        {
            for (var i = 0; i < count.NumberOfHashes; i++)
            {
                Console.Write("\n(" + i + ")=>");
                foreach (var value in count.GetCountArray(i))
                    Console.Write("," + value);
                Console.WriteLine();
            }
        }
    }
}