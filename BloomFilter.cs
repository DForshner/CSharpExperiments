using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BloomFilter
{
    public interface IHashStrategy<TObject>
    {
        IEnumerable<int> GetIndexes(TObject item, int numOfBits, int numOfHashes);
    }

    /// <summary>
    /// A bloom filter is a probabilistic data structure that can be used to determine if an item is part of a set or not.  Space
    /// efficiency is achieve by trading for the possibility of false positives.  As more elements are added to the set the probablity
    /// of a false positive (says item is in set when it isn't) increases.
    /// See: http://en.wikipedia.org/wiki/Bloom_filter
    /// </summary>
    public class BloomFilter<T>
    {
        private BitArray set;

        private IHashStrategy<T> hasher;

        /// <summary>
        /// n = Expected number of elements 
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// m = Number of bits in bit array.
        /// </summary>
        public int NumberOfBits { get; private set; }

        /// <summary>
        /// k = Number of hash functions. 
        /// </summary>
        public int NumberOfHashes { get; private set; }

        public int Count { get; private set; }

        /// <summary>
        /// Creates a bloom filter with the specified capacity and false positive rate. 
        /// </summary>
        /// <param name="capacity">Max number of items</param>
        /// <param name="falsePositiveRate">False positive rate at max capacity.</param>
        public BloomFilter(int capacity, double falsePositiveRate, IHashStrategy<T> hasher)
        {
            this.Capacity = capacity;
            this.hasher = hasher;

            double bits = -(capacity * Math.Log(falsePositiveRate)) / Math.Pow(Math.Log(2), 2);
            this.NumberOfBits = (int)bits;

            double hashes = -Math.Log(0.7) * bits / capacity;
            this.NumberOfHashes = (int)hashes;

            this.set = new BitArray(NumberOfBits);
        }

        public void Clear()
        {
            this.set = new BitArray(NumberOfBits);
            Count = 0;
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public bool IsFull 
        {
            get { return Count == Capacity; }
        }

        private static double c = Math.Pow(Math.Log(2), 2);
        public double CurrentFalsePositiveRate
        {
            get 
            { 
                if (Count == 0) { return 0D; }
                return Math.Pow(Math.E, -(c * (double)NumberOfBits / (double)Count));
            }
        }

        public void Insert(T item)
        {
            if (Count >= Capacity) 
                throw new Exception("Maximum false positive rate reached"); 

            foreach (int index in Probe(item))
                set.Set(index, true);

            Count++;
        }

        public bool Contains(T item)
        {
            foreach (int index in Probe(item))
                if (!set.Get(index))
                    return false;
            return true;
        }

        /// <summary>
        /// Return k array positions that correspond to a k hashes of the item.
        /// </summary>
        private IEnumerable<int> Probe(T item)
        {
            return hasher.GetIndexes(item, NumberOfBits, NumberOfHashes);
        }
    }

    public class IntHasher : IHashStrategy<int>
    {
        HashAlgorithm hasher = MD5.Create(); 

        // For a good hash function with a wide output, there should be little if any correlation between different bit-fields, 
        // so this type of hash can be used to generate multiple "different" hash functions by slicing its output into multiple bit fields. 
        public IEnumerable<int> GetIndexes(int item, int numOfBits, int numOfHashes)
        {
            int num32BitSegs = hasher.HashSize / sizeof(int) * 8 ;
            var itemBytes = BitConverter.GetBytes(item); 

            for (int i = 0; i < numOfHashes;)
            {
                byte[] hash = hasher.ComputeHash(itemBytes);
                for (int j = 0; j < num32BitSegs && i < numOfHashes; j++, i++)
                {
                    yield return Math.Abs(BitConverter.ToInt32(hash, j)) % numOfBits;
                }
            }
        }
    }

    public class StringHasher : IHashStrategy<string>
    {
        public IEnumerable<int> GetIndexes(string item, int numOfBits, int numOfHashes)
        {
            if (numOfHashes == 0) { yield break; }

            int previousHash = item.GetHashCode();
            yield return previousHash;

            for (var i = 1; i < numOfHashes; i++)
            {
                var hash = previousHash.GetHashCode();
                yield return hash % numOfBits;
                previousHash = hash;
            }
        }
    }

    [TestClass]
    public class BloomFilterTests 
    {
        [TestMethod]
        public void WhenNew_ExpectIsEmpty()
        {
            var sut = new BloomFilter<int>(10, 0.05D, new IntHasher());
            Assert.IsTrue(sut.IsEmpty);
            Assert.AreEqual(0, sut.Count);
        }

        [TestMethod]
        public void WhenEmpty_ExpectZeroFalsePositiveRate()
        {
            var sut = new BloomFilter<int>(0, 0.05D, new IntHasher());
            Assert.AreEqual(0D, sut.CurrentFalsePositiveRate);
        }

        [TestMethod]
        public void WhenFull_ExpectInsertCausesException()
        {
            var sut = new BloomFilter<int>(10, 0.05D, new IntHasher());
            FillWithRandom(sut, sut.Capacity);
            Assert.AreEqual(0.05D, Math.Round(sut.CurrentFalsePositiveRate, 2));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void WhenFull_ExpectSpecifiedFalsePositiveRate()
        {
            var sut = new BloomFilter<int>(10, 0.05D, new IntHasher());
            FillWithRandom(sut, sut.Capacity);
            sut.Insert(11);
        }

        [TestMethod]
        public void WhenReasonableFalsePositiveRate_ExpectLessBitsThanDeterministically()
        {
            const int SIZE = 1000;
            var sut = new BloomFilter<int>(SIZE, 0.02D, new IntHasher());
            var deterministicSize = SIZE * sizeof(int) * 8;
            Assert.IsTrue(deterministicSize > sut.NumberOfBits);
        }

        [TestMethod]
        public void WhenAlmostFullDoesNotContainItem_ExpectContainsAfterItemAdded()
        {
            var sut = GetSUTWithout1234();
            Assert.IsFalse(sut.Contains(1234));
            sut.Insert(1234);
            Assert.IsTrue(sut.Contains(1234));
        }

        /// <summary>
        /// Keep building new filters until no false positives for 1234.
        /// </summary>
        private static BloomFilter<int> GetSUTWithout1234()
        {
            BloomFilter<int> sut = null;
            bool exists = true;
            while (exists)
            {
                sut = new BloomFilter<int>(10000, 0.02D, new IntHasher());
                FillWithRandom(sut, 9000);
                exists = sut.Contains(1234);
            }
            return sut;
        }

        /// <summary>
        /// Fill with random numbers between 0 and 100.
        /// </summary>
        private static void FillWithRandom(BloomFilter<int> sut, int n)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            foreach (var i in Enumerable.Range(0, n)) { sut.Insert(rnd.Next(int.MaxValue)); }
        }

        /// <summary>
        /// Fill with random numbers between 0 and 100.
        /// </summary>
        private static void FillWithRandom(BloomFilter<int> sut, int n, int maxValue)
        {
            var rnd = new Random(DateTime.Now.Millisecond);
            foreach (var i in Enumerable.Range(0, n)) { sut.Insert(rnd.Next(maxValue)); }
        }
    }

    //public static class Program
    //{
    //    public static void Main()
    //    {
    //        var hasher = new IntHasher();
    //        var filter = new BloomFilter<int>(100, 0.02F, hasher);
    //        Display(filter);

    //        filter.Insert(1);
    //        filter.Insert(3);
    //        filter.Insert(5);
    //        filter.Insert(5);
    //        Display(filter);

    //        var rnd = new Random(DateTime.Now.Millisecond);
    //        foreach (var i in Enumerable.Range(1, 1000))
    //        {
    //            try
    //            {
    //                filter.Insert(rnd.Next(5));
    //            }
    //            catch
    //            {
    //                Console.WriteLine("Maximum capacity reached");
    //                break;
    //            }
    //        }
    //        Display(filter);

    //        Console.WriteLine("Press [Enter Key] to exit.");
    //        Console.ReadLine();
    //    }

    //    private static void Display(BloomFilter<int> filter)
    //    {
    //        foreach (var i in Enumerable.Range(0, 10))
    //        {
    //            Console.Write("\n({0}) : ({1})", i, filter.Contains(i));
    //        }
    //        Console.WriteLine("\nFilter has {0} elements stored in {1} bits using {2} hashes", 
    //            filter.Count, filter.NumberOfBits, filter.NumberOfHashes);
    //        Console.WriteLine("with a false positive rate of {0}\n", filter.CurrentFalsePositiveRate);
    //    }
    //}
}