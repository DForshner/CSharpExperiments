using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;

// Given a positive integer, print the next smallest and next largest number that have the
// same number of set bits in their binary representation.

namespace GivenPositiveIntGetNextSmallestAndLargestIntThatHasSameNumberOfSetBits
{
    public static class FixedSetBitsIncrementer
    {
        public static int GetNextLargest(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException();

            int temp = n;

            // Count number of tailing zero bits
            // MSB 0000 ... 1001 1[000] LSB => 3 
            int numberOfTrailingUnsetBits = 0;
            while (((temp & 1) == 0 && (temp != 0)))
            {
                numberOfTrailingUnsetBits++;
                temp >>= 1; // Shift LSB right.
            }

            // Count number of set bits to the right of the trailing zeros
            // MSB 0000 ... 100[1 1]000 LSB => 2 
            int numberOfSetBitsToRightOfTrailingZeros = 0;
            while ((temp & 1) == 1)
            {
                numberOfSetBitsToRightOfTrailingZeros++;
                temp >>= 1; // Shift LSB right.
            }

            // If MSB 1111 ... 0000 LSB there is no larger number with same amount of set bits.
            if (numberOfTrailingUnsetBits + numberOfSetBitsToRightOfTrailingZeros == 31)
            {
                return -1;
            }

            // If MSB 0000 ... 0000 LSB there is no larger number with zero bits set.
            if (numberOfTrailingUnsetBits + numberOfSetBitsToRightOfTrailingZeros == 0)
            {
                return -1;
            }

            // MSB 0000 ... 10[0]1 1000 LSB
            int rightMostNonTailingZeroPos = numberOfTrailingUnsetBits + numberOfSetBitsToRightOfTrailingZeros;

            // MSB 0000 ... 10[1]1 1000 LSB - Set the right most not tailing zero.
            n |= (1 << rightMostNonTailingZeroPos); 

            // MSB 0000 ... 101[0 0000] LSB - Unset the bits to the right of the rightmost not tailing zero.
            n &= ~((1 << rightMostNonTailingZeroPos) - 1); 

            // MSB 0000 ... 1010 000[1] LSB - Insert the set bits that where removed (minus one) working from the left.
            n |= (1 << (numberOfSetBitsToRightOfTrailingZeros - 1)) - 1;

            return n;
        }

        public static int GetNextSmallest(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException();

            int temp = n;

            // Count number of tailing ones 
            // MSB 0000 ... 1001 10[11] LSB => 2 
            int numberOfTrailingOnes = 0;
            while ((temp & 1) == 1)
            {
                numberOfTrailingOnes++;
                temp >>= 1;
            }

            // If MSB 0000 ... 0000 LSB there is no positive smaller number with zero bits set.
            if (temp == 0)
            {
                return -1;
            }

            // Count number of zeros to the left of trailing ones.
            // MSB 0000 ... 1001 1[0]11 LSB => 1
            int sizeOfBlockOfZerosLeftOfTrailingOnes = 0;
            while (((temp & 1) == 0) && (temp != 0))
            {
                sizeOfBlockOfZerosLeftOfTrailingOnes++;
                temp >>= 1;
            }

            // MSB 0000 ... 1001 [1]011 LSB
            int rightMostNonTrailingOne = numberOfTrailingOnes + sizeOfBlockOfZerosLeftOfTrailingOnes;

            // MSB 0000 ... 1001 [1011] LSB - Clear all bits from right non trailing one onwards.
            n &= ((~0) << (rightMostNonTrailingOne + 1));

            // MSB 0000 ... 1001 1[0]11 LSB - Fill the block of zeros to the left of the trailing zeros with ones.
            int mask = (1 << (sizeOfBlockOfZerosLeftOfTrailingOnes + 1)) - 1;
            n |= mask << (numberOfTrailingOnes - 1);

            return n;
        }
    }

    [TestClass]
    public class GivenPositiveIntGetNextSmallestAndLargestIntThatHasSameNumberOfSetBits
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetNextLargest_WhenNegative_ExpectException()
        {
            FixedSetBitsIncrementer.GetNextLargest(-1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetNextSmallest_WhenNegative_ExpectException()
        {
            FixedSetBitsIncrementer.GetNextSmallest(-1);
        }

        [TestMethod]
        public void GetNextLargest_WhenValidInts_ExpectCorrectResults()
        {
            var testCases = new[]
            {
                new 
                { 
                    ToTest = ToInt32(new[] { true, false, true, true }), 
                    Expect = ToInt32(new[] { false, true, true, true }) 
                },
                new 
                { 
                    ToTest = ToInt32(new[] { false, false, false, true, true, false, false, true }),
                    Expect = ToInt32(new[] { true, false, false, false, false, true, false, true }) 

                },
            };

            foreach (var test in testCases)
            {
                var result = FixedSetBitsIncrementer.GetNextLargest(test.ToTest);
                Assert.AreEqual(test.Expect, result);
            }
        }

        [TestMethod]
        public void GetNextSmallest_WhenValidInts_ExpectCorrectResults()
        {
            var testCases = new[]
            {
                new 
                { 
                    ToTest = ToInt32(new[] { true, false, true, true }), // 13
                    Expect = ToInt32(new[] { true, true, false, true })  // 11
                },
                new 
                { 
                    ToTest = ToInt32(new[] { true, true, false, true, true, false, false, true }), // 155
                    Expect = ToInt32(new[] { false, true, true, false, true, false, false, true })  // 150
                },
            };

            foreach (var test in testCases)
            {
                var result = FixedSetBitsIncrementer.GetNextSmallest(test.ToTest);
                Assert.AreEqual(test.Expect, result);
            }
        }

        private static int ToInt32(bool[] bits)
        {
            return ToInt32(new BitArray(bits));
        }

        private static int ToInt32(BitArray bits)
        {
            if (bits.Length > 32) throw new ArgumentOutOfRangeException();
            var array = new int[1];
            bits.CopyTo(array, 0);
            return array[0];
        }
    }
}