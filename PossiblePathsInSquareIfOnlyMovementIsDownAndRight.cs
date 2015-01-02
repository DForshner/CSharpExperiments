using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// Assume the only movement allowed is moving one unit down or right.
// If we started at the upper left corner of a rectangle how many possible
// paths are there to the lower right corner.

namespace PossiblePathsInRectangleIfOnlyMovementIsDownAndRight
{
    public static class RectanglePathEstimator 
    {
        private const uint yMin = 0;

        public static uint GetPossiblePaths(uint width, uint height)
        {
            if (width == 0) throw new ArgumentOutOfRangeException();
            if (height == 0) throw new ArgumentOutOfRangeException();

            // Translate height/width to x/y starting at upper right corner.
            var xStart = 0U;
            var yStart = height - 1;
            var xMax = width - 1; 

            var visited = new Dictionary<Tuple<uint, uint>, uint>(); 

            // Insert base case lower right tile into results
            visited.Add(Tuple.Create(xMax, yMin), 1U);

            return GetPossiblePaths(xStart, yStart, xMax, visited);
        }

        private static uint GetPossiblePaths(uint x, uint y, uint xMax, IDictionary<Tuple<uint, uint>, uint> visited)
        {
            // Common Case: Check if we have already visited this tile (dynamic programming)
            var tile = Tuple.Create(x, y);
            if (visited.ContainsKey(tile))
            {
                return visited[tile];
            }

            Debug.Assert(!(x == xMax && y == yMin), "Lowest right tile should have been stored in visited.");

            var paths = 0U;
            if (CanMoveRight(x, xMax))
            {
                paths = GetPossiblePaths(x + 1, y, xMax, visited); 
            }

            if (CanMoveDown(y))
            {
                paths += GetPossiblePaths(x, y - 1, xMax, visited);
            }

            // Store the number of paths from this tile.
            visited.Add(tile, paths);

            return paths;
        }

        private static bool CanMoveRight(uint x, uint xMax)
        {
            return (x < xMax);
        }

        private static bool CanMoveDown(uint y)
        {
            return (y > yMin);
        }
    }

    [TestClass]
    public class RectanglePathEstimatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WhenZeroArea_ExpectException()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(0U, 0U);
            Assert.AreEqual(1U, result);
        }

        [TestMethod]
        public void WhenOneUnit_ExpectOnePath()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(1U, 1U);
            Assert.AreEqual(1U, result);
        }

        [TestMethod]
        public void WhenTwoByTwoUnitSquare_ExpectTwoWays()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(2U, 2U);
            Assert.AreEqual(2U, result);
        }

        [TestMethod]
        public void WhenThreeByThreeSquare_ExpectSixWays()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(3U, 3U);
            Assert.AreEqual(6U, result);
        }

        [TestMethod]
        public void WhenFourByTwoRect_ExpectFourWays()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(4U, 2U);
            Assert.AreEqual(4U, result);
        }

        [TestMethod]
        public void WhenThousandByThousand_ExpectNonZeroNumberOfPaths()
        {
            var result = RectanglePathEstimator.GetPossiblePaths(1000U, 1000U);
            Assert.IsTrue(result > 0U, "Expected large non-zero number.");
        }
    }
}