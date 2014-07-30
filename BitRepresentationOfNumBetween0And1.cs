using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

// Display a 32 bit representation of of a number between 0 and 1 as a string. 

namespace BitRepresentationOfNumBetween0And1
{
    public class DoubleToBitsAsString
    {
        const int MAX_BITS = 32;

        public string Convert(double num)
        {
            if (num > 1 || num < 0) { throw new ArgumentOutOfRangeException(); }

            double div = 1D / 2D;
            var sb = new StringBuilder(".");
            int count = 0;

            while (num > 0)
            {
                if (count >= MAX_BITS) { throw new Exception(); } 

                if (num >= div)
                {
                    num -= div;
                    sb.Append(1);
                }
                else
                {
                    sb.Append(0);
                }

                count++;
                div /= 2;
            }

            return sb.ToString();
        }
    }

    [TestClass]
    public class BitRepresentationOfNumBetween0And1
    {
        [TestMethod]
        public void WhenHalf_ExpectOnlyHighestBitSet()
        {
            var converter = new DoubleToBitsAsString();
            Assert.AreEqual(".1", converter.Convert(0.5D));
        }

        [TestMethod]
        public void WhenQuarter_ExpectOnlySecondHighestBitSet()
        {
            var converter = new DoubleToBitsAsString();
            Assert.AreEqual(".01", converter.Convert(0.25D));
        }

        [TestMethod]
        public void WhenMultipleBitsSet_ExpectCorrectConversion()
        {
            var converter = new DoubleToBitsAsString();
            Assert.AreEqual(".011", converter.Convert(0.375D));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestMethod_SmallestQuantum()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(double.Epsilon);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestMethod_Max()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(double.MaxValue);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestMethod_LargestNegative()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(double.MinValue);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestMethod_PostiveInfinity()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(double.PositiveInfinity);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestMethod_NegativeInfinity()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(double.NegativeInfinity);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WhenGreaterThanOne_ExpectRangeException()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(1.1D);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WhenNegative_ExpectRangeException()
        {
            var converter = new DoubleToBitsAsString();
            converter.Convert(-0.1D);
        }
    }
}
