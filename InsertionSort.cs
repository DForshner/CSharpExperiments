//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//namespace DijkstrasShortestPath
//{
//    /// <summary>
//    /// Represents a single one-way connection to a node.
//    /// For symmetric connections two NodeConnection objects will be created.
//    /// </summary>
//    internal class NodeConnection
//    {
//        internal Node Target {get; private set;}
//        internal double Distance {get; private set;}

//        internal NodeConnection(Node target, double distance)
//        {
//            this.Target = target;
//            this.Distance = distance;
//        }
//    }

//    /// <summary>
//    /// Represents a single node on the graph.
//    /// </summary>
//    internal class Node
//    {
//        private readonly List<NodeConnection> connections = new List<NodeConnection>();

//        /// <summary>
//        /// Name of node in graph.
//        /// </summary>
//        internal string Name { get; private set; }

//        /// <summary>
//        /// The shortest possible route from the starting node that was found.
//        /// </summary>
//        internal double DistanceFromStart { get; set; }

//        internal IReadOnlyList<NodeConnection> Connections
//        {
//            get { return connections.AsReadOnly();  }
//        }

//        internal Node(string name)
//        {
//            this.Name = name;
//        }

//        internal void AddConnection(Node targetNode, double distance, bool isSymmetric)
//        {
//            // preconditions
//            if (targetNode == null) throw new ArgumentNullException("targetNode");
//            if (targetNode == this) throw new ArgumentException("Node may not connect to itself.");
//            if (distance <= 0) throw new ArgumentException("Distance must be positive.");

//            this.connections.Add(new NodeConnection(targetNode, distance));

//            // Create mirroring connection from target node to current node.
//            if (isSymmetric) targetNode.AddConnection(this, distance, false);
//        }

//    }

//    /// <summary>
//    /// Represents the entire graph containing all the nodes and their connections.
//    /// </summary>
//    public class Graph
//    {
//        internal IDictionary<string, Node> Nodes { get; private set; }

//        public Graph()
//        {
//            this.Nodes = new Dictionary<string, Node>();
//        }

//        /// <summary>
//        /// Adds a unique node to the graph.
//        /// </summary>
//        public void AddNode(string name)
//        {
//            var node = new Node(name);
//            Nodes.Add(name, node);
//        }

//        public void AddConnection(string fromNode, string toNode, int distance, bool isSymmetric)
//        {
//            Nodes[fromNode].AddConnection(Nodes[toNode], distance, isSymmetric);
//        }
//    }

//    /// <summary>
//    /// Finds the shortest route between two nodes.
//    /// Note: Unconnected nodes are assigned a distance of infinity.
//    /// </summary>
//    public class ShortestPathCalculator
//    {
//        public IDictionary<string ,double> Calculate(Graph graph, string startingNode)
//        {
//            // Preconditions
//            if (!graph.Nodes.ContainsKey(startingNode))
//                throw new ArgumentException("Starting node must exist in graph.");

//            InitialiseGraph(graph, startingNode);
//            ProcessDistancesFromStart(graph, startingNode);
//            return ExtractDistances(graph);
//        }

//        /// <summary>
//        /// Initialize the graph by setting the distance of every node to infinity, except for the
//        /// starting node which has a distance of zero.  Mark every node in the graph as unprocessed.
//        /// </summary>
//        private void InitialiseGraph(Graph graph, string startingNode)
//        {
//            foreach (var node in graph.Nodes.Values)
//                node.DistanceFromStart = double.PositiveInfinity;

//            graph.Nodes[startingNode].DistanceFromStart = 0;
//        }

//        /// <summary>
//        /// Traverse graph and calculate the distance from the start for each node.
//        /// </summary>
//        private void ProcessDistancesFromStart(Graph graph, string startingNode)
//        {
//            var unvisitedNodes = graph.Nodes.Values.ToList();
//            while(true)
//            {
//                // Next node to process is the node with shortest distance that has been processed.
//                var nextNode = unvisitedNodes
//                    .OrderBy(x => x.DistanceFromStart)
//                    .FirstOrDefault(x => !double.IsPositiveInfinity(x.DistanceFromStart));

//                // Stop when all nodes have been visited.
//                if (nextNode == null)
//                    break;

//                ProcessNeighbors(nextNode, unvisitedNodes);
//                unvisitedNodes.Remove(nextNode);
//            }
//        }

//        /// <summary>
//        /// Calculates the distances for all neighbors of the node.
//        /// </summary>
//        private void ProcessNeighbors(Node nodeToVisit, List<Node> unvisitedNodes)
//        {
//            Debug.Assert(nodeToVisit.DistanceFromStart != double.PositiveInfinity, "Expected processed node with a distance from the start.");

//            // Get all neighbors of this node.
//            var connections = nodeToVisit.Connections.Where(c => unvisitedNodes.Contains(c.Target));

//            foreach (var connection in connections)
//            {
//                // add the connection distance to the distance from start of the current node.
//                var distance = nodeToVisit.DistanceFromStart + connection.Distance;

//                // If distance is shorter then than the neighbors current distance from start
//                // update it with the shorter value.
//                if (distance < connection.Target.DistanceFromStart)
//                    connection.Target.DistanceFromStart = distance;
//            }
//        }

//        /// <summary>
//        /// Create dictionary of nodes and their distances from the starting node.
//        /// </summary>
//        private IDictionary<string, double> ExtractDistances(Graph graph)
//        {
//            return graph.Nodes.ToDictionary(n => n.Key, n => n.Value.DistanceFromStart);
//        }
//    }

//    public static class Program
//    {
//        public static void Main()
//        {
//            Graph graph = new Graph();

//            //Nodes
//            graph.AddNode("A");
//            graph.AddNode("B");
//            graph.AddNode("C");
//            graph.AddNode("D");
//            graph.AddNode("E");
//            graph.AddNode("F");
//            graph.AddNode("G");
//            graph.AddNode("H");
//            graph.AddNode("I");
//            graph.AddNode("J");
//            graph.AddNode("Z");

//            //Connections
//            graph.AddConnection("A", "B", 14, true);
//            graph.AddConnection("A", "C", 10, true);
//            graph.AddConnection("A", "D", 14, true);
//            graph.AddConnection("A", "E", 21, true);
//            graph.AddConnection("B", "C", 9, true);
//            graph.AddConnection("B", "E", 10, true);
//            graph.AddConnection("B", "F", 14, true);
//            graph.AddConnection("C", "D", 9, false);
//            graph.AddConnection("D", "G", 10, false);
//            graph.AddConnection("E", "H", 11, true);
//            graph.AddConnection("F", "C", 10, false);
//            graph.AddConnection("F", "H", 10, true);
//            graph.AddConnection("F", "I", 9, true);
//            graph.AddConnection("G", "F", 8, false);
//            graph.AddConnection("G", "I", 9, true);
//            graph.AddConnection("H", "J", 9, true);
//            graph.AddConnection("I", "J", 10, true);

//            var calculator = new ShortestPathCalculator();
//            var start = "G";
//            var distances = calculator.Calculate(graph, start);

//            foreach (var d in distances) { Console.WriteLine("{0}, {1}", d.Key, d.Value); }

//            Console.WriteLine("Press [Enter] to exit.");
//            Console.ReadLine();
//        }
//    }
//}
