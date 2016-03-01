using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// An interval tree stores a set of intervals and can be used to find all intervals that overlap with a query interval.
// In the augmented variant, on each node we maintain the maximum end value of a node and all of its children which we use during searches
// to prune subtrees that cannot not contain the query interval.
// Query Operation Complexity: O(n) worst case and I want to guess O(log(n + m)) average where m is the number of intersections but I'm not sure.
// Insert Operation Complexity : O(nlog(n))

namespace AugmentedIntervalTree
{
    [DebuggerDisplay("[{Start}, {End}]")]
    public class Interval : IComparable<Interval>, IEqualityComparer<Interval>
    {
        public readonly long Start;
        public readonly long End;

        public Interval(long start, long end)
        {
            Start = start;
            End = end;
        }

        public bool Equals(Interval x, Interval y)
        {
            if (x == y)
                return true; // Reference equality
            else
                return (x.End == y.End && x.Start == y.Start); // Value equality
        }

        public bool Equals(Interval other)
        {
            return Equals(this, other);
        }

        public int GetHashCode(Interval obj)
        {
            unchecked // Just wrap overflow
            {
                int h = 17;
                h = h * 23 + obj.Start.GetHashCode();
                h = h * 23 + obj.End.GetHashCode();
                return h;
            }
        }

        public int CompareTo(Interval other)
        {
            if (this.Start < other.Start)
                return -1;
            else if (this.Start == other.Start)
                return (this.End <= other.End) ? -1 : 1; // Compare ends
            else // (this.start > other.start
                return 1;
        }

        public bool Intersects(Interval other)
        {
            if (other == null) { return false; }

            // Current:          [-------------]
            // Not:  [-------]
            // Not:                               [-------]
            return !(other.End < this.Start) && !(other.Start > this.End);
        }
    }

    [TestClass]
    public class IntervalTests
    {
        [TestMethod]
        public void Before()
        {
            var left = new Interval(0, 1);
            var right = new Interval(2, 3);
            Assert.AreEqual(-1, left.CompareTo(right));
            Assert.AreEqual(1, right.CompareTo(left));
        }

        [TestMethod]
        public void Equals()
        {
            var x = new Interval(0, 1);
            var y = x;
            var z = new Interval(0, 1);
            Assert.IsTrue(x.Equals(y)); // Reference equality
            Assert.IsTrue(x.Equals(z)); // Value equality
        }

        [TestMethod]
        public void Intersects()
        {
            // Current:            [------------]
            //                     4            8
            var testCases = new[]
            {
                //   [------------]
                new { Start = 1, End = 3, Expected = false },

                new { Start = 2, End = 4, Expected = true},
                new { Start = 3, End = 5, Expected = true},
                new { Start = 6, End = 7, Expected = true},
                new { Start = 7, End = 9, Expected = true},
                new { Start = 8, End = 9, Expected = true},
                new { Start = 3, End = 9, Expected = true},

                //                               [----------]
                new { Start = 9, End = 10, Expected = false },
            };

            var current = new Interval(4, 8);
            foreach(var testCase in testCases)
            {
                var result = current.Intersects(new Interval(testCase.Start, testCase.End));
                Assert.AreEqual(testCase.Expected, result);
            }
        }
    }

    public class IntervalNode
    {
        public Interval Val { get; private set; }
        public IntervalNode Left { get; set; }
        public IntervalNode Right { get; set; }
        public long Max { get; set; }

        public IntervalNode(Interval i)
        {
            Val = i;
            Max = i.End;
        }
    }

    public class AugmentedIntervalTree
    {
        private IntervalNode _root = null;

        public void Insert(Interval toInsert)
        {
            if (_root == null)
            {
                _root = new IntervalNode(toInsert);
            }
            else
            {
                Insert(_root, toInsert);
            }
        }

        private void Insert(IntervalNode node, Interval toInsert)
        {
            // Keep track of the maximum end of all of this node's children
            if (toInsert.End > node.Max)
                node.Max = toInsert.End;

            if (node.Val.CompareTo(toInsert) <= 0)
            {
                // new Node is greater than or equal to current so insert right.
                if (node.Right == null)
                    node.Right = new IntervalNode(toInsert);
                else
                    Insert(node.Right, toInsert);
            }
            else
            {
                // new Node is less than current so insert left.
                if (node.Left == null)
                    node.Left = new IntervalNode(toInsert);
                else
                    Insert(node.Left, toInsert);
            }
        }

        public IEnumerable<Interval> FindIntersecting(Interval query)
        {
            return FindIntersecting(_root, query);
        }

        private IEnumerable<Interval> FindIntersecting(IntervalNode node, Interval query)
        {
            if (node.Val.Intersects(query))
                yield return node.Val; // Current intersects

            // Check if the max value of the left subtree is greater than or equal to the start of the query.
            if (node.Left != null && node.Left.Max >= query.Start)
                foreach (var leftNode in FindIntersecting(node.Left, query)) { yield return leftNode; } // TODO: just pass a return list via param

			// TODO: This seems rather unbalanced ... should I be keeping track of a min as well and using it to prune
			// away right subtrees?!?!?!?!
            if (node.Right != null)
                foreach (var rightNode in FindIntersecting(node.Right, query)) { yield return rightNode; }
        }

