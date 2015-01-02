using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

// Assume you can go up 1, 2, or 3 steps at a time.
// How many possible ways are there are to run up n stairs?
// Space Complexity: O(n)
// Compiled with: Visual Studio 2013

namespace PossibleWaysToHopUpStaircase
{
    public static class Estimator 
    {
        public static uint WaysToRunUpNStairs(uint n)
        {
            // Keep a record of stair counts we have already seen (Dynamic Programming)
            var map = new Dictionary<uint, uint>();

            // Insert the base cases (n == 0) and (n == 1) into the map because we check the map first thing.
            map.Add(0, 0); // Base Case: Zero steps zero ways.
            map.Add(1, 1); // Base Case: One step one way.
            map.Add(2, 2); // Base Case: Two steps two ways 2, (1 + 1).
            map.Add(3, 4); // Base Case: Three steps four ways 3, (2 + 1), (1 + 2), (1 + 1 + 1).

            return WaysToRunUpNStairs(n, map);
        }

        private static uint WaysToRunUpNStairs(uint n, IDictionary<uint, uint> map)
        {
            // Check if we have already seen this value before
            if (map.ContainsKey(n))
            {
                return map[n];
            }
            Debug.Assert(n > 3, "n = 0,1,2,3 should have been handled by map[n].");

            var ways = WaysToRunUpNStairs(n - 3, map)
                    + WaysToRunUpNStairs(n - 2, map)
                    + WaysToRunUpNStairs(n - 1, map);
            map[n] = ways;
            return ways;
        }
    }

    [TestClass]
    public class EstimatorTests
    {
        [TestMethod]
        public void WhenZeroSteps_ExpectOneWays()
        {
            var result = Estimator.WaysToRunUpNStairs(0U);
            Assert.AreEqual(0U, result);
        }

        /// <summary>
        /// w(1)
        /// w(0)
        /// </summary>
        [TestMethod]
        public void WhenOneStep_ExpectOneWay()
        {
            var result = Estimator.WaysToRunUpNStairs(1U);
            Assert.AreEqual(1U, result);
        }

        /// <summary>
        /// w(2)
        /// w(1) + w(0)
        /// w(0)
        /// </summary>
        [TestMethod]
        public void WhenTwoSteps_ExpectTwoWays()
        {
            var result = Estimator.WaysToRunUpNStairs(2U);
            Assert.AreEqual(2U, result);
        }

        /// <summary>
        /// w(3)
        /// w(2) + w(1) + w(0) 
        /// w(1) + 1 + 1
        /// </summary>
        [TestMethod]
        public void WhenThreeSteps_ExpectFourWays()
        {
            var result = Estimator.WaysToRunUpNStairs(3U);
            Assert.AreEqual(4U, result);
        }

        /// <summary>
        /// w(5)
        /// w(4) + w(3) + w(2)
        /// w(3) + w(2) + w(1) + 4 + 2
        /// 4 + 2 + 1 + 4 + 2
        /// </summary>
        [TestMethod]
        public void WhenFiveSteps_ExpectThirteenWays()
        {
            var result = Estimator.WaysToRunUpNStairs(5U);
            Assert.AreEqual(13U, result);
        }
    }
}