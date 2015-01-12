using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Find the maximal flow for a given flow network using the Ford-Fulkerson augmenting path search method.
//
// As flows are integers this algorithm is guaranteed to complete with O(Ef) complexity.  Where E is
// the number of edges and f is the maximum flow.  In the worst case we would visit every edge (E) and
// increment its flow by one f times.

namespace MaxFlowInFlowNetworkFordFulkersonPathSearch
{
    #region MAX FLOW CALCULATOR

    public interface ISearchMethod
    {
        IEnumerable<IList<Step>> FindAugmentingPath(FlowNetwork network);
    }

    public class MaxFlowCalculator
    {
        private ISearchMethod _searchMethod;

        /// <summary>
        /// Updates the flow network's nodes with the maximal flow.
        /// Returns false if no valid path could be found from source to sink.
        /// </summary>
        public bool UpdateMaxFlow(FlowNetwork networkToUpdate)
        {
            var atLeastOnePathFound = false;
            foreach (var path in _searchMethod.FindAugmentingPath(networkToUpdate))
            {
                atLeastOnePathFound = true;

                var minFlowOnPath = FindMinFlowOnPath(path, networkToUpdate);

                AugmentFlowsOnPath(path, minFlowOnPath, networkToUpdate);
            }

            return atLeastOnePathFound;
        }

        /// <summary>
        /// Search backwards along path to find the edge that can accept the 
        /// least amount (limiting case) of increased flow on the current path.
        /// </summary>
        protected static int FindMinFlowOnPath(IList<Step> path, FlowNetwork network)
        {
            int minFlowOnPath = int.MaxValue;

            int v = network.SinkIndex;
            while (v != network.SourceIndex)
            {
                int u = path[v].Previous;

                int available;
                if (path[v].Forward)
                {
                    // Forward edges can be adjusted by the remaining capacity.
                    var edge = network.FindEdge(u, v);
                    available = edge.Capacity - edge.Flow;
                }
                else
                {
                    // Backwards edges can only be reduced by their existing flow.
                    var edge = network.FindEdge(v, u);
                    available = edge.Flow;
                }

                // Is this edge's flow the minimum we have seen?
                minFlowOnPath = (available < minFlowOnPath) ? available : minFlowOnPath;

                // Follow reverse path to source.
                v = u;
            }

            return minFlowOnPath;
        }

        /// <summary>
        /// Augment the flows along the path with the lowest amount of flow found earlier. 
        /// </summary>
        protected static void AugmentFlowsOnPath(IList<Step> path, int minFlowOnPath, FlowNetwork networkToUpdate)
        {
            int v = networkToUpdate.SinkIndex;
            while (v != networkToUpdate.SourceIndex)
            {
                int u = path[v].Previous;
                if (path[v].Forward)
                {
                    networkToUpdate.AddFlow(u, v, minFlowOnPath);
                }
                else
                {
                    networkToUpdate.AddFlow(v, u, -minFlowOnPath);
                }

                // Follow reverse path to source.
                v = u;
            }
        }
    }

    [TestClass]
    public class MaxFlowCalculatorTests
    {
        private class TestCalc : MaxFlowCalculator
        {
            public static int TestFindMinFlowOnPath(IList<Step> path, FlowNetwork network)
            {
                return FindMinFlowOnPath(path, network);
            }

            public static void TestAugmentFlowsOnPath(IList<Step> path, int minFlowOnPath, FlowNetwork networkToUpdate)
            {
                AugmentFlowsOnPath(path, minFlowOnPath, networkToUpdate);
            }
        }

