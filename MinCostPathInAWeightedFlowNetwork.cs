using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Given a flow network with costs associated with each edge find the minimum cost path.

namespace MinCostPathInAWeightedFlowNetwork
{
    public class MinCostAugmentingPathSearcher 
    {
        private MinPriorityQueue _pq = new MinPriorityQueue();

        public Tuple<bool, IList<Step>> FindPath(FlowNetwork network)
        {
            var costToReachSource = new List<int>(Enumerable.Repeat(int.MaxValue, network.NVertices));
            costToReachSource[network.Source] = 0;

            IList<Step> path = new List<Step>(Enumerable.Repeat(new Step(int.MinValue, Step.Directions.FORWARD), network.NVertices));

            var visited = new List<bool>(Enumerable.Repeat(false, network.NVertices));
            visited[network.Source] = true;

            _pq.Enqueue(network.Source, 0);
            while (!_pq.Empty)
            {
                var u = _pq.DequeueMin();

                // If we have reached the sink vertex we are done.
                if (u == network.Sink) { break; }

                for (var v = 0; v < network.NVertices; v++)
                {
                    if (v == u || v == network.Source) { continue; }

                    // forward edge with remaining capacity if cost is better.
                    var forwardEdge = network.Edges[u][v];
                    if (forwardEdge.Capacity > 0 && forwardEdge.Flow <= forwardEdge.Capacity)
                    {
                        var newCost = costToReachSource[u] + forwardEdge.Cost;

                        if (newCost >= 0 && newCost < costToReachSource[v])
                        {
                            // This route to reach v is cheaper than we have seen before.
                            path[v] = new Step(u, Step.Directions.FORWARD);
                            costToReachSource[v] = newCost; 

                            if (visited[v])
                            {
                                _pq.updateKey(v, newCost);
                            }
                            else
                            {
                                _pq.Enqueue(v, newCost);
                                visited[v] = true;
                            }
                        }
                    }

                    // backward edge with flow if cost is better.
                    var backwardEdge = network.Edges[v][u];
                    if (backwardEdge.Flow > 0)
                    {
                        var newCost = costToReachSource[u] - backwardEdge.Cost;

                        if (newCost >= 0 && newCost < costToReachSource[v])
                        {
                            // This route to reach v is cheaper than we have seen before.
                            path[v] = new Step(u, Step.Directions.BACKWARD);
                            costToReachSource[v] = newCost;
                            
                            if(visited[v])
                            {
                                _pq.updateKey(v, newCost);
                            }
                            else
                            {
                                _pq.Enqueue(v, newCost);
                                visited[v] = true;
                            }
                        }
                    }
                }
            }

            // return the path if we reached the sink vertex
            return (costToReachSource[network.Sink] != int.MaxValue) ? Tuple.Create(true, path) : Tuple.Create(false, path);
        }
    }

    [TestClass]
    public class MinCostAugmentingPathSearcherTests
    {
        [TestMethod]
        public void WhenNoPath_ExpectFalseReturned()
        {
            var edges = new[]
            {
                Tuple.Create(0, 1, 10, 20),
                Tuple.Create(2, 3, 20, 30),
            };
            var network = new FlowNetwork(edges, 1, 3);
            var path = new MinCostAugmentingPathSearcher().FindPath(network);

            Assert.IsFalse(path.Item1);
        }

        [TestMethod]
        public void WhenOneValidPath_ExpectPathReturned()
        {
            var edges = new[]
            {
                Tuple.Create(1, 2, 10, 20),
                Tuple.Create(2, 3, 20, 30),
            };
            var network = new FlowNetwork(edges, 1, 3);
            var path = new MinCostAugmentingPathSearcher().FindPath(network);

            Assert.IsTrue(path.Item1);
            Assert.AreEqual(2, path.Item2[3].Previous);
            Assert.AreEqual(1, path.Item2[2].Previous);
            Assert.AreEqual(int.MinValue, path.Item2[1].Previous); // Source
        }

        [TestMethod]
        public void WhenVertexesNotIncludedInPath_ExpectPreviousIntMin()
        {
            var edges = new[]
            {
                Tuple.Create(2, 3, 20, 30),
            };
            var network = new FlowNetwork(edges, 2, 3);
            var path = new MinCostAugmentingPathSearcher().FindPath(network);

            Assert.AreEqual(int.MinValue, path.Item2[1].Previous);
            Assert.AreEqual(int.MinValue, path.Item2[0].Previous);
        }
    }