        public IEnumerable<Interval> GetInOrderTraversal()
        {
			// Note: Recursive version of inorder DFS would have been simpler
            var toVisit = new Stack<Tuple<bool, IntervalNode>>();
            toVisit.Push(Tuple.Create(false, _root));
            while (toVisit.Any())
            {
                var curr = toVisit.Pop();
                if (curr.Item1)
                {
                    // Second time we have seen this node so visit it.
                    yield return curr.Item2.Val;

                    if (curr.Item2.Right != null)
                        toVisit.Push(Tuple.Create(false, curr.Item2.Right));
                }
                else
                {
                    // First time we have seen this node.  Put node back on
                    // stack with indication we have seen it and scan left.
                    toVisit.Push(Tuple.Create(true, curr.Item2));
                    if (curr.Item2.Left != null)
                        toVisit.Push(Tuple.Create(false, curr.Item2.Left));
                }
            }
        }
    }

    [TestClass]
    public class AnagramSubStringFinderTests
    {
        [TestMethod]
        public void WhenInsertRoot_ExpectRootOnlyNode()
        {
            var root = new Interval(0, 1);
            var sut = new AugmentedIntervalTree();
            sut.Insert(root);

            var result = sut.GetInOrderTraversal().Single();

            Assert.AreEqual(root, result);
        }

        [TestMethod]
        public void WhenInsertThreeNonOverlapping_ExpectLeftMiddleRightOrder()
        {
            //      [5,10]
            //      /    \
            //  [1,12]    [15,20]
            //             /
            //          [8,16]

            var root = new Interval(5, 10);
            var right = new Interval(15, 20);
            var left = new Interval(1, 12);
            var rightLeft = new Interval(8, 16);

            var sut = new AugmentedIntervalTree();
            sut.Insert(root);
            sut.Insert(right);
            sut.Insert(left);
            sut.Insert(rightLeft);

            var results = sut.GetInOrderTraversal().ToList();

            Assert.AreEqual(left, results[0]);
            Assert.AreEqual(root, results[1]);
            Assert.AreEqual(rightLeft, results[2]);
            Assert.AreEqual(right, results[3]);
        }

        [TestMethod]
        public void WhenBuildFromLeft_ExpectAllIntersectionsFound()
        {
            // 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20
            // [--------]
            //    [--------]
            //       [--------]
            //          [--------]
            //             [--------]
            //                [--------]
            //                   [--------]
            var sut = new AugmentedIntervalTree();
            sut.Insert(new Interval(1, 4)); // No overlap
            sut.Insert(new Interval(2, 5));
            sut.Insert(new Interval(3, 6));
            sut.Insert(new Interval(4, 7));
            sut.Insert(new Interval(5, 8));
            sut.Insert(new Interval(6, 9));
            sut.Insert(new Interval(7, 10)); // No overlap

            //             [--]
            var results = sut.FindIntersecting(new Interval(5, 6));

            Assert.AreEqual(5, results.Count());
            Assert.IsTrue(results.All(x => x.End >= 5));
            Assert.IsTrue(results.All(x => x.Start <= 6));
        }

        [TestMethod]
        public void WhenBuildFromRight_ExpectAllIntersectionsFound()
        {
            // 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20
            //                   [--------]
            //                [--------]
            //             [--------]
            //          [--------]
            //       [--------]
            //    [--------]
            // [--------]
            var sut = new AugmentedIntervalTree();
            sut.Insert(new Interval(7, 10)); // No overlap
            sut.Insert(new Interval(6, 9));
            sut.Insert(new Interval(5, 8));
            sut.Insert(new Interval(4, 7));
            sut.Insert(new Interval(3, 6));
            sut.Insert(new Interval(2, 5));
            sut.Insert(new Interval(1, 4)); // No overlap

            //             [--]
            var results = sut.FindIntersecting(new Interval(5, 6));

            Assert.AreEqual(5, results.Count());
            Assert.IsTrue(results.All(x => x.End >= 5));
            Assert.IsTrue(results.All(x => x.Start <= 6));
        }

        [TestMethod]
        public void WhenBuildRandom_ExpectAllIntersectionsFound()
        {
            // 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20
            // [--------]
            //             [--------]
            //                   [--------]
            //       [--------]
            //          [--------]
            //    [--------]
            //                [--------]
            var sut = new AugmentedIntervalTree();
            sut.Insert(new Interval(1, 4)); // No overlap
            sut.Insert(new Interval(5, 8));
            sut.Insert(new Interval(7, 10)); // No overlap
            sut.Insert(new Interval(3, 6));
            sut.Insert(new Interval(4, 7));
            sut.Insert(new Interval(2, 5));
            sut.Insert(new Interval(6, 9));

            var temp = sut.GetInOrderTraversal().ToList();

            //             [--]
            var results = sut.FindIntersecting(new Interval(5, 6));

            Assert.AreEqual(5, results.Count());
            Assert.IsTrue(results.All(x => x.End >= 5));
            Assert.IsTrue(results.All(x => x.Start <= 6));
        }
    }
}