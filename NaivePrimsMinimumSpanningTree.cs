using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// An inefficient version of PRIM's algorithm for finding the minimum spanning tree of a undirected graph.
// Time Complexity: O(mn)
//
// TODO: Clean up the code and use a heap to keep track of the cheapest edge to explore.
//
// The graph is stored in a text file in the format:
// [number_of_nodes] [number_of_edges]
// [one_node_of_edge_1] [other_node_of_edge_1] [edge_1_cost]
// [one_node_of_edge_2] [other_node_of_edge_2] [edge_2_cost]

namespace NaivePrimsMinimumSpanningTree
{
    public class Program 
    {
        static void Main(string[] args)
        {
            var data = ReadFile();

            var adjacency = ParseGraphFromFile(data);

            var minSpanningTree = BuildMST(adjacency);

            var sumOfMSTEdgeCosts = GetSumOfEdges(minSpanningTree);

            Console.WriteLine("\n\nTotal cost of the MST: " + sumOfMSTEdgeCosts);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static int[,] BuildMST(int[,] adjacency)
        {
            var startingNode = 1;
            var visited = new HashSet<int>();
            visited.Add(startingNode);

            var potentialEdges = new List<Tuple<int, int, int>>();
            AddNodesEdges(adjacency, startingNode, potentialEdges);

            var minSpanningTree = new int[adjacency.GetLength(0), adjacency.GetLength(1)];

            while (potentialEdges.Any())
            {
                // TODO: This is going to re-sort the entire list each iteration :-(
                // Change to use a heap.
                var cheapest = potentialEdges.OrderBy(x => x.Item3).First();
                potentialEdges.Remove(cheapest);
                var u = cheapest.Item1;
                var v = cheapest.Item2;
                var cost = cheapest.Item3;

                Debug.Assert(visited.Contains(u));

                // Does this edge lead somewhere new?
                if (visited.Contains(v))
                {
                    continue;
                }

                minSpanningTree[u, v] = cost;
                minSpanningTree[v, u] = cost;

                AddNodesEdges(adjacency, v, potentialEdges);

                visited.Add(v);
            }
            return minSpanningTree;
        }

        /// <summary>
        /// TODO: We only need to count half of the nodes.
        /// </summary>
        private static int GetSumOfEdges(int[,] adjacency)
        {
            var cost = 0;
            for (var u = 0; u < adjacency.GetLength(0); u++)
            {
                for (var v = 0; v < adjacency.GetLength(0); v++)
                {
                    cost += adjacency[u, v];
                }
            }

            return cost / 2;
        }

        private static void AddNodesEdges(int[,] adjacency, int nodeToSearch, List<Tuple<int, int, int>> potentialEdges)
        {
            for (var v = 0; v < adjacency.GetLength(0); v++)
            {
                if (adjacency[nodeToSearch, v] != 0 && v != nodeToSearch)
                {
                    potentialEdges.Add(Tuple.Create(nodeToSearch, v, adjacency[nodeToSearch, v]));
                }
            }
        }

        private static int[,] ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of nodes and edges 
            var header = lines.First().Split(' ');
            var numNodes = Int32.Parse(header[0]);
            var numEdges = Int32.Parse(header[1]);

            var edgeData = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var adjacency = new int[numNodes + 1, numNodes + 1];

            var edgesProcessed = 0;
            foreach(var edge in edgeData)
            {
                var details = edge.Data.Split(' ');
                var u = Int32.Parse(details[0]);
                var v = Int32.Parse(details[1]);
                var cost = Int32.Parse(details[2]);

                if (adjacency[v,u] != 0 || adjacency[u,v] != 0)
                {
                    throw new Exception("Multiple edges for same node pair found in file.");
                }

                adjacency[u,v] = cost;
                adjacency[v,u] = cost;
                edgesProcessed++;
            }

            if (edgesProcessed != numEdges)
            {
                throw new Exception("Number of edges processed was different than number of edges listed in file header.");
            }

            return adjacency;
        }

        private static string ReadFile()
        {
            var data = string.Empty;
            try
            {
                using (var sr = new StreamReader("../../edges.txt"))
                {
                    data = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return data;
        }
    }
}