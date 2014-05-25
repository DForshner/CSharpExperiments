using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

// Does route exist between two nodes in a directed graph?
// Implemented using breadth first search.
//
// Compiled: Visual Studio 2013

namespace DoesPathExistInDirectedGraphBFS
{
    public class Edge<TKey, TVal>
    {
        public Node<TKey, TVal> To { get; private set; }
        public Node<TKey, TVal> From { get; private set; }

        public Edge(Node<TKey, TVal> to, Node<TKey, TVal> from)
        {
            this.To = to;
            this.From = from;
        }
    }
    
    public class Node<TKey, TVal>
    {
        public TKey Key { get; private set; }
        public TVal Value { get; private set; }

        private IList<Edge<TKey, TVal>> edges;
        public IEnumerable<Edge<TKey, TVal>> Edges
        {
            get { return edges; }
        }

        public Node(TKey key, TVal value)
        {
            this.Key = key;
            this.Value = value;
            this.edges = new List<Edge<TKey, TVal>>();
        }

        public void AddConnection(Node<TKey, TVal> to)
        {
            this.edges.Add(new Edge<TKey,TVal>(to, this));
        }
    }

    public class Graph<TKey, TValue> where TKey : IEquatable<TKey>
    {
        // Max number of nodes to visit while searching
        private const int MAX_SEARCH = 1000;

        private IList<Node<TKey, TValue>> nodes;
        public IEnumerable<Node<TKey, TValue>> Nodes 
        {
            get { return nodes; }
        }

        public Graph()
        {
            this.nodes = new List<Node<TKey, TValue>>();
        }

        public void AddNode(TKey key, TValue value)
        {
            AddNode(new Node<TKey, TValue>(key, value));
        }

        public void AddNode(Node<TKey, TValue> nodeToAdd)
        {
            this.nodes.Add(nodeToAdd);
        }

        public Node<TKey, TValue> Search(TKey key)
        {
            // TODO: O(n) linear search - could be improved.
            foreach (var node in nodes)
                if (node.Key.Equals(key))
                    return node;

            // Node wasn't found
            return null;
        }

        public void AddConnection(TKey to, TKey from)
        {
            var toNode = Search(to);
            var fromNode = Search(from);
            if (toNode == null || fromNode == null)
                throw new Exception("Cannot add connection to non-existent node(s).");
            fromNode.AddConnection(toNode);
        }

        public bool DoesPathExist(TKey to, TKey from)
        {
            var toNode = Search(to);
            var fromNode = Search(from);
            if (toNode == null || fromNode == null)
                return false;
            return DoesPathExist(toNode, fromNode);
        }
 
        /// <summary>
        /// Performs a breadth first search to find target node.
        /// Time Complexity: O(Nodes ^ Max Depth)
        /// </summary>
        public bool DoesPathExist(Node<TKey, TValue> to, Node<TKey, TValue> from)
        {
            var visited = new HashSet<TKey>();

            var nodesToVisit = new Queue<Node<TKey, TValue>>();
            nodesToVisit.Enqueue(from);

            while (nodesToVisit.Any() & visited.Count < MAX_SEARCH)
            {
                var current = nodesToVisit.Dequeue();
                visited.Add(current.Key);

                // Check if current node is target
                if (current.Key.Equals(to.Key))
                    return true;

                // Enqueue current node's unvisited neighbors 
                foreach (var edge in current.Edges)
                {
                    var neighbor = edge.To;
                    if (!visited.Contains(neighbor.Key))
                        nodesToVisit.Enqueue(neighbor);
                }
            }

            return false; // Didn't find a matching node.
        }
    }

    [TestClass]
    public class GraphTests 
    {
        [TestMethod]
        public void DoesPathExist_WhenBothNodesDoNotExist_ExpectFalse()
        {
            var sut = new Graph<int,int>();
            Assert.IsFalse(sut.DoesPathExist(1, 2));
        }

        [TestMethod]
        public void DoesPathExist_WhenSingleNodesDoNotExist_ExpectFalse()
        {
            var sut = new Graph<int,string>();
            sut.AddNode(1, "A");
            sut.AddNode(2, "B");
            Assert.IsFalse(sut.DoesPathExist(1, 3));
        }

        [TestMethod]
        public void DoesPathExist_WhenPathToItself_ExpectTrue()
        {
            var sut = new Graph<int,string>();
            sut.AddNode(1, "A");
            Assert.IsTrue(sut.DoesPathExist(1, 1));
        }

        [TestMethod]
        public void DoesPathExist_WhenNoConnection_ExpectFalse()
        {
            var sut = MakeFourNodeGraphWithTwoConnections();
            Assert.IsFalse(sut.DoesPathExist(4, 1));
        }

        [TestMethod]
        public void DoesPathExist_WhenConnection_ExpectTrue()
        {
            var sut = MakeFourNodeGraphWithTwoConnections();
            sut.AddConnection(4, 3);
            Assert.IsTrue(sut.DoesPathExist(4, 1));
        }

        private static Graph<int, string> MakeFourNodeGraphWithTwoConnections()
        {
            var graph = new Graph<int, string>();
            graph.AddNode(1, "A");
            graph.AddNode(2, "B");
            graph.AddNode(3, "C");
            graph.AddNode(4, "D");
            graph.AddConnection(2, 1);
            graph.AddConnection(3, 2);
            return graph;
        }
    }
}