using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Determine if a txt contains an anagram of a pattern as a substring.
// Solution slides a window with substring length over the text maintaining a
// rolling hash to look for possible matches and then checks for a character frequency match.
// Time complexity: I think this will be similar to Rabin-Karp: Average Case O(n+m), Worst Case: O(nm)

namespace AnagramSubStringMatching
{
    public static class AnagramSubStringFinder
    {
        private const long HASH_MOD = long.MaxValue / 2; // Modulo to prevent rollover

        public static bool ContainsAnagram(String txt, String pattern)
        {
            if (txt == null && pattern == null) { return true; }
            if (txt == null || pattern == null) { return false; }
            if (pattern.Length == 0 && txt.Length == 0) { return true; }
            if (pattern.Length > txt.Length) { return false; }

            // Build a character frequency map of the anagram.
            var anagramFreq = new Dictionary<char, int>();
            long anagramHash = 0;
            foreach(var c in pattern)
            {
                if (!anagramFreq.ContainsKey(c)) { anagramFreq.Add(c, 0); }
                anagramFreq[c] += 1;
                anagramHash = (anagramHash + c) % HASH_MOD; // This is is not a good hash function.
            }

            // Slide our window of character frequencies over txt from left to right.
            // At each possible starting point check for that the rolling hash matches
            // and that the character frequencies match.
            var windowFreq = new Dictionary<char, int>();
            long rollingHash = 0;
            for (int i = 0; i < txt.Length; ++i)
            {
                // Add char from right
                var charToAdd = txt[i];

                if (!windowFreq.ContainsKey(charToAdd)) { windowFreq.Add(charToAdd, 0); }
                windowFreq[charToAdd] += 1;

                rollingHash = (rollingHash + charToAdd) % HASH_MOD; // This is is not a good hash function.

                // Remove leftmost(oldest) char if not still building the leftmost (first) window.
                if (i >= pattern.Length)
                {
                    var charToRemove = txt[i - pattern.Length];

                    windowFreq[charToRemove] -= 1;
                    if (windowFreq[charToRemove] == 0) { windowFreq.Remove(charToRemove); }

                    rollingHash = (rollingHash - charToRemove) % HASH_MOD;  // This is is not a good hash function.
                    if (rollingHash < 0) { rollingHash += HASH_MOD; } // Rotate to a positive value.
                }

                if (rollingHash == anagramHash // Additive hash matches so we have a potential match
                    && AreCharacterFrequenciesSame(anagramFreq, windowFreq))
                {
                    return true; // Found substring anagram that matches
                }
            }

            return false; // Didn't find substring anagram that matched
        }

        private static bool AreCharacterFrequenciesSame(IDictionary<char, int> a, IDictionary<char, int> b)
        {
            return a.Keys.All(key => b.ContainsKey(key) && b[key] == a[key]);
        }
    }

    [TestClass]
    public class AnagramSubStringFinderTests
    {
        [TestMethod]
        public void WhenContains_ExpectTrue()
        {
            var cases = new[]
            {
                new { Txt = (String)null, Pattern = (String)null },
                new { Txt = "", Pattern = "" },
                new { Txt = "cat", Pattern = "" },
                new { Txt = "cat", Pattern = "cat" },
                new { Txt = "acat", Pattern = "cat" },
                new { Txt = "the cat in the hat.", Pattern = "cat" },
                new { Txt = "zzz ZZZzzzZzZzzzz", Pattern = "ZzZz" },
                new { Txt = new String(new [] { Char.MaxValue, Char.MinValue, Char.MaxValue } ), Pattern = new String(new [] { Char.MinValue, Char.MaxValue }) },
                new { Txt = new String(new [] { Char.MaxValue, Char.MaxValue, Char.MaxValue } ), Pattern = new String(new [] { Char.MaxValue, Char.MaxValue }) },
            };
            foreach (var testCase in cases)
            {
                Assert.IsTrue(AnagramSubStringFinder.ContainsAnagram(testCase.Txt, testCase.Pattern));
            }
        }

        [TestMethod]
        public void WhenDoesNotContain_ExpectFalse()
        {
            var cases = new[]
                {
                    new { Txt = "", Pattern = "cat" },
                    new { Txt = "the cat in the hat.", Pattern = "dog" }
                };
            foreach (var testCase in cases)
            {
                Assert.IsFalse(AnagramSubStringFinder.ContainsAnagram(testCase.Txt, testCase.Pattern));
            }
        }
    }
}
