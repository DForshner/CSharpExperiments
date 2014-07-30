using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// Perform run length encoding/decoding on repeated characters in a string in O(n) time.
// TODO:
// - Decoding
// - Support numeric characters

namespace RunLengthCodecArray
{
    /// <summary>
    /// A run length encoder/decoder implemented using char arrays.
    /// </summary>
    public static class RunLengthCodec
    {
        public static char[] Encode(char[] buff)
        {
            var numDuplicates = UnneededChars(buff);

            // If no duplicates just return the original.
            if (numDuplicates == 0)
                return buff;

            var newBuff = new char[buff.Length - numDuplicates];

            int count = 0;
            Nullable<char> lastChar = null;
            int current = 0;
            foreach(var c in buff)
            {
                if (c == lastChar)
                {
                    count++;
                }
                else
                {
                    // Write out any counts from last character
                    if (count > 0)
                    {
                        newBuff[current++] = count.ToString()[0];
                        count = 0;
                    }

                    newBuff[current++] = c;
                    lastChar = c;
                }
            }

            // Write out any remaining counts from last char
            if (count > 0)
            {
                newBuff[current++] = count.ToString()[0];
            }
                
            return newBuff;
        }

        public static int UnneededChars(char[] buff)
        {
            int totalCount = 0;

            int currentCount = 0;
            Nullable<char> lastChar = null;
            foreach(char c in buff)
            {
                if (c == lastChar)
                {
                    currentCount++;
                }
                else
                {
                    lastChar = c;
                    totalCount += (currentCount - NumberOfCharsToDisplayCountDigits(currentCount));
                    currentCount = 0;
                }
            }

            totalCount += (currentCount - NumberOfCharsToDisplayCountDigits(currentCount));

            return totalCount;
        }

        public static int NumberOfCharsToDisplayCountDigits(int count)
        {
            var digits = 0;
            var newCount = count;
            while (count >= 1)
            {
                count /= 10;
                digits++;
            }
            return digits;
        }

        public static char[] Decode(char[] str)
        {
            return str;
        }
    }

    [TestClass]
    public class RunLengthCodecTests
    {
        [TestMethod]
        public void CountDuplicate_WhenNoDuplicates_ExpectZeroCounted()
        {
            Assert.AreEqual(0, RunLengthCodec.UnneededChars("Test".ToCharArray()));
        }

        [TestMethod]
        public void CountDuplicate_WhenOneCharDuplicates_ExpectDuplicatesCounted()
        {
            Assert.AreEqual(2, RunLengthCodec.UnneededChars("Testttt".ToCharArray()));
        }

        [TestMethod]
        public void CountDuplicate_WhenManyCharDuplicates_ExpectDuplicatesCounted()
        {
            Assert.AreEqual(1 + 2 + 3, RunLengthCodec.UnneededChars("TThhhiiiisssssa".ToCharArray()));
        }

        [TestMethod]
        public void CountDuplicate_WhenAll_ExpectDuplicatesCounted()
        {
            Assert.AreEqual(3, RunLengthCodec.UnneededChars("aaaaa".ToCharArray()));
        }

        [TestMethod]
        public void NumberOfCharsToDisplayDigits_WhenZero_ExpectZero()
        {
            Assert.AreEqual(0, RunLengthCodec.NumberOfCharsToDisplayCountDigits(0));
        }

        [TestMethod]
        public void NumberOfCharsToDisplayDigits_WhenOne_ExpectOne()
        {
            Assert.AreEqual(1, RunLengthCodec.NumberOfCharsToDisplayCountDigits(1));
        }

        [TestMethod]
        public void NumberOfCharsToDisplayDigits_WhenTen_ExpectTwo()
        {
            Assert.AreEqual(2, RunLengthCodec.NumberOfCharsToDisplayCountDigits(10));
        }

        [TestMethod]
        public void Encode_NumberOfCharsToDisplayDigits_WhenOneHundred_ExpectThree()
        {
            Assert.AreEqual(3, RunLengthCodec.NumberOfCharsToDisplayCountDigits(100));
        }

        [TestMethod]
        public void Encode_WhenAllCharsUnique_ExpectSameString()
        {
            var result = new string(RunLengthCodec.Encode("Test".ToCharArray()));
            Assert.AreEqual("Test", result);
        }

        [TestMethod]
        public void Encode_WhenStartWithDuplicates_ExpectEncoded()
        {
            var result = new string(RunLengthCodec.Encode("TTTTTest".ToCharArray()));
            Assert.AreEqual("T4est", result);
        }

        [TestMethod]
        public void Encode_WhenAllButMiddleHaveDuplicates_ExpectEncoded()
        {
            var result = new string(RunLengthCodec.Encode("TThhhiiiisssssaTTTTTeeeessstt".ToCharArray()));
            Assert.AreEqual("T1h2i3s4aT4e3s2t1", result);
        }

        [TestMethod]
        public void Encode_WhenEndWithDuplicates_ExpectEncoded()
        {
            var result = new string(RunLengthCodec.Encode("Testttt".ToCharArray()));
            Assert.AreEqual("Test3", result);
        }
    }
}
