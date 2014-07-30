using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FindRotatedSubstring 
{
    public static class RotatedSubstring 
    {
        public static bool HasRotatedSubstring(this String target, String pattern)
        {
            if (target == null || pattern == null) { throw new ArgumentNullException(); }
            if (target.Length == 0 || pattern.Length == 0) { return false; }
            if (pattern.Length > target.Length) { return false; }

            var newTarget = target + target;
            return newTarget.Contains(pattern);
        }

        /// <summary>
        /// This is an implementation that doesn't require any string functions.
        /// </summary>
        public static bool HasRotatedSubstring(this char[] target, char[] pattern)
        {
            if (target == null || pattern == null) { throw new ArgumentNullException(); }
            if (target.Length == 0 || pattern.Length == 0) { return false; }
            if (pattern.Length > target.Length) { return false; }

            // Rotate the pattern over the target. 
            int start = 0;
            while (start < target.Length)
            {
                var stop = (start + pattern.Length) % target.Length;

                // Check each char of the pattern against the target.
                var targetIdx = 0;
                var match = true;
                for (var patternIdx = 0; patternIdx < pattern.Length; patternIdx++)
                {
                    targetIdx = (start + patternIdx) % target.Length;
                    if (target[targetIdx] != pattern[patternIdx])
                        match = false;
                }

                if (match)
                    return true;
                start++;
            }

            // Didn't find any matches
            return false;
        }
    }

    [TestClass]
    public class RotatedSubstringTests
    {
        #region String 

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void String_WhenTargetNull_ExpectException()
        {
            Assert.IsFalse(RotatedSubstring.HasRotatedSubstring(null, "ab"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void String_WhenPatternNull_ExpectException()
        {
            Assert.IsFalse(RotatedSubstring.HasRotatedSubstring("ab", null));
        }

        [TestMethod]
        public void String_WhenNoSubstring_ExpectFalse()
        {
            Assert.IsFalse("abcd".HasRotatedSubstring(""));
        }

        [TestMethod]
        public void String_WhenNoTarget_ExpectFalse()
        {
            Assert.IsFalse("".HasRotatedSubstring("ab"));
        }

        [TestMethod]
        public void String_WhenNotSubstring_ExpectFalse()
        {
            Assert.IsFalse("abcd".HasRotatedSubstring("ef"));
        }

        [TestMethod]
        public void String_StartsWithSubstring_ExpectTrue()
        {
            Assert.IsTrue("abcd".HasRotatedSubstring("ab"));
        }

        [TestMethod]
        public void String_EndsWithSubstring_ExpectTrue()
        {
            Assert.IsTrue("abcd".HasRotatedSubstring("cd"));
        }

        [TestMethod]
        public void String_SubstringInMiddle_ExpectTrue()
        {
            Assert.IsTrue("abcd".HasRotatedSubstring("bc"));
        }

        [TestMethod]
        public void String_SubstringWrapsAround_ExpectTrue()
        {
            Assert.IsTrue("abcd".HasRotatedSubstring("da"));
        }

        #endregion

        #region CHAR[]

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Char_WhenTargetNull_ExpectException()
        {
            Assert.IsFalse(RotatedSubstring.HasRotatedSubstring(null, "ab".ToCharArray()));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Char_WhenPatternNull_ExpectException()
        {
            Assert.IsFalse(RotatedSubstring.HasRotatedSubstring("ab".ToCharArray(), null));
        }

        [TestMethod]
        public void Char_WhenNoSubstring_ExpectFalse()
        {
            Assert.IsFalse("abcd".ToCharArray().HasRotatedSubstring("".ToCharArray()));
        }

        [TestMethod]
        public void Char_WhenNoTarget_ExpectFalse()
        {
            Assert.IsFalse("".ToCharArray().HasRotatedSubstring("ab".ToCharArray()));
        }

        [TestMethod]
        public void Char_WhenNotSubstring_ExpectFalse()
        {
            Assert.IsFalse("abcd".ToCharArray().HasRotatedSubstring("ef".ToCharArray()));
        }

        [TestMethod]
        public void Char_StartsWithSubstring_ExpectTrue()
        {
            Assert.IsTrue("abcd".ToCharArray().HasRotatedSubstring("ab".ToCharArray()));
        }

        [TestMethod]
        public void Char_EndsWithSubstring_ExpectTrue()
        {
            Assert.IsTrue("abcd".ToCharArray().HasRotatedSubstring("cd".ToCharArray()));
        }

        [TestMethod]
        public void Char_SubstringInMiddle_ExpectTrue()
        {
            Assert.IsTrue("abcd".ToCharArray().HasRotatedSubstring("bc".ToCharArray()));
        }

        [TestMethod]
        public void Char_SubstringWrapsAround_ExpectTrue()
        {
            Assert.IsTrue("abcd".ToCharArray().HasRotatedSubstring("da".ToCharArray()));
        }

        #endregion
    }
}
