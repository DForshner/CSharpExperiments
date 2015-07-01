using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// The 2SAT problem is a special case of the boolean satisfiability problem (normally NP-Complete) which is solvable in polynomial time.
///
/// Aspvall, Plass & Tarjan Creates an implication graph and check that each variable and its negation belong to different components.  
/// Two terms must have the same value if they exist in the same SSC.  If a a variable and its negation occur in the same component 
/// then it will be impossible to assign both terms the same value.
/// 
/// Time Complexity: O(n)
///
/// The graph is stored in the following format:
/// [number_of_variables_and_clauses]
/// [clause_1_variable_1] [clause_1_variable_2]
/// [clause_2_variable_1] [clause_2_variable_2]

namespace Solve2SATWithImplicationGraphAndSCC
{
    public class Clause
    {
        /// <summary>
        /// A or ~A (-ve)
        /// </summary>
        public readonly int First;

        /// <summary>
        /// B or ~B (-ve)
        /// </summary>
        public readonly int Second;

        public Clause(int var1, int var2)
        {
            this.First = var1;
            this.Second = var2;
        }

        public bool Evaluate(bool val1, bool val2)
        {
            if (First < 0)
            {
                val1 = !val1;
            }

            if (Second < 0)
            {
                val2 = !val2;
            }

            return val1 || val2;
        }
    }

    /// <summary>
    /// A 2-CNF (conjunctive normal form) formula.
    /// </summary>
    public class Formula
    {
        /// <summary>
        /// A list of clauses representing a 2-CNF formula
        /// </summary>
        public readonly IReadOnlyList<Clause> Clauses;

        public Formula(IReadOnlyList<Clause> clauses)
        {
            Clauses = clauses;
        }
    }

    public class DirectedGraph<T> {

        /// <summary>
        /// Nodes stored with a set of destination nodes.
        /// </summary>
        private readonly Dictionary<T, HashSet<T>> _graph = new Dictionary<T, HashSet<T>>();

        public void AddNode(T node) {
            if (_graph.ContainsKey(node))
            {
                return; // Nothing to do
            }

            // Add node with no outgoing edges
            _graph.Add(node, new HashSet<T>());
        }

        /// <summary>
        /// Add directed edge between nodes.
        /// </summary>
        public void AddEdge(T source, T destination) {
            if (!_graph.ContainsKey(source) || !_graph.ContainsKey(destination))
            {
                throw new Exception("Both nodes must be in the graph.");
            }

            _graph[source].Add(destination);
        }

        /// <summary>
        /// Returns all destination nodes which have outgoing links from this node. 
        /// </summary>
        public HashSet<T> EdgesFrom(T node) {
            var outgoingEdges = new HashSet<T>();
            _graph.TryGetValue(node, out outgoingEdges);
            if (outgoingEdges == null)
            {
                throw new Exception("Source node does not exist.");
            }

            return outgoingEdges;
        }

        /// <summary>
        /// Iterator for all nodes in graph
        /// </summary>
        public IEnumerable<T> Nodes 
        {
            get { return _graph.Keys; }
        }
    }

    public class KosarajuStronglyConnectedComponent<T>
    {
        /// Return a mapping of nodes to strongly connected components.  Each SCC is identified
        /// by a different integer so nodes sharing an id belong to the same strongly connected
        /// component.
        public static Dictionary<T, int> StronglyConnectedComponents(DirectedGraph<T> graph) 
        {
            // Get the order in which nodes are visited via a depth first search.
            var reverseGraph = ReverseGraph(graph);
            Stack<T> visitOrder = GetDFSVisitOrder(reverseGraph);

            // Iterate over the nodes, marking all reachable nodes with the current
            // component id if they are not already part of another component.
            Dictionary<T, int> nodeToComponentId = new Dictionary<T, int>();
            int componentId = 0;
            while (visitOrder.Any()) {
                var startPoint = visitOrder.Pop();
                if (nodeToComponentId.ContainsKey(startPoint))
                {
                    continue;  // Node has already been labeled so skip it.
                }

                // Mark all nodes that are reachable with the current component Id.
                MarkReachableNodes(startPoint, graph, nodeToComponentId, componentId);

                ++componentId;
            }

            return nodeToComponentId;
        }

