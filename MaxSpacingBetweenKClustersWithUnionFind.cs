using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Given a graph compute the max-spacing k-clustering for 4 clusters.
//
// Creates 4 clusters of nodes that who's members are near each other.
// The max-spacing will be the shortest edge that spans two different 
// clusters.
//
// The graph of distances is stored in a text file in the format:
// [number of nodes]
// [edge 1 node 1] [edge 1 node 2] [edge 1 cost]
// [edge 2 node 1] [edge 2 node 2] [edge 2 cost]

namespace MaxSpacingKClusteringWithUnionFind
{
    public struct Edge
    {
        public readonly int U;
        public readonly int V;
        public readonly int Cost;

        public Edge(int u, int v, int cost)
        {
            U = u;
            V = v;
            Cost = cost;
        }
    }

    public class MaxSpaceKClustering
    {
        private readonly IReadOnlyList<Edge> _edges;
        private readonly int _numNodes;
        private readonly int _numClustersK;

        public MaxSpaceKClustering(IReadOnlyList<Edge> edges, int numNodes, int numClustersK)
        {
            this._edges = edges;
            this._numNodes = numNodes;
            this._numClustersK = numClustersK;
        }

        public int FindMaxSpacing()
        {
            // Sort the edges by ascending cost.
            var edges = _edges 
                .OrderBy(x => x.Cost)
                .ToList();

            // Take the min cost/length edge and add its nodes to the same cluster.
            var unionFind = new QuickUnionPathCompressionUF(_numNodes);
            foreach (var edge in edges)
            {
                unionFind.Union(edge.U - 1, edge.V - 1);

                if (unionFind.Count() == _numClustersK)
                {
                    // Stop adding nodes to clusters when there are K remaining clusters.
                    break;
                }
            }

            // Loop through the edges and find the first (minimum edge cost/length) that spans two different clusters.
            var max = Int32.MaxValue;
            foreach (var edge in edges)
            {
                if (unionFind.Find(edge.U - 1) != unionFind.Find(edge.V - 1))
                {
                    max = Math.Min(max, edge.Cost);
                }
            }

            return max;
        }
    }

    /// <summary>
    /// Based on Sedgewick's union find implementation.
    /// </summary>
    public class QuickUnionPathCompressionUF
    {
        private int[] _id;    // id[i] = parent of i
        private int _count;   // number of components

        // Create an empty union find data structure with N isolated sets.
        // Initially each item is in its own disjoint set.
        public QuickUnionPathCompressionUF(int N)
        {
            _count = N;

            _id = new int[N];
            for (int i = 0; i < N; i++)
            {
                _id[i] = i;
            }
        }

        // Return the number of disjoint sets.
        public int Count()
        {
            return _count;
        }

        // Return component identifier for component containing p
        public int Find(int p)
        {
            // Find the root under which p is stored.
            int root = p;
            while (root != _id[root])
            {
                root = _id[root];
            }

            // Perform path compression by linking every
            // parent of p directly to the root.
            while (p != root)
            {
                int newp = _id[p];
                _id[p] = root;
                p = newp;
            }

            return root;
        }

        // Are objects p and q in the same set?
        public bool Connected(int p, int q)
        {
            return Find(p) == Find(q);
        }

        // Replace sets containing p and q with their union.
        public void Union(int p, int q)
        {
            int i = Find(p);
            int j = Find(q);
            if (i == j)
            {
                // Already in same set
                return;
            }

            // Combine sets
            _id[i] = j;
            _count--;
        }
    }

    public class Program 
    {

        static void Main(string[] args)
        {
            var graph = ParseGraphFromFile(ReadFile());

            var solver = new MaxSpaceKClustering(graph.Item2, graph.Item1, 4);
            var max = solver.FindMaxSpacing();
            Console.WriteLine("\n\nMax-Space K-Clustering: " + max);

            Console.WriteLine("\n[Press any key to exit]");
            Console.ReadKey();
        }

        private static Tuple<int, IReadOnlyList<Edge>> ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of nodes and edges 
            var header = lines.First().Split(' ');
            var numNodes = Int32.Parse(header[0]);

            var edgeData = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var edges = new List<Edge>(numNodes);
            var distinctNodes = new HashSet<int>();
            foreach(var edge in edgeData)
            {
                var details = edge.Data.Split(' ');
                var u = Int32.Parse(details[0]);
                var v = Int32.Parse(details[1]);
                var cost = Int32.Parse(details[2]);

                edges.Add(new Edge(u, v, cost));
                distinctNodes.Add(u);
                distinctNodes.Add(v);
            }

            if (numNodes != distinctNodes.Count)
            {
                throw new Exception("Number of nodes processed was different than number of nodes specified in the file header.");
            }

            return Tuple.Create(numNodes, (IReadOnlyList<Edge>)edges.AsReadOnly());
        }

        private static string ReadFile()
        {
            try
            {
                using (var sr = new StreamReader("../../../clustering1.txt"))
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