using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

// Find the largest and smallest numbers that have the same number of bits
// as the input number.

namespace LargestAndSmallestNumberWithSameNumberOfSetBits
{
    public static class FindLargestAndSmallest
    {
        public static int Smallest(int num)
        {
            if (num < 0) { throw new ArgumentOutOfRangeException(); }
            if (num == 0) { return 0; }

            int bits = CountSetBits(num);

            // Construct new number consuming bits from LSB to MSB.
            int mask = 1;
            int result = 0;
            while (bits > 0)
            {
                result |= mask;
                mask <<= 1;
                bits--;
            }

            return result;
        }

        public static int Largest(int num)
        {
            if (num < 0) { throw new ArgumentOutOfRangeException(); }
            if (num == 0) { return 0; }

            int bits = CountSetBits(num);

            // Construct new number consuming bits from MSB to LSB.
            int mask = unchecked((1 << 31) - 1); // Remember MSB is negative sign
            Debug.Assert(mask == Math.Pow(2, 31) - 1, "Expected mask to be 2^31 - 1.");

            int result = 0;
            while (bits > 0)
            {
                result |= mask;
                mask >>= 1;
                bits--;
            }

            return result;
        }
       
        /// <summary>
        /// Find number of set bits using Brian Kernighan method
        /// </summary>
        private static int CountSetBits(int num)
        {
            int bits;
            for (bits = 0; num > 0; bits++)
            {
                num &= num - 1; 
            }
            return bits;
        }
    }

    [TestClass]
    public class FindLargestAndSmallestTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Smallest_WhenNegative_ExpectException()
        {
            FindLargestAndSmallest.Smallest(-1);
        }

        [TestMethod]
        public void Smallest_WhenZero_ExpectZero()
        {
            Assert.AreEqual(0, FindLargestAndSmallest.Smallest(0));
        }

        [TestMethod]
        public void Smallest_WhenOneBit_ExpectOne()
        {
            Assert.AreEqual(1, FindLargestAndSmallest.Smallest(2));
        }

        [TestMethod]
        public void Smallest_WhenTwoBits_ExpectThree()
        {
            Assert.AreEqual(3, FindLargestAndSmallest.Smallest(12));
        }

        [TestMethod]
        public void Smallest_WhenMaxIntBits_ExpectMaxIntBits()
        {
            // Remember that first bit is negative sign so: 
            // Negative: 2^31 values
            // Zero: 1 value
            // Positive: 2^31 - 1 values
            Assert.AreEqual(int.MaxValue, FindLargestAndSmallest.Smallest(int.MaxValue));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Largest_WhenNegative_ExpectException()
        {
            FindLargestAndSmallest.Largest(-1);
        }

        [TestMethod]
        public void Largest_WhenZero_ExpectZero()
        {
            Assert.AreEqual(0, FindLargestAndSmallest.Largest(0));
        }

        [TestMethod]
        public void Largest_WhenOneBit_ExpectOne()
        {
            var expected = unchecked((1 << 31) - 1);
            Assert.AreEqual(expected, FindLargestAndSmallest.Largest(2));
        }

        [TestMethod]
        public void Largest_WhenTwoBits_ExpectThree()
        {
            var expected = unchecked( ((1 << 31) - 1) | (1 << 30));
            Assert.AreEqual(expected, FindLargestAndSmallest.Largest(12));
        }

        [TestMethod]
        public void Largest_WhenMaxIntBits_ExpectMaxIntBits()
        {
            // Remember that first bit is negative sign so: 
            // Negative: 2^31 values
            // Zero: 1 value
            // Positive: 2^31 - 1 values
            Assert.AreEqual(int.MaxValue, FindLargestAndSmallest.Largest(int.MaxValue));
        }
    }
}
