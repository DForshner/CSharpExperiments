using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Use the FloydWarshall algorithm to compute all pairs shortest paths.
// For each graph find the shortest path between any two pairs of vertices
// present in the graph.
//
// Subproblems are stored in a matrix of Int16 to reduce memory usage.
//
// The graph is stored in the following format:
// [number_of_vertices][number_of_edges]
// [edge_1_tail] [edge_1_head] [edge_1_length]
// [edge_2_tail] [edge_2_head] [edge_2_length]

namespace FindShortedPathBetweenAllPairsOfVerticesFloydWarshall 
{
    public class Edge 
    {
        public readonly int Tail;
        public readonly int Head;
        public readonly int Length;

        public Edge(int tail, int head, int length)
        {
            this.Tail = tail;
            this.Head = head;
            this.Length = length;
        }
    }

    public static class FindShortestPathFloydWarshall
    {
        private const int NEGATIVE_EDGE_CYCLE_DETECTED = -1;

        /// <summary>
        /// Find the shortest path length between any two vertices present
        /// in the graph.  Returns -1 if there is a negative edge cycle present.
        /// </summary>
        public static int FindShortestPathLength(int numVertices, IReadOnlyList<Edge> edges)
        {
            var subProblems = BuildSubProblemMatrix(edges, numVertices);

            CalculateSubProblems(numVertices, subProblems);

            if (DetectNegativeEdgeCycle(numVertices, subProblems)) ;
            {
                return NEGATIVE_EDGE_CYCLE_DETECTED;
            }

            return FindShortestPath(numVertices, subProblems);
        }

        /// <summary>
        /// Build the initial sub problem matrix.
        /// </summary>
        private static short[, ,] BuildSubProblemMatrix(IReadOnlyList<Edge> edges, int numVertices)
        {
            // Subproblems indexed by i,j,k  
            var subProblems = new Int16[numVertices, numVertices, numVertices];

            var edgeLookup = edges.ToLookup(x => x.Head);

            // Initialize sub problem matrix for the first round where there are
            // only direct paths between i and j (k == 0).
            for (var i = 0; i < numVertices; i++)
            {
                for (var j = 0; j < numVertices; j++)
                {
                    // Case 1: If i == j then same node so zero distance.
                    if (i == j)
                    {
                        subProblems[i, j, 0] = 0;
                        continue;
                    }

                    // Case 2: If there is a directed edge from i to j use the edge cost.
                    var inwardEdges = edgeLookup[j];
                    var edge = inwardEdges.FirstOrDefault(x => x.Tail == i && x.Head == j);
                    if (edge != null)
                    {
                        subProblems[i, j, 0] = (Int16)edge.Length;
                    }
                    else
                    {
                        // Case 3: Otherwise +ve infinity.
                        subProblems[i, j, 0] = Int16.MaxValue;
                    }
                }
            }

            // Initialize sub problem matrix for the rest of the rounds.
            for (var k = 1; k < numVertices; k++)
            {
                for (var i = 0; i < numVertices; i++)
                {
                    for (var j = 0; j < numVertices; j++)
                    {
                        // Case 1: Zero if same node 
                        // Case 2: Otherwise +ve infinity
                        subProblems[i, j, k] = (i == j) ? (Int16)0 : Int16.MaxValue;
                    }
                }
            }

            return subProblems;
        }

        /// <summary>
        /// Calculate the sub-problems for k > 0 rounds
        /// </summary>
        private static void CalculateSubProblems(int numVertices, short[, ,] subProblems)
        {
            for (var k = 1; k < numVertices; k++)
            {
                for (var i = 0; i < numVertices; i++)
                {
                    for (var j = 0; j < numVertices; j++)
                    {
                        // Check if the path between i and j would be shorter if we
                        // included intermediate node k in the path.
                        var excludeVertex = subProblems[i, j, k - 1];

                        var includeVertex = subProblems[i, k, k - 1] + subProblems[k, j, k - 1];
                        if (includeVertex > Int16.MaxValue)
                        {
                            includeVertex = Int16.MaxValue;
                        }

                        subProblems[i, j, k] = Math.Min((Int16)includeVertex, excludeVertex);
                    }
                }
            }
        }

        /// <summary>
        /// Checks all possible pairs of vertices and looks for the shortest path between them.
        /// </summary>
        private static int FindShortestPath(int numVertices, short[, ,] subProblems)
        {
            var shortestPath = 0;
            for (var i = 0; i < numVertices; i++)
            {
                for (var j = 0; j < numVertices; j++)
                {
                    shortestPath = Math.Min(shortestPath, subProblems[i, j, numVertices - 1]);
                }
            }
            return shortestPath;
        }

        /// <summary>
        /// Returns true if there is a negative edge cycle in the graph.
        /// </summary>
        private static bool DetectNegativeEdgeCycle(int numVertices, short[, ,] subProblems)
        {
            // Detect negative edge cycles by checking all diagonal values (i == j) for a negative value 
            for (var ij = 0; ij < numVertices; ij++)
            {
                if (subProblems[ij, ij, numVertices - 1] < 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Program 
    {
        static void Main(string[] args)
        {
            for (var i = 1; i <= 3; i++)
            {
                var fileName = "g" + i.ToString() + ".txt"; 
                var graph = ParseGraphFromFile(ReadFile(fileName));
                var shortestPath = FindShortestPathFloydWarshall.FindShortestPathLength(graph.Item1, graph.Item2);

                if (shortestPath >= 0)
                {
                    Console.WriteLine("\n\nShortest Path for graph " + i.ToString() + ": " + shortestPath);
                }
                else
                {
                    Console.WriteLine("\n\nNegative edge cycle detected in graph " + i.ToString());
                }

            }

            Console.WriteLine("[Press any key to exit.]");
            Console.ReadKey();
        }

        private static Tuple<int, IReadOnlyList<Edge>> ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of nodes and edges 
            var header = lines.First().Split(' ');
            var numVertices = Int32.Parse(header[0]);
            var numEdges = Int32.Parse(header[1]);

            var edgeLines = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var edges = new List<Edge>(numEdges);
            var distinctVertices = new HashSet<int>();
            foreach (var line in edgeLines)
            {
                var columns = line.Data.Split(' ');
                var tail = Int32.Parse(columns[0]);
                var head = Int32.Parse(columns[1]);
                var length = Int32.Parse(columns[2]);

                edges.Add(new Edge(tail, head, length));

                if (!distinctVertices.Contains(tail)) { distinctVertices.Add(tail); }
                if (!distinctVertices.Contains(head)) { distinctVertices.Add(head); }
            }

            if (numEdges != edges.Count)
            {
                throw new Exception("The number of edges read from file didn't match number of edges specified in file header.");
            }

            if (numVertices != distinctVertices.Count)
            {
                throw new Exception("The number of vertices read from file didn't match number of vertices specified in file header.");
            }

            return Tuple.Create<int, IReadOnlyList<Edge>>(numVertices, edges.AsReadOnly());
        }

        private static string ReadFile(string fileName)
        {
            try
            {
                using (var sr = new StreamReader("../../../" + fileName))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return string.Empty;
        }
    }
}