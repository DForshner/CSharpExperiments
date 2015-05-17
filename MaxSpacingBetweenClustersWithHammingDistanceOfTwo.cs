using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnionFind;

// Consider a set of nodes where each node's location is represented by a
// bit sequence.  Two nodes are considered nearby if their respective bit
// sequences have a hamming distance of less than three (differ by only 2 bits).
// Cluster nearby nodes together and determine the overall number of clusters.
//
// The graph of distances is stored in a text file in the format:
// [number of nodes] [# of bits for each node's label]
// [first bit of node 1] ... [last bit of node 1]
// [first bit of node 2] ... [last bit of node 2]

namespace MaxSpacingBetweenClustersWithHammingDistanceOfTwo
{
    public struct Node 
    {
        public readonly int Id;
        public readonly string BitPattern;

        public Node(int id, string bitString)
        {
            Id = id;
            BitPattern = bitString;
        }
    }

    public class ClusterNearbyNodes
    {
        private readonly int _numBitsPerNode;

        public ClusterNearbyNodes(int numOfBits)
        {
            this._numBitsPerNode = numOfBits;
        }

        public int FindNumberOfClusters(IEnumerable<Node> nodes)
        {
            var nodesByBitPattern = GetDistinctNodes(nodes);

            var unionFind = new UnionFindWithPathCompression(nodesByBitPattern.Count);
            foreach (var node in nodesByBitPattern.Values)
            {
                // Search for nearby nodes by trying all possible nearby bit patterns.
                var nearbyBitPatterns = getNearbyNodes(node.BitPattern);
                foreach (var nearbyPattern in nearbyBitPatterns)
                {
                    if (nodesByBitPattern.ContainsKey(nearbyPattern))
                    {
                        // We found a nearby node so join their respective clusters together.
                        unionFind.Union(nodesByBitPattern[node.BitPattern].Id, nodesByBitPattern[nearbyPattern].Id);
                    }
                }
            }

            return unionFind.Count();
        }

        /// <summary>
        /// Produces a new set of nodes with no duplicate bit patterns.
        /// </summary>
        private Dictionary<string, Node> GetDistinctNodes(IEnumerable<Node> nodes)
        {
            var distinctBitPatterns = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (!distinctBitPatterns.Contains(node.BitPattern))
                {
                    distinctBitPatterns.Add(node.BitPattern);
                }
            }

            var distictNodes = distinctBitPatterns
                .Select((bitPattern, i) => new Node(i, bitPattern))
                .ToDictionary(x => x.BitPattern);

            Debug.Assert(distinctBitPatterns.Count == distictNodes.Count);

            return distictNodes;
        }

        /// <summary>
        /// Return all possible bit patterns with a hamming distance of less than three (differ by only 2 bits)
        /// </summary>
        private IEnumerable<String> getNearbyNodes(string bitPattern)
        {
            var bitPatternInt = GetBinaryBoolArray(bitPattern);

            // Swap the bit values for all possible pairs of 2 bits. 
            for (int i = 0; i < _numBitsPerNode; i++)
            {
                for (int j = 0; j < _numBitsPerNode; j++)
                {
                    var newNodeBinary = (bool[])bitPatternInt.Clone();
                    if (i != j)
                    {
                        newNodeBinary[i] = !newNodeBinary[i];
                        newNodeBinary[j] = !newNodeBinary[j];
                    }
                    else
                    {
                        newNodeBinary[i] = !newNodeBinary[i];
                    }

                    yield return CreateString(newNodeBinary);
                }
            }
        }

        private static bool[] GetBinaryBoolArray(string bitPattern)
        {
            return bitPattern
                .Select(c => c)
                .Where(c => c != ' ')
                .Select(c => c == '1')
                .ToArray();
        }

        private static string CreateString(bool[] binaryArray)
        {
            return binaryArray
                .Select(x => x.ToString() + " ")
                .Aggregate((x, y) => { return x + y; })
                .TrimEnd();
        }
    }

    public class Program 
    {
        static void Main(string[] args)
        {
            var nodeInfo = ParseGraphFromFile(ReadFile());

            var clustering = new ClusterNearbyNodes(nodeInfo.Item2);

            var numClusters = clustering.FindNumberOfClusters(nodeInfo.Item3);
            Console.WriteLine("\n\nNumber of clusters of nodes: " + numClusters.ToString());

            Console.WriteLine("\n[Press any key to exit]");
            Console.ReadKey();
        }

        private static Tuple<int, int, IEnumerable<Node>> ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of nodes and edges 
            var header = lines.First().Split(' ');
            var numNodes = Int32.Parse(header[0]);
            var numBits = Int32.Parse(header[1]);

            var nodeData = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var edges = new List<Node>(numNodes);

            var distinctNodes = new HashSet<int>();
            foreach(var edge in nodeData)
            {
                edges.Add(new Node(edge.Index, edge.Data.TrimEnd()));
                distinctNodes.Add(edge.Index);
            }

            if (numNodes != distinctNodes.Count)
            {
                throw new Exception("Number of nodes processed was different than number of nodes listed in file header.");
            }

            return Tuple.Create(numNodes, numBits, edges.AsEnumerable());
        }

        private static string ReadFile()
        {
            try
            {
                using (var sr = new StreamReader("../../../clustering_big.txt"))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read.");
                Console.WriteLine(e.Message);
            }
            return string.Empty;
        }
    }
}