    #region VALUE OBJECTS

    public struct Edge
    {
        public readonly int Capacity;
        public readonly int Cost;
        public int Flow;

        public Edge(int cap, int cost)
        {
            Capacity = cap;
            Cost = cost;
            Flow = 0;
        }
    }

    /// <summary>
    /// Flow network is implemented as adjacency matrix which works best for dense graphs.
    /// </summary>
    public class FlowNetwork
    {
        public readonly int Source;
        public readonly int Sink;
        public readonly int NVertices;
        public readonly List<List<Edge>> Edges;

        /// <summary>
        /// Create a new cost weighted flow network.
        /// </summary>
        /// <param name="edges">u, v, capacity, cost</param>
        /// <param name="source">source vertex</param>
        /// <param name="sink">sink vertex</param>
        public FlowNetwork(ICollection<Tuple<int, int, int, int>> edges, int source, int sink)
        {
            Source = source;
            Sink = sink;

            NVertices = FindMaxVertexId(edges) + 1;
            Edges = CreateEmptyAdjacencyMatrix(NVertices);

            // Assign edges to adjacency matrix
            foreach (var edge in edges)
            {
                Edges[edge.Item1][edge.Item2] = new Edge(edge.Item3, edge.Item4);
            }
        }

        private static List<List<Edge>> CreateEmptyAdjacencyMatrix(int nVertices)
        {
            var matrix = new List<List<Edge>>(nVertices);
            for (var u = 0; u <= nVertices; u++)
            {
                var row = new List<Edge>(nVertices);
                for (var v = 0; v <= nVertices; v++)
                {
                    row.Add(new Edge(0, 0));
                }
                matrix.Add(row);
            }

            Debug.Assert(matrix.Count == nVertices + 1, "Expected " + nVertices + "x" + nVertices + " matrix.");
            Debug.Assert(matrix.All(x => x.Count == nVertices + 1), "Expected " + nVertices + "x" + nVertices + " matrix.");

            return matrix;
        }

        private static int FindMaxVertexId(ICollection<Tuple<int, int, int, int>> edges)
        {
            var maxVertex = 0;
            foreach (var edge in edges)
            {
                if (edge.Item1 > maxVertex)
                    maxVertex = edge.Item1;
                if (edge.Item2 > maxVertex)
                    maxVertex = edge.Item2;
            }
            return maxVertex;
        }
    }

    public struct Step
    {
        public readonly int Previous;
        public readonly Directions Direction;

        public enum Directions : int
        {
            FORWARD,
            BACKWARD
        }

        public Step(int previous, Directions direction)
        {
            Previous = previous;
            Direction = direction;
        }
    }

    #endregion

    #region IGNORE 

    /// <summary>
    /// Throwaway code for demo purposes.  A real min heap has O(nlog(n)) insert/remove.
    /// </summary>
    public class MinPriorityQueue
    {
        private List<Tuple<int, int>> _elements;
        Comparer<Tuple<int, int>> _keyComparer;

        public MinPriorityQueue()
        {
           _keyComparer = Comparer<Tuple<int, int>>.Create((a, b) => { return (a.Item1 < b.Item1) ? -1 : (a.Item1 > b.Item1) ? 1 : 0; });
            Clear();
        }

        public void Clear() { _elements = new List<Tuple<int,int>>(); }

        public bool Empty { get { return _elements.Count == 0; } } 

        public void Enqueue(int value, int key)
        {
            _elements.Add(Tuple.Create(key, value));
            _elements.Sort(_keyComparer);
        }

        public void updateKey(int value, int newKey)
        {
            var ele = _elements.Find(x => x.Item2 == value);
            _elements.Remove(ele);
            _elements.Add(Tuple.Create(newKey, ele.Item2));
            _elements.Sort(_keyComparer);
        }

        public int DequeueMin()
        {
            if (_elements.Count == 0) { return 0; }
            var ele = _elements[0];
            _elements.RemoveAt(0);
            _elements.Sort(_keyComparer);
            return ele.Item2;
        }
    }

    #endregion
}
