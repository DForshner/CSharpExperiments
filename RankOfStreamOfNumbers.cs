using Microsoft.VisualStudio.TestTools.UnitTesting;

// Determine the rank of a integer compared to all of the integers that
// have been streamed thus far.

namespace RankOfStreamOfNumbers
{
    public class Node
    {
        public int Value { get; set; }

        Node Left { get; set; }
        Node Right { get; set; }
        int LeftSize { get; set; }

        public void Insert(Node node)
        {
            if (node.Value < this.Value)
            {
                if (Left == null)
                {
                    Left = node;
                    return;
                }

                Left.Insert(node);
                LeftSize++;
            }
            else
            {
                if (Right == null)
                {
                    Right = node;
                    return;
                }
                    
                Right.Insert(node);
            }
        }

        public int Rank(int valueToRank)
        {
            if (valueToRank == this.Value)
            {
                return LeftSize;
            }
            else if (valueToRank < this.Value)
            {
                if (Left == null) { return -1; }
                return Left.Rank(valueToRank);
            }
            else
            {
                int RightRank = (Right == null) ? -1 : Right.Rank(valueToRank);
                if (RightRank == -1) 
                    return -1; // Didn't find element

                return LeftSize + 1 + RightRank; 
            }
        }
    }

    public class Tree
    {
        Node root { get; set; }

        public void Insert(int valueToInsert)
        {
            var node = new Node() { Value = valueToInsert };

            if (root == null)
            {
                root = node;
                return;
            }

            root.Insert(node);
        }

        public int Rank(int valueToRank)
        {
            if (root == null)
                return -1;

            return root.Rank(valueToRank);
        }
    }

    [TestClass]
    public class UnitTests 
    {
        const int NO_RANK = -1;

        [TestMethod]
        public void Rank_WhenEmptyTree_ExpectNoRank()
        {
            var tree = new Tree();
            Assert.AreEqual(NO_RANK, tree.Rank(7));
        }

        [TestMethod]
        public void Rank_WhenOnlyLesserNodes_ExpectNoRank()
        {
            var tree = new Tree();
            tree.Insert(5);
            tree.Insert(4);
            tree.Insert(6);
            Assert.AreEqual(NO_RANK, tree.Rank(7));
        }

        [TestMethod]
        public void Rank_WhenOnlyGreaterNodes_ExpectNoRank()
        {
            var tree = new Tree();
            tree.Insert(5);
            tree.Insert(4);
            tree.Insert(6);
            Assert.AreEqual(NO_RANK, tree.Rank(3));
        }

        [TestMethod]
        public void Rank_WhenLowestNode_ExpectBottomRank()
        {
            var tree = new Tree();
            tree.Insert(5);
            tree.Insert(4);
            tree.Insert(6);
            Assert.AreEqual(0, tree.Rank(5));
        }

        [TestMethod]
        public void Rank_WhenGreatestNode_ExpectTopRank()
        {
            var tree = new Tree();
            tree.Insert(5);
            tree.Insert(4);
            tree.Insert(6);
            Assert.AreEqual(1, tree.Rank(6));
        }

        [TestMethod]
        public void Rank_WhenRightLeafNode_ExpectCorrect()
        {
            var tree = new Tree();
            tree.Insert(10);
            tree.Insert(5);
            tree.Insert(15);
            tree.Insert(7);
            tree.Insert(13);
            Assert.AreEqual(1, tree.Rank(7));
        }
    }
}
