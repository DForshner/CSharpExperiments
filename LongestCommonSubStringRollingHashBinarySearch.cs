using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

// Find the longest common substring between two strings.
// Solved using a rolling hash and binary search
// Time Complexity: O(nLog(n))

namespace LongestCommonSubStringRollingHashBinarySearch
{
    public static class LongestCommonSubStringFinder
    {
        private const long HASH_MOD = long.MaxValue / 2;

        public static int FindLongestCommonSubstring(String a, String b)
        {
            if (a == null || b == null) { return 0; }
            if (a.Length == 0 || b.Length == 0) { return 0; }

            // Binary search all possible substring lengths
            int left = 1;
            int right = Math.Min(a.Length, b.Length);
            int lastFound = 0;
            while (left <= right)
            {
                int len = (left + right) / 2;

                // Hash all possible substrings of length n and store them by hash value.
                var aSubStrings = GetStartIndexesOfSubstringsByHash(a, len);
                var bSubStrings = GetStartIndexesOfSubstringsByHash(b, len);

                // Compare pairs of substrings that have the same hash value checking for a string match
                var foundMatch = false;
                foreach(var hash in aSubStrings.Keys)
                {
                    var bSubStringsToCheck = bSubStrings.ContainsKey(hash) ? bSubStrings[hash] : Enumerable.Empty<int>();
                    if (CheckForMatchingSubstringPair(a, b, len, aSubStrings[hash], bSubStringsToCheck))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                if (foundMatch)
                {
                    // Found a substring match for this length so try a longer substring
                    lastFound = len;
                    left = len + 1;
                }
                else
                {
                    right = len - 1; // Try shorter length
                }
            }

            return lastFound;
        }

        /// <summary>
        /// Given two sets of substring starting indexes check every possible pair of substrings for a string match.
        /// </summary>
        private static bool CheckForMatchingSubstringPair(String a, String b, int len, IEnumerable<int> aSubStringStartIndexes, IEnumerable<int> bSubStringStartIndexes)
        {
            foreach (int aSubStringStart in aSubStringStartIndexes)
            {
                foreach (int bSubStringStart in bSubStringStartIndexes)
                {
                    for (int i = 0; i < len; ++i)
                    {
                        if (a[(aSubStringStart + i)] != b[bSubStringStart + i])
                        {
                            break; // Current pair didn't match so try another
                        }
                    }

                    // This pair of substrings matched
                    return true;
                }
            }

            return false; // Didn't find a matching pair of substrings
        }

        private static IDictionary<long, List<int>> GetStartIndexesOfSubstringsByHash(String s, int len)
        {
            var subStrings = new Dictionary<long, List<int>>();

            long rollingHash = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                // Add char from right
                var charToAdd = s[i];
                rollingHash = (rollingHash + charToAdd) % HASH_MOD; // This is not a good rolling hash function

                if (i >= len)
                {
                    // Remove char from left
                    var charToRemove = s[i - len];
                    rollingHash = (rollingHash - charToRemove) % HASH_MOD;  // This is not a good rolling hash function
                    if (rollingHash < 0) { rollingHash += HASH_MOD; } // Make positive
                }

                if (i >= (len - 1))
                {
                    // Add possible substring
                    if (!subStrings.ContainsKey(rollingHash)) { subStrings.Add(rollingHash, new List<int>()); }
                    subStrings[rollingHash].Add(i - (len - 1));
                }
            }

            return subStrings;
        }
    }

    [TestClass]
    public class LongestCommonSubStringFinderTests
    {
        [TestMethod]
        public void WhenContains_ExpectLengthOfCommonSubString()
        {
            var cases = new[]
            {
                new { A = "a", B = "b", Expected = 0 },
                new { A = "a", B = "a", Expected = 1 },
                new { A = "abc", B = "def", Expected = 0 },
                new { A = "bc", B = "cd", Expected = 1 },
                new { A = "aba", B = "b", Expected = 1 },
                new { A = "abcd", B = "abcd", Expected = 4 },
                new { A = "the cat in the hat.", B = "eeeecateeee", Expected = 3 },
                new { A = new String(new [] { Char.MaxValue, Char.MinValue, Char.MaxValue } ), B = new String(new [] { Char.MinValue, Char.MaxValue }), Expected = 2 },
                new { A = new String(new [] { Char.MaxValue, Char.MaxValue, Char.MaxValue } ), B = new String(new [] { Char.MaxValue, Char.MaxValue }), Expected = 2 },
            };
            foreach(var testCase in cases)
            {
                var result = LongestCommonSubStringFinder.FindLongestCommonSubstring(testCase.A, testCase.B);
                Assert.AreEqual(testCase.Expected, result, String.Format("When {0} and {1} expected {2}", testCase.A, testCase.B, testCase.Expected));
            }
        }

        [TestMethod]
        public void WhenDoesNotContain_ExpectZero()
        {
            var cases = new[]
            {
                new { A = (String)null, B = (String)null },
                new { A = "", B = "" },
                new { A = "a", B = "b" },
                new { A = "abc", B = "def" },
                new { A = new String(new [] { Char.MaxValue, Char.MaxValue, Char.MaxValue } ), B = new String(new [] { Char.MinValue, Char.MinValue }) },
            };
            foreach(var testCase in cases)
            {
                var result = LongestCommonSubStringFinder.FindLongestCommonSubstring(testCase.A, testCase.B);
                Assert.AreEqual(0, result, String.Format("When {0} and {1} expected {2}", testCase.A, testCase.B, 0));
            }
        }
    }

}