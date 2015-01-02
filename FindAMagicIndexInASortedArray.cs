using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

// Given a sorted array of non-distinct integers find
// a magic index (one where A[i] = i) if one exists.

namespace FindAMagicIndexInASortedArray
{
    public static class MagicIndexFinder 
    {
        private const int FAILED_TO_LOCATE_SENTINEL = -1;

        public static Tuple<bool, int> Find(IList<int> arrayToSearch)
        {
            if (!arrayToSearch.Any())
            {
                return Tuple.Create(false, FAILED_TO_LOCATE_SENTINEL);
            }

            var left = 0;
            var right = arrayToSearch.Count - 1;

            return Locate(arrayToSearch, left, right);
        }

        private static Tuple<bool, int> Locate(IList<int> toSearch, int left, int right)
        {
            if (right < left 
                || left < 0
                || right >= toSearch.Count)
            {
                // Unable to find magic index
                return Tuple.Create(false, FAILED_TO_LOCATE_SENTINEL);
            }

            int mid = (left + right) / 2;
            var midValue = toSearch[mid];
            if (mid == midValue)
            {
                // Found magic index
                return Tuple.Create(true, mid);
            }

            // Searching left
            // Since the values are always ascending the last possible location from that middle 
            // that could be a magic index is at midValue.  The values between A[midValue + 1] and A[mid]
            // have to have values that are less than midValue (which is greater than their associated indexes) 
            // which means they can't be magic numbers.
            var leftEnd = Math.Min(mid - 1, midValue);
            var leftResult = Locate(toSearch, left, leftEnd);
            if (leftResult.Item1)
            {
                return leftResult;
            }

            // Searching right
            // Since the values are always ascending the first possible location from the middle
            // that could be a magic index is at midValue.  The values between A[mid + 1] and A[midValue]
            // have to have values that are greater than midValue (which is greater than their associated indexes)
            // which means they can't be magic numbers.
            var rightStart = Math.Max(mid + 1, midValue);
            var rightResult = Locate(toSearch, rightStart, right);

            return rightResult;
        }
    }

    [TestClass]
    public class MagicIndexFinderTests
    {
        [TestMethod]
        public void WhenEmpty_ExpectFailure()
        {
            var result = MagicIndexFinder.Find(Enumerable.Empty<int>().ToList());
            Assert.IsFalse(result.Item1);
            Assert.AreEqual(-1, result.Item2);
        }

        [TestMethod]
        public void WhenZeroValueAtZeroIndex_ExpectMagicIndexAtFirstElement()
        {
            var result = MagicIndexFinder.Find(new[] { 0 });
            Assert.IsTrue(result.Item1);
            Assert.AreEqual(0, result.Item2);
        }

        [TestMethod]
        public void WhenElementCountMinusOneValueAtMaxElement_ExpectMagicIndexAtMaxElementLocation()
        {
            var result = MagicIndexFinder.Find(Enumerable.Repeat(10, 11).ToList()); // [10, ..., 10]
            Assert.IsTrue(result.Item1);
            Assert.AreEqual(10, result.Item2);
        }

        [TestMethod]
        public void WhenTestSequencesWithMagicValues_ExpectFindCorrectMagicValue()
        {
            var testTable = new[]
                {
                    new { Expect = 2, ToTest = new[] { int.MinValue, int.MinValue, 2, int.MaxValue, int.MaxValue } },
                    new { Expect = 3, ToTest = new[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, } },
                    new { Expect = 7, ToTest = new[] { 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, } },
                    new { Expect = 2, ToTest = new[] { -1, -1, 2, 11, 11, 11, 11, 11, 11, 11, 11 } },
                    new { Expect = 10, ToTest = new[] { 1, 4, 4, 4, 8, 9, 10, 10, 10, 10, 10 } }
                };

            foreach (var test in testTable)
            {
                var result = MagicIndexFinder.Find(test.ToTest);
                Assert.IsTrue(result.Item1);
                Assert.AreEqual(test.Expect, result.Item2);
            }
        }

        [TestMethod]
        public void WhenTestSequencesWithoutMagicValues_ExpectNoMagicIndexFound()
        {
            var testTable = new[]
                {
                    new { ToTest = new[] { 3, 3, 3 } },
                    new { ToTest = new[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, int.MaxValue } },
                    new { ToTest = new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 } },
                };

            foreach (var test in testTable)
            {
                var result = MagicIndexFinder.Find(test.ToTest);
                Assert.IsFalse(result.Item1);
            }
        }

    }
}