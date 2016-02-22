using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

// Give a text and a pattern write a function that returns true if the pattern occurs in the text.
// Time complexity: Average Case O(n+m), Worst Case: O(nm)

namespace SubStringMatchingRabinKarp
{
    public static class SubStringFinder
    {
        // The mod should be large enough to reduce collisions and small enough to prevent the
        // hash from overflowing the long it's stored in.
        // I'm leaving it purposely small to cause collisions.
        private const int PRIME_MOD = 13;

        // The prime base should be sized based on the number of characters
        // in the alphabet.  A char in C# has range 0x0000-0xffff
        private const int ALPHABET_SIZE = 0xFFFF;

        public static bool Contains(string txt, string pattern)
        {
            if (String.IsNullOrEmpty(txt) && String.IsNullOrEmpty(pattern)) { return true; }
            if (String.IsNullOrEmpty(txt) || String.IsNullOrEmpty(pattern)) { return false; }

            // find the first term (base^(n - 1)) value.
            long firstTermPower = 1;
            for (int i = 0; i < (pattern.Length - 1); ++i)
            {
                firstTermPower = (firstTermPower * ALPHABET_SIZE) % PRIME_MOD;
            }

            // find the hash values for the pattern and first window of text
            long patHash = 0;
            long txtHash = 0;
            for (int i = 0; i < pattern.Length; ++i)
            {
                txtHash = (ALPHABET_SIZE * txtHash + txt[i]) % PRIME_MOD;
                patHash = (ALPHABET_SIZE * patHash + pattern[i]) % PRIME_MOD;
            }

            // Slide pattern over text
            for (int i = 0; i <= (txt.Length - pattern.Length); ++i)
            {
                // Check for match
                if (txtHash == patHash)
                {
                    // Confirm this isn't a collision by performing a character by character match.
                    for (int j = 0; j < pattern.Length; ++j)
                    {
                        if (txt[i + j] != pattern[j])
                            break;
                    }
                    return true; // Found match
                }

                // Shift window
                if (i < txt.Length - pattern.Length)
                {
                    // Remove first letter
                    // [0]*base^(n) + [1]*base^(n-1) + ... + [n-1]*base^(1) + [n]
                    // [1]*base^(n-1) + ... + [n-1]*base^(1) + + [n]
                    txtHash = txtHash - (txt[i] * firstTermPower);

                    // Shift everything over by one
                    // [1]*base^(n-1) + ... + [n-1]*base^(1) + [n]
                    // [1]*base^(n) + ... + [n-1]*base^(2) + [n]*base^(1)
                    txtHash = txtHash * ALPHABET_SIZE;

                    // Add last letter
                    // [1]*base^(n) + ... + [n-1]*base^(2) + [n]*base^(1)
                    // [1]*base^(n) + ... + [n-1]*base^(2) + [n]*base^(1) + [n]
                    txtHash = txtHash + txt[i + pattern.Length];

                    // Mod result to prevent overflows
                    txtHash %= PRIME_MOD;

                    if (txtHash < 0) { txtHash += PRIME_MOD; } // Make positive
                }
            }

            return false; // Could not find match
        }
    }

    [TestClass]
    public class SubStringFinderTests
    {
        [TestMethod]
        public void WhenContains_ExpectTrue()
        {
            var cases = new[]
            {
                new { Txt = (String)null, Pattern = (String)null },
                new { Txt = "", Pattern = "" },
                new { Txt = "cat", Pattern = "cat" },
                new { Txt = "the cat in the hat.", Pattern = "cat" },
                new { Txt = "zzz ZZZzzzZzZzzzz", Pattern = "ZzZz" },
                new { Txt = new String(new [] { Char.MaxValue, Char.MinValue, Char.MaxValue } ), Pattern = new String(new [] { Char.MinValue, Char.MaxValue }) },
                new { Txt = new String(new [] { Char.MaxValue, Char.MaxValue, Char.MaxValue } ), Pattern = new String(new [] { Char.MaxValue, Char.MaxValue }) },
            };
            foreach(var testCase in cases)
            {
                Assert.IsTrue(SubStringFinder.Contains(testCase.Txt, testCase.Pattern));
            }
        }

        [TestMethod]
        public void WhenDoesNotContain_ExpectFalse()
        {
            var cases = new[]
            {
                new { Txt = "cat", Pattern = "" },
                new { Txt = "", Pattern = "cat" },
                new { Txt = "the cat in the hat.", Pattern = "dog" }
            };
            foreach(var testCase in cases)
            {
                Assert.IsFalse(SubStringFinder.Contains(testCase.Txt, testCase.Pattern));
            }
        }

        /// <summary>
        /// Throw some random strings at it.
        /// </summary>
        [TestMethod]
        public void WhenRandomString_ExpectRandomSubStringIsFound()
        {
            Enumerable.Range(0, 100).ToList()
                .ForEach((x) => TestRandomString());
        }

        private static void TestRandomString()
        {
            // Make a random string
            const int LENGTH = 100;
            var rnd = new Random();
            var chr = Enumerable.Range(0, LENGTH)
                .Select(x =>
                {
                    while (true)
                    {
                        var c = (Char)rnd.Next(Char.MaxValue);
                        if (Char.IsLetter(c)) return c;
                    }
                })
                .ToArray();
            var text = new String(chr);

            // Make a random length pattern
            var start = rnd.Next(LENGTH - 2); // Substring of at least one
            var end = rnd.Next(text.Length - start);
            var pattern = text.Substring(start, end);

            Assert.IsTrue(SubStringFinder.Contains(text, pattern));
        }
    }

}