        [TestMethod]
        public void WhenPathHasVertexWithFiveAndTwoAvailable_ExpectMinFlowIsTwo()
        {
            var edges = new[] 
            {
                Tuple.Create(0, 1, 10),
                Tuple.Create(1, 2, 10),
            };
            var network = new FlowNetwork(edges, 0, 2);
            network.AddFlow(0, 1, 5); // Available: 5 
            network.AddFlow(1, 2, 8); // Available: 2

            var path = new[]
            {
                new Step { Forward = true, Previous = -1 }, // [0]
                new Step { Forward = true, Previous = 0 }, // [1]
                new Step { Forward = true, Previous = 1 }, // [2]
            };
           
            var result = TestCalc.TestFindMinFlowOnPath(path, network);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void WhenBackwardsStepHasLowestFlow_ExpectBackwardsFlowIsMinFlow()
        {
            var edges = new[] 
            {
                Tuple.Create(0, 1, 4),
                Tuple.Create(1, 2, 4),
                Tuple.Create(2, 4, 4),
                Tuple.Create(3, 2, 4),
                Tuple.Create(3, 4, 4),
            };
            var network = new FlowNetwork(edges, 0, 4);
            network.AddFlow(1, 2, 1);
            network.AddFlow(3, 2, 1);
            network.AddFlow(3, 4, 2);

            var path = new[]
            {
                new Step { Forward = true, Previous = -1 }, // 0->X 
                new Step { Forward = true, Previous = 0 }, // 1->0
                new Step { Forward = true, Previous = 1 }, // 2->1 Available: 4 - 1 = 3 
                new Step { Forward = false, Previous = 2 }, // 3->2 Available: 1 
                new Step { Forward = true, Previous = 3 }, // 4->3 Available: 4 - 2 = 1 
            };

            var result = TestCalc.TestFindMinFlowOnPath(path, network);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void WhenAugmentPathByTwo_ExpecForwardEdgesHaveFlowIncreased()
        {
            var edges = new[] 
            {
                Tuple.Create(0, 1, 10),
                Tuple.Create(1, 2, 10),
            };
            var network = new FlowNetwork(edges, 0, 2);
            var path = new[]
            {
                new Step { Forward = true, Previous = -1 }, // 0->X
                new Step { Forward = true, Previous = 0 }, // 1->0
                new Step { Forward = true, Previous = 1 }, // 2->3
            };
           
            TestCalc.TestAugmentFlowsOnPath(path, 3, network);

            Assert.AreEqual(3, network.FindEdge(0, 1).Flow);
            Assert.AreEqual(3, network.FindEdge(1, 2).Flow);
        }

        [TestMethod]
        public void WhenAugmentPathByTwo_ExpecBackwardsEdgesHaveFlowDecreased()
        {
            var edges = new[] 
            {
                Tuple.Create(1, 0, 10),
                Tuple.Create(2, 1, 10),
            };
            var network = new FlowNetwork(edges, 0, 2);
            network.AddFlow(1, 0, 5);
            network.AddFlow(2, 1, 5);
            var path = new[]
            {
                new Step { Forward = true, Previous = -1 }, // 0->X
                new Step { Forward = false, Previous = 0 }, // 1->0
                new Step { Forward = false, Previous = 1 }, // 2->1
            };
           
            TestCalc.TestAugmentFlowsOnPath(path, 3, network);

            Assert.AreEqual(2, network.FindEdge(1, 0).Flow);
            Assert.AreEqual(2, network.FindEdge(2, 1).Flow);
        }
    }

    /// <summary>
    /// Uses a Ford-Fulkerson (depth-first search) approach to find an augmented path in the network.
    /// </summary>
    internal class FordFulkersonSearch : ISearchMethod
    {
        public IEnumerable<IList<Step>> FindAugmentingPath(FlowNetwork network)
        {
            var path = new Step[network.TotalVertices];

            // Begin potential augmenting path at the source vertex.
            path[network.SourceIndex] = new Step { Previous = -1 };
            var verticesToVisit = new Stack<int>();
            verticesToVisit.Push(network.SourceIndex);

            while (verticesToVisit.Any())
            {
                // Expand augmented path by popping next vertex and exploring adjacent vertexes.
                var u = verticesToVisit.Pop();

                // Try to make forward progress by checking edges forwards edges (u,v)
                // Forward edges must have unfilled capacity.
                var forwardEdges = network.FindForwardEdges(u);
                foreach (var forwardEdge in forwardEdges)
                {
                    int v = forwardEdge.End;

                    // If not yet visited and has unused capacity then plan to increase.
                    if (path[v] == null && forwardEdge.Capacity > forwardEdge.Flow)
                    {
                        path[v] = new Step { Previous = u, Forward = true };
                        if (v == network.SinkIndex)
                        {
                            yield return path; // Found augmenting path
                        }
                        verticesToVisit.Push(v);
                    }
                }

                // Try to make backwards progress by checking edges backwards edges (v,u)
                // Backwards edges must have flow that can be reduced.
                var backwardEdges = network.FindBackwardEdges(u);
                foreach (var backwardEdge in backwardEdges)
                {
                    int v = backwardEdge.Start;

                    // Try to find an incoming edge into u who hasn't been visited and whose flow can be reduced.
                    if (path[v] == null && backwardEdge.Flow > 0)
                    {
                        path[v] = new Step { Previous = u, Forward = false };
                        verticesToVisit.Push(v);
                    }
                }
            }

            yield break; // No augmenting path was found
        }
    }

    [TestClass]
    public class FordFulkersonSearchTests
    {
        [TestMethod]
        public void WhenOnlySinglePath_ExpectPathReturned()
        {
            var edges = new[]
            {
                Tuple.Create(0, 1, 5),
                Tuple.Create(1, 2, 5),
                Tuple.Create(2, 3, 5),
            };
            var network = new FlowNetwork(edges, 0, 3);
            var search = new FordFulkersonSearch();

            var path = search.FindAugmentingPath(network).Single();

            Assert.AreEqual(2, path[3].Previous);
            Assert.AreEqual(1, path[2].Previous);
            Assert.AreEqual(0, path[1].Previous);
            Assert.AreEqual(-1, path[0].Previous);
        }

        [TestMethod]
        public void WhenOnlySinglePathWithCapacity_ExpectPathReturned()
        {
            var edges = new[]
            {
                Tuple.Create(0, 1, 5),
                Tuple.Create(1, 2, 5),
                Tuple.Create(2, 4, 5),
                Tuple.Create(2, 3, 5),
                Tuple.Create(4, 3, 5),
            };
            var network = new FlowNetwork(edges, 0, 3);
            network.AddFlow(2, 3, 5);

            var search = new FordFulkersonSearch();
            var path = search.FindAugmentingPath(network).Single();

            Assert.AreEqual(4, path[3].Previous);
            Assert.AreEqual(2, path[4].Previous);
            Assert.AreEqual(1, path[2].Previous);
            Assert.AreEqual(0, path[1].Previous);
            Assert.AreEqual(-1, path[0].Previous);
        }
    }

    #endregion

    #region FLOW NETWORK

    /// <summary>
    /// Implemented in a hybrid dense/sparse fashion where the vertices are a dense array providing O(1) lookup
    /// and the edges are implemented as a sparse collection with a worse case O(E) lookup.  Works best
    /// for networks where there are many vertices with few (localized traffic?) edges between them.
    /// </summary>
    public class FlowNetwork 
    {
        private class Vertex
        {
            public IList<Edge> Forward { get; private set; }
            public IList<Edge> Backwards { get; private set; }

            public Vertex()
            {
                Forward = new List<Edge>();
                Backwards = new List<Edge>();
            }

            public void AddForwardEdge(Edge edgeToAdd)
            {
                Forward.Add(edgeToAdd);
                Debug.Assert(Forward.All(x => x.Start == edgeToAdd.Start), "All forward edge links should start on the current vertex.");
            }

            public void AddBackwardEdge(Edge edgeToAdd)
            {
                Backwards.Add(edgeToAdd);
                Debug.Assert(Backwards.All(x => x.End == edgeToAdd.End), "All backwards edge links should end on the current vertex.");
            }
        }

        public readonly int TotalVertices;

        public readonly int SourceIndex;

        public readonly int SinkIndex;

        private IList<Vertex> Vertices;

        public FlowNetwork(IEnumerable<Tuple<int, int, int>> edges, int source, int sink)
        {
            SourceIndex = source;

            SinkIndex = sink;

            TotalVertices = edges.Select(x => x.Item1)
                .Concat(edges.Select(x => x.Item2))
                .Max() + 1;

            // Initialize vertex collection;
            Vertices = Enumerable.Range(0, TotalVertices)
                .Select(x => { return new Vertex(); })
                .ToList();

            // Populate edges
            foreach (var edge in edges)
            {
                if (FindEdge(edge.Item1, edge.Item2) != null)
                {
                    throw new Exception("Duplicate edge definition found while constructing flow network.");
                }

                var edgeToAdd = new Edge { Start = edge.Item1, End = edge.Item2, Capacity = edge.Item3 };

                var startVertex = Vertices[edgeToAdd.Start];
                startVertex.AddForwardEdge(edgeToAdd);

                var endVertex = Vertices[edgeToAdd.End];
                endVertex.AddBackwardEdge(edgeToAdd);
            }
        }

        public IEnumerable<Edge> FindForwardEdges(int u)
        {
            var vertex = Vertices[u];
            if (vertex.Forward == null)
            {
                return Enumerable.Empty<Edge>();
            }
            return vertex.Forward;
        }

        public IEnumerable<Edge> FindBackwardEdges(int u)
        {
            var vertex = Vertices[u];
            if (vertex.Backwards == null)
            {
                return Enumerable.Empty<Edge>();
            }
            return vertex.Backwards;
        }

        /// <summary>
        /// Worse case O(E) would require one vertex to have all edges.
        /// Return null if edge does not exist.
        /// </summary>
        public Edge FindEdge(int u, int v)
        {
            var vertex = Vertices[u];
            return vertex.Forward.FirstOrDefault(x => x.End == v);
            Debug.Assert(vertex.Forward.All(x => x.Start == u), "Expected all forward edges to start at the current vertex.");
        }

        public void AddFlow(int u, int v, int flowToAdd)
        {
            var edge = FindEdge(u, v);

            if (edge.Flow + flowToAdd > edge.Capacity)
            {
                throw new ArgumentOutOfRangeException("Additional flow would exceed vertex's capacity.");
            }

            edge.Flow += flowToAdd;
        }
    }

    [TestClass]
    public class FlowNetworkTests
    {
        [TestMethod]
        public void WhenAddFlowToEdge_ExpectFlowRetainedBetweenEdgeQueries()
        {
            var edges = new[]
            {
                Tuple.Create(1, 2, 3),
                Tuple.Create(2, 1, 4),
            };
            var network = new FlowNetwork(edges, 3, 1);

            var before = network.FindEdge(1, 2);
            Assert.AreEqual(3, before.Capacity);
            Assert.AreEqual(0, before.Flow);

            network.AddFlow(1, 2, 3);

            var after = network.FindEdge(1, 2);
            Assert.AreEqual(3, after.Capacity);
            Assert.AreEqual(3, after.Flow);
        }

        [TestMethod]
        public void WhenAddMaxFlowToEachEdge_ExpectEachVertexFlowEqualCapacity()
        {
            var edges = new[]
            {
                Tuple.Create(1, 2, 3),
                Tuple.Create(2, 1, 4),
                Tuple.Create(3, 2, 5),
                Tuple.Create(3, 1, 6)
            };
            var network = new FlowNetwork(edges, 3, 1);

            network.AddFlow(1, 2, 3);
            network.AddFlow(2, 1, 4);
            network.AddFlow(3, 2, 5);

            network.AddFlow(3, 1, 6);

            foreach (var u in Enumerable.Range(0, 3))
            {
                foreach (var v in Enumerable.Range(0, 3))
                {
                    var edge = network.FindEdge(u, v);
                    if (edge == null)
                    {
                        continue;
                    }
                    Assert.AreEqual(edge.Capacity, edge.Flow);
                }
            }
        }
    }

    [DebuggerDisplay("{Start}->{End}")]
    public class Edge
    {
        public int Start { get; set; }
        public int End { get; set; }
        public int Capacity { get; set; }
        public int Flow { get; set; }
    }

    public class Step
    {
        public int Previous { get; set; }
        public bool Forward { get; set; } 
    }

    #endregion
}
