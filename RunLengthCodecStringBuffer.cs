using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// Perform run length encoding/decoding on repeated characters in a string in O(n) time.
// TODO:
// - Decoding
// - Support numeric characters

namespace RunLengthCodecStringBuffer
{
    /// <summary>
    /// A run length encoder/decoder implemented using a string builder.
    /// </summary>
    public class RunLengthCodec
    {
        public static String Encode(String str)
        {
            var sb = new StringBuilder();
            sb.Capacity = str.Length; // New string will be at least as long.

            Nullable<char> lastChar = null;
            int count = 0;
            foreach (char c in str)
            {
                Debug.Assert(!Regex.Match(c.ToString(), "[0-9]").Success, "Numbers are not currently supported.");

                // If the current char is the same as the last char.
                if (c == lastChar)
                {
                    count++;
                    continue;
                }

                // If not first char in string or character sequence
                if (lastChar != null)
                {
                    if (count > 0)
                        sb.Append(count);
                    lastChar = null;
                    count = 0;
                }

                sb.Append(c);
                lastChar = c;
            }

            // Check if ended on repeated character
            if (count > 0)
                sb.Append(count);
            
            return sb.ToString();
        }

        public static String Decode(String str)
        {
            var sb = new StringBuilder(); 

            return sb.ToString();
        }
    }

    [TestClass]
    public class RunLengthCodecTests 
    {
        [TestMethod]
        public void Encode_WhenAllCharsUnique_ExpectSameString()
        {
            Assert.AreEqual("Test", RunLengthCodec.Encode("Test"));
        }

        [TestMethod]
        public void Encode_WhenStartWithDuplicates_ExpectEncoded()
        {
            Assert.AreEqual("T4est", RunLengthCodec.Encode("TTTTTest"));
        }

        [TestMethod]
        public void Encode_WhenAllButMiddleHaveDuplicates_ExpectEncoded()
        {
            Assert.AreEqual("T1h2i3s4aT4e3s2t1", RunLengthCodec.Encode("TThhhiiiisssssaTTTTTeeeessstt"));
        }

        [TestMethod]
        public void Encode_WhenEndWithDuplicates_ExpectEncoded()
        {
            Assert.AreEqual("Test3", RunLengthCodec.Encode("Testttt"));
        }
    }
}
