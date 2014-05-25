using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// Determine if a Binary Search Tree (BST) is balanced.
//
// Compiled: C# - Visual Studio 2013

namespace IsBSTBalanced 
{
    public class Node<T> where T : IComparable<T>
    {
        public Node<T> Left { get; set; }
        public Node<T> Right { get; set; }
        public T Value { get; private set ; }

        public Node(T value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// Partial BST implementation with no balancing logic.
    /// </summary>
    public class BSTree<T> where T : IComparable<T>
    {
        public Node<T> Root { get; private set; }

        /// <summary>
        /// Checks if a Binary Search Tree (BST) is balanced.  A balanced tree is one in which
        /// the heights of the two subtrees of any node never differ by more than one.
        /// </summary>
        public bool IsBalanced
        {
            get { return (GetHeight(Root) != -1); }
        }

        /// <summary>
        /// Recursively check height
        /// O(n) time and O(log N) space.
        /// </summary>
        public int GetHeight(Node<T> root)
        {
            if (root == null)
                return 0;

            // Check left
            int leftSubtreeHeight = GetHeight(root.Left);
            if (leftSubtreeHeight == -1)
                return -1; // Left subtree is unbalanced

            // Check right
            int rightSubTreeHeight = GetHeight(root.Right);
            if (rightSubTreeHeight == -1)
                return -1; // Right subtree is unbalanced

            // Check current
            if (Math.Abs(leftSubtreeHeight - rightSubTreeHeight) > 1)
                return -1; // Unbalanced
            else
                return Math.Max(leftSubtreeHeight, rightSubTreeHeight) + 1; // left + right + current
        }

        /// <summary>
        /// Iterative insert
        /// </summary>
        public void Insert(T value)
        {
            var newNode = new Node<T>(value);
            var curr = Root;
            if (curr == null)
            {
                Root = newNode;
                return;
            }

            while(true)
            {
                if (newNode.Value.CompareTo(curr.Value) < 0)
                {
                    if (curr.Left != null) // Common case first
                    {
                        curr = curr.Left; // Search left
                    }
                    else
                    {
                        curr.Left = newNode; // Insert left
                        return;
                    }
                }
                else if (newNode.Value.CompareTo(curr.Value) > 0)
                {
                    if (curr.Right != null)
                    {
                        curr = curr.Right;
                    }
                    else
                    {
                        curr.Right = newNode;
                        return;
                    }
                }
                else
                {
                    throw new NotSupportedException("Duplicates not supported.");
                }
            }
        }
    }

    [TestClass]
    public class BSTreeTests
    {
        [TestMethod]
        public void IsBalanced_WhenEmpty_ExpectBalanced()
        {
            var sut = new BSTree<int>();
            Assert.IsTrue(sut.IsBalanced);
        }

        [TestMethod]
        public void IsBalanced_WhenLeftHeightGreaterByOne_ExpectBalanced()
        {
            var sut = new BSTree<int>();
            sut.Insert(10);
            sut.Insert(5);
            sut.Insert(3);
            sut.Insert(15);
            Assert.IsTrue(sut.IsBalanced);
        }

        [TestMethod]
        public void IsBalanced_WhenRightHeightGreaterByOne_ExpectBalanced()
        {
            var sut = new BSTree<int>();
            sut.Insert(10);
            sut.Insert(5);
            sut.Insert(15);
            sut.Insert(13);
            Assert.IsTrue(sut.IsBalanced);
        }

        [TestMethod]
        public void IsBalanced_WhenLeftHeightGreaterByTwo_ExpectUnbalanced()
        {
            var sut = new BSTree<int>();
            sut.Insert(10);
            sut.Insert(5);
            sut.Insert(3);
            sut.Insert(2);
            sut.Insert(15);
            Assert.IsFalse(sut.IsBalanced);
        }

        [TestMethod]
        public void IsBalanced_WhenRightHeightGreaterByTwo_ExpectUnbalanced()
        {
            var sut = new BSTree<int>();
            sut.Insert(10);
            sut.Insert(5);
            sut.Insert(15);
            sut.Insert(17);
            sut.Insert(18);
            Assert.IsFalse(sut.IsBalanced);
        }
    }

    //public static class TestProgram
    //{
    //    public static void Main()
    //    {
    //        var tree = new BTree<int>();
    //        tree.Insert(10);
    //        tree.Insert(5);
    //        Console.WriteLine(tree.IsBalanced());

    //        tree.Insert(3);
    //        Console.WriteLine(tree.IsBalanced());

    //        Console.WriteLine("Press [Enter Key] to exit.");
    //        Console.ReadLine();
    //    }
    //}
}