using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestConsoleTest
{
    /// <summary>
    /// Find all sets of triples in an input with a sum less than n.
    /// O(n^3) worst case complexity.
    /// </summary>
    public class FindAllTriplesWithSumLessThanN
    {
        public IEnumerable<Tuple<int,int,int>> FindAllTriplesWithSumLessThan(IEnumerable<int> list, int n)
        {
            var sorted = list.ToList();
            sorted.Sort();

            foreach (var outer in list)
            {
                foreach (var inner in list)
                {
                    var target = n - (inner + outer);

                    // All numbers less than target satisfy the condition.
                    for (int i = 0; i < sorted.Count ; ++i)
                    {
                        if (sorted[i] >= target)
                            break;

                        yield return new Tuple<int, int, int>(inner, outer, sorted[i]);
                    }
                }
            }
        }
    }

    [TestClass]
    public class FindAllTriplesWithSumLessThanNTests
    {
        [TestMethod]
        public void WhenSortedInput_ExpectAllTriplesFound()
        {
            var sut = new FindAllTriplesWithSumLessThanN();
            var results = sut.FindAllTriplesWithSumLessThan(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5);
            Assert.IsTrue(results.All(x => (x.Item1 + x.Item2 + x.Item3) < 5));
            Assert.AreEqual(4, results.Count());
        }

        [TestMethod]
        public void WhenUnsortedInput_ExpectedAllTriplesFound()
        {
            var sut = new FindAllTriplesWithSumLessThanN();
            var results = sut.FindAllTriplesWithSumLessThan(new int[] { 5, 9, 4, 7, 6, 10, 8, 3, 1, 2, }, 8);
            Assert.IsTrue(results.All(x => (x.Item1 + x.Item2 + x.Item3) < 8));
            Assert.AreEqual(35, results.Count());
        }
    }

    //public static class Program
    //{
    //    public static void Main()
    //    {
    //        var sut = new FindAllTriplesWithSumLessThanN();

    //        var results = sut.FindAllTriplesWithSumLessThan(new int[] { 1, 2, 3, 4, 5, 6 }, 5);
    //        DisplayResults(results);
    //        //var results2 = sut.FindAllTriplesWithSumLessThan(new int[] { 5, 9, 4, 7, 6, 10, 8, 3, 1, 2, }, 8);
    //        //DisplayResults(results2);

    //        Console.WriteLine("Press [Enter Key] to exit.");
    //        Console.ReadLine();
    //    }

    //    private static void DisplayResults(IEnumerable<Tuple<int, int, int>> results)
    //    {
    //        foreach (var result in results)
    //            Console.WriteLine(result.ToString() + " = " + (result.Item1 + result.Item2 + result.Item3).ToString());
    //    }
    //}
}
