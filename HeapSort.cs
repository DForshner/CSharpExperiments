using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

// Heap Sort performs an unstable in place sort.
// Time complexity: O(nlog n) as the heapify operation is O(log(n)) and it is performed n times.
// Space complexity: O(1) as it sorts in place.
// While slower than quick sort it guarantees a worst case performance of O(nlogn) vs. quick sort's O(n^2).
// While slower than merge sort it uses constant space vs. merge sort's O(n) space requirement. 
// Compiled: Visual Studio 2013

namespace HeapSort
{
    public static class HeapSort
    {
        public static void Sort<T>(this IList<T> toSort) where T : IComparable<T>
        {
            if (toSort == null) { throw new ArgumentNullException(); }

            BuildHeap(toSort);

            var heapSize = toSort.Count;
            for (var i = toSort.Count - 1; i >= 0; i--)
            {
                // Since the max element is now in position one swap swap it with last element.
                Swap(toSort, 0, i);

                // Reduce the range of unsorted values by one.
                heapSize--;

                // The new root may violate the heap order property to so Adjust heap so run heapify again to restore order.
                Heapify(toSort, 0, heapSize);
            }
        }

        /// <summary>
        /// Recursively work from middle to root check that for each level the parent node
        /// is larger than its children.  When this is complete the entire tree will be
        /// ordered.
        /// </summary>
        private static void BuildHeap<T>(IList<T> collection) where T : IComparable<T>
        {
            var heapSize = collection.Count;
            for (var i = heapSize / 2; i >= 0; i--)
            {
                Heapify(collection, i, heapSize);
            }
        }

        /// <summary>
        /// If the parent element at the current level is not greater than both of its children swap
        /// and then work downwards through the levels ensuring the parent element is always larger
        /// then the child elements at each level.
        /// Complexity: O(log n)
        /// </summary>
        private static void Heapify<T>(IList<T> collection, int parentIdx, int heapSize) where T : IComparable<T>
        {
            int leftChildIdx = 2 * parentIdx + 1;
            int rightChildIdx = 2 * parentIdx + 2;
            int largest = parentIdx;

            if (leftChildIdx < heapSize && collection[leftChildIdx].CompareTo(collection[parentIdx]) > 0)
            {
                largest = leftChildIdx;
            }

            if (rightChildIdx < heapSize && collection[rightChildIdx].CompareTo(collection[largest]) > 0)
            {
                largest = rightChildIdx;
            }

            if (largest != parentIdx)
            {
                // Move the larger child into the parent's position
                Swap(collection, parentIdx, largest);
                Heapify(collection, largest, heapSize);
            }
        }

        /// <summary>
        /// In place swap
        /// </summary>
        private static void Swap<T>(IList<T> collection, int x, int y)
        {
            var temp = collection[x];
            collection[x] = collection[y];
            collection[y] = temp;
        }
    }

    [TestClass]
    public class HeapSortTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhenSortNull_ExpectException()
        {
            IList<int> empty = null;
            HeapSort.Sort(empty);
        }

        [TestMethod]
        public void WhenSortEmptyCollection_ExpectNoChange()
        {
            var values = new List<int>(); 
            HeapSort.Sort(values);
            Assert.IsTrue(values.SequenceEqual(new List<int>()));
        }

        [TestMethod]
        public void WhenReversedSorted_ExpectNoLongerReversed()
        {
            var values = new List<int> { 3, 2, 1 };
            HeapSort.Sort(values);
            Assert.IsFalse(values.SequenceEqual(new[] { 3, 2, 1 }));
        }

        [TestMethod]
        public void WhenSortIntValues_ExpectSortedCorrectly()
        {
            var testCases = new[]
            {
                Tuple.Create(new List<int> { 1, 2, 3 }, new List<int> { 1, 2, 3 }),
                Tuple.Create(new List<int> { 2, 2, 3, 3, 3, 1, 4, 4, 4, 4 }, new List<int> { 1, 2, 2, 3, 3, 3, 4, 4, 4, 4 }),
            };
            foreach (var testCase in testCases)
            {
                HeapSort.Sort(testCase.Item1);
                Assert.IsTrue(testCase.Item1.SequenceEqual(testCase.Item2));
            }
        }

        [TestMethod]
        public void WhenSortFloatValues_ExpectSortedCorrectly()
        {
            var testCases = new[]
            {
                Tuple.Create(new List<float> { 2.1F, 2.11F, 3.1F, 3.111F, 3.11F }, new List<float> { 2.1F, 2.11F, 3.1F, 3.11F, 3.111F }),
                Tuple.Create(new List<float> { 2.1F, 2.2F, 3.3F, 3.2F, 3.1F, 1.1F }, new List<float> { 1.1F, 2.1F, 2.2F, 3.1F, 3.2F, 3.3F }),
            };
            foreach (var testCase in testCases)
            {
                HeapSort.Sort(testCase.Item1);
                Assert.IsTrue(testCase.Item1.SequenceEqual(testCase.Item2));
            }
        }

    }
}