        /// <summary>
        /// Returns a reversed copy of the graph.
        /// </summary>
        private static DirectedGraph<T> ReverseGraph(DirectedGraph<T> graphToReverse) {
            var result = new DirectedGraph<T>();

            // Copy the nodes
            foreach (var node in graphToReverse.Nodes)
            {
                result.AddNode(node);
            }

            // Add the reverse of all the edges
            foreach (var node in graphToReverse.Nodes)
            {
                foreach (var destNode in graphToReverse.EdgesFrom(node))
                {
                    result.AddEdge(destNode, node);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Returns a stack of nodes stored in the order that a DFS traversed the graph.
        /// </summary>
        private static Stack<T> GetDFSVisitOrder(DirectedGraph<T> graphToSearch) {
            var searchOrder = new Stack<T>();
            var visited = new HashSet<T>();

            foreach (var node in graphToSearch.Nodes)
            {
                // Try starting a search from every node.
                depthFirstSearch(node, graphToSearch, searchOrder, visited);
            }

            return searchOrder;
        }

        private static void depthFirstSearch(T currentNode, DirectedGraph<T> graphToSearch, Stack<T> visitOrder, HashSet<T> visited) {
            if (visited.Contains(currentNode))
            {
                return; // Node has already been visited
            }

            visited.Add(currentNode);

            foreach (var destNode in graphToSearch.EdgesFrom(currentNode))
            {
                depthFirstSearch(destNode, graphToSearch, visitOrder, visited);
            }

            // All nodes reachable from this one have been visited so add it to the visit order. 
            visitOrder.Push(currentNode);
        }

        /// <summary>
        /// Marks all reachable nodes from the current node with the component Id.
        /// </summary>
        private static void MarkReachableNodes(T currentNode, DirectedGraph<T> graphToSearch, Dictionary<T, int> nodesToComponentIds, int componentId) {
            if (nodesToComponentIds.ContainsKey(currentNode))
            {
                // Node has already been visited so we can stop the search.
                return; 
            }

            // Since the node has not been visited add it to the current component.
            nodesToComponentIds.Add(currentNode, componentId);

            // Explore all reachable nodes assigning them to the current component.
            foreach (var destNode in graphToSearch.EdgesFrom(currentNode))
            {
                MarkReachableNodes(destNode, graphToSearch, nodesToComponentIds, componentId);
            }
        }
    }

    public class Solve2SATWithSCC
    {
        public static bool IsSatisfiable(Formula formula) {

            var variables = CreateSetOfAllVariablesInFormula(formula);

            var implications = CreateImplicationGraph(formula, variables);

            var scc = KosarajuStronglyConnectedComponent<int>.StronglyConnectedComponents(implications);

            // Search the strongly connected components.  If a literal and its negation are in the
            // same component the clause is unsatisfiable.
            foreach (var variable in variables)
            {
                if (scc[variable] == scc[-variable])
                {
                    // A literal and its negation are in the same SSC so formula is unsatisfiable.
                    return false;
                }
            }

            // Formula has a satisfiable assignment.
            return true;
        }

        private static HashSet<int> CreateSetOfAllVariablesInFormula(Formula formula)
        {
            var variables = new HashSet<int>();
            foreach (var clause in formula.Clauses)
            {
                variables.Add(clause.First);
                variables.Add(clause.Second);
            }
            return variables;
        }

        private static DirectedGraph<int> CreateImplicationGraph(Formula formula, HashSet<int> variables)
        {
            var implications = new DirectedGraph<int>();

            // Add both variable and its negation as nodes
            foreach (var variable in variables)
            {
                implications.AddNode(variable);
                implications.AddNode(-variable);
            }

            // For each clause (A or B) add (~A -> B) and (~B -> A) as edges.
            foreach (var clause in formula.Clauses)
            {
                implications.AddEdge(-clause.First, clause.Second); // Not A implies B
                implications.AddEdge(-clause.Second, clause.First); // Not B implies A
            }

            return implications;
        }

        private static Formula ReadClausesFromFile(string fileName)
        {
            var data = ReadFile(fileName);
            var lines = data.Split('\n');

            // First line contains number of clauses in file
            var header = lines[0];
            var numberOfClauses = Int32.Parse(header);

            var clauses = new List<Clause>();
            for (var i = 1; i < lines.Count(); i++)
            {
                var line = lines[i];

                // Skip empty lines
                if (String.IsNullOrEmpty(line)) { continue; }

                var columns = line.Split(' ');
                var var1 = Int32.Parse(columns[0]);
                var var2 = Int32.Parse(columns[1]);

                clauses.Add(new Clause(var1, var2));
            }

            if (numberOfClauses != clauses.Count)
            {
                throw new Exception("The number of clauses read from file didn't match number of cities specified in file header.");
            }

            return new Formula(clauses.AsReadOnly());
        }

        private static string ReadFile(string fileName)
        {
            const string PATH = "../../../";
            try
            {
                using (var sr = new StreamReader(PATH + fileName))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return null;
        }

        public static void Main(string[] args)
        {
            for (var i = 1; i <= 6; i++)
            {
                var fileName = "2sat" + i.ToString() + ".txt";
                Console.WriteLine("file: " + fileName + " - Start");
                var canBeSatisfied = IsSatisfiable(ReadClausesFromFile(fileName));
                Console.WriteLine(fileName + " - Result: " + canBeSatisfied);
            }

            Console.WriteLine("[Press any key to exit]");
            Console.ReadKey();
        }
    }
}
