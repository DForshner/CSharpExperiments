using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// Find the In-Order successor node of a given node in a BST.
// Nodes have parent pointers and there are no duplicates values.
//
// Compiled: Visual Studio 2013

namespace FindBSTInOrderSuccessorNode
{
    public class Node<T> where T : IComparable<T> 
    {
        public Node<T> Parent { get; set; }
        public Node<T> Left { get; set; }
        public Node<T> Right { get; set; }
        public T Value { get; private set; } 

        public Node(T value)
        {
            this.Value = value;
        }

        public void Insert(Node<T> nodeToInsert)
        {
            var cmp = nodeToInsert.Value.CompareTo(Value);
            if (cmp < 0) // New node is less than current
                InsertLeft(nodeToInsert);
            else if (cmp > 0) // New node is greater
                InsertRight(nodeToInsert);
            else
                throw new NotSupportedException("Duplicate values not supported.");
        }

        private void InsertRight(Node<T> nodeToInsert)
        {
            if (Right != null) // Common case first.
            {
                Right.Insert(nodeToInsert);
                return;
            }

            nodeToInsert.Parent = this;
            Right = nodeToInsert;
        }

        private void InsertLeft(Node<T> nodeToInsert)
        {
            if (Left != null) // Common case first.
            {
                Left.Insert(nodeToInsert);
                return;
            }

            nodeToInsert.Parent = this;
            Left = nodeToInsert;
        }

        public Node<T> FindInOrderSuccessor()
        {
            if (this.Right != null) // Has a right child.
                return Right.LeftMostChild();
            else if (this.Parent == null) // This is the root node. 
                return Right.LeftMostChild();

            // Scan upwards until the current node is not the parent node's left.
            var curr = this;
            var next = this.Parent;
            while (next != null && next.Left != curr)
            {
                curr = next;
                next = next.Parent;
            }
            return next;
        }

        public Node<T> LeftMostChild()
        {
            var curr = this;
            while (curr.Left != null)
                curr = curr.Left;
            return curr;
        }
    }

    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void LeftMostChild_WhenLeftLeaningTree_ExpectSmallestValueReturned()
        {
            var root = new Node<int>(5);
            root.Insert(new Node<int>(4));
            root.Insert(new Node<int>(2));
            root.Insert(new Node<int>(1));
            var result = root.LeftMostChild();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Value);
        }

        [TestMethod]
        public void FindInOrderSuccessor_WhenRoot_ExpectRightSubTreeLeftMostChildReturned()
        {
            var root = new Node<int>(5);
            root.Insert(new Node<int>(3));
            root.Insert(new Node<int>(7));
            Assert.AreEqual(7, root.FindInOrderSuccessor().Value);
        }

        [TestMethod]
        public void FindInOrderSuccessor_WhenLeftOfParent_ExpectParentIsSuccessor()
        {
            var root = new Node<int>(5);
            var left = new Node<int>(3);
            root.Insert(left);
            root.Insert(new Node<int>(7));
            Assert.AreEqual(5, left.FindInOrderSuccessor().Value);
        }

        [TestMethod]
        public void FindInOrderSuccessor_WhenLeftOfRoot_ExpectNoSuccessor()
        {
            var root = new Node<int>(5);
            root.Insert(new Node<int>(3));
            var left = new Node<int>(7);
            root.Insert(left);
            Assert.AreEqual(null, left.FindInOrderSuccessor());
        }

        [TestMethod]
        public void FindInOrderSuccessor_WhenRightChildHasNoChildren_ExpectParentsParentIsSuccessor()
        {
            var root = new Node<int>(5);
            root.Insert(new Node<int>(3));
            root.Insert(new Node<int>(2));
            var left = new Node<int>(4);
            root.Insert(left);
            Assert.AreEqual(5, left.FindInOrderSuccessor().Value);
        }
    }
}