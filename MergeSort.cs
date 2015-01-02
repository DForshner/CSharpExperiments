using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

// Merge sort is a stable sort with O(nlog(n)) time complexity and O(N^2) space complexity.
//
// Compiled: Visual Studio 2013

namespace MergeSort 
{
    public static class MergeSort
    {
        public static void Sort(int[] toSort)
        {
            if (toSort == null) { throw new ArgumentNullException(); }
            SortPartition(toSort, 0, toSort.Count() - 1);
        }

        private static void SortPartition(int[] toSort, int left, int right)
        {
            // Base case
            if (right <= left)
                return;

            var mid = (right + left) / 2;
            SortPartition(toSort, left, mid);
            SortPartition(toSort, mid + 1, right);

            CombinePartitions(toSort, left, (mid + 1), right);
        }

        private static void CombinePartitions(int[] toSort, int leftStart, int mid, int rightEnd)
        {
            var leftPos = leftStart;
            int leftEnd = (mid - 1);

            var rightPos = mid;

            // Create temporary array to hold sorted values
            int numElements = (rightEnd - leftStart + 1);
            var temp = new int[numElements];
            int curr = 0;

            // Combining the two sections always taking the smaller element
            while (leftPos <= leftEnd && rightPos <= rightEnd)
            {
                if (toSort[leftPos] <= toSort[rightPos])
                    temp[curr++] = toSort[leftPos++];
                else
                    temp[curr++] = toSort[rightPos++];
            }

            // Combine remaining elements in left partition 
            while (leftPos <= leftEnd)
                temp[curr++] = toSort[leftPos++];

            // Combine remaining elements in right partition 
            while (rightPos <= rightEnd)
                temp[curr++] = toSort[rightPos++];

            // Write back the temporary array elements into the original array
            Array.Copy(temp, 0, toSort, leftStart, numElements);
        }
    }

    public static class ArrayExtensions
    {
        public static void FisherYatesShuffle(this int[] toShuffle)
        {
            if (toShuffle == null) { throw new ArgumentNullException(); }

            int idx = toShuffle.Length - 1;
            var rnd = new Random();

            while (idx >= 0)
            {
                var eleToSwap = rnd.Next(idx);
                var temp = toShuffle[eleToSwap];
                toShuffle[eleToSwap] = toShuffle[idx];
                toShuffle[idx] = temp;
                idx--;
            }
        }
    }
 
    [TestClass]
    public class MergeSortTests 
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhenNull_ExpectArgumentException()
        {
            int[] array = null;
            MergeSort.Sort(array);
        }

        [TestMethod]
        public void WhenZeroElements_ExpectEmpty()
        {
            var array = new int[0];
            MergeSort.Sort(array);
            Assert.IsNotNull(array);
            Assert.IsFalse(array.Any());
        }

        [TestMethod]
        public void WhenOneElement_ExpectOneElementReturned()
        {
            var array = new[] { 3 };
            MergeSort.Sort(array);
            Assert.AreEqual(3, array[0]);
        }

        [TestMethod]
        public void WhenManyElements_ExpectSortedIncreasingOrderIncreasing()
        {
            const int SIZE = 100;
            var original = Enumerable.Range(0, SIZE).ToArray();
            var array = new int[SIZE];
            Array.Copy(original, array, SIZE);
            array.FisherYatesShuffle();

            MergeSort.Sort(array);

            Assert.IsTrue(array.SequenceEqual(original));
        }
    }
}