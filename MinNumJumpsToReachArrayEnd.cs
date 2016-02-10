using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// Given an array where each element is the maximum integer steps that can be made from that element.
// Find the minimum number of jumps to reach the end of the array from the start.  If an element is zero
// then you cannot move through that element.

namespace MinNumJumpsToReachArrayEnd
{
    using Path = List<int>;

    public interface ISolution
    {
        String Name { get; }

        IEnumerable<int> Solve(IList<int> steps);
    }

    /// <summary>
    /// Complexity: O(n^2) - In the worst case each element has enough distance to reach
    /// the element before the end so for each step we have to search all possible other steps.
    /// </summary>
    public class BreadthFirstSearchSolution : ISolution
    {
        public String Name { get { return "Breadth First Search Solution"; } }

        public IEnumerable<int> Solve(IList<int> steps)
        {
            Debug.Assert(steps != null);

            var toProcess = new Queue<Path>();
            toProcess.Enqueue(new Path { 0 }); // Start from first element

            int lastElementIdx = steps.Count - 1;
            while (toProcess.Any())
            {
                var path = toProcess.Dequeue();
                var curr = path.Last();

                if (curr == lastElementIdx)
                {
                    // Path the end of the array
                    return path.Select(x => steps[x]);
                }

                // Store each possible path
                for (int i = 1; i <= steps[curr]; ++i)
                {
                    int next = curr + i;
                    if (next >= steps.Count)
                    {
                        continue; // Past end of array so discard
                    }

                    var possiblePath = new Path(path);
                    possiblePath.Add(next);

                    toProcess.Enqueue(possiblePath);
                }
            }

            // No path found
            return Enumerable.Empty<int>();
        }
    }

    /// <summary>
    /// Working from left to right find the first element that can reach each position
    /// Complexity: O(n^2)
    /// </summary>
    public class DynamicProgrammingSolution : ISolution
    {
        public String Name { get { return "Dynamic Programming Solution"; } }

        public IEnumerable<int> Solve(IList<int> steps)
        {
            Debug.Assert(steps != null);

            var jumps = new int[steps.Count()];

            jumps[0] = Int16.MinValue;
            for (var i = 0; i < jumps.Length; ++i)
            {
                jumps[i] = Int16.MinValue;

                // Find first element that can reach this one
                for (var j = 0; j < i; ++j)
                {
                    Debug.Assert(j < steps.Count);
                    if (steps[j] + j >= i)
                    {
                        // Element j can reach element i
                        jumps[i] = j;
                        break;
                    }
                }
            }

            // Working backwards reconstruct the path to the first element.
            var path = new List<int>();
            var curr = steps.Count - 1;
            while (curr >= 0)
            {
                path.Add(steps[curr]);
                curr = jumps[curr];
            }

            path.Reverse();

            return path;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            ShowSolution(new BreadthFirstSearchSolution());
            ShowSolution(new DynamicProgrammingSolution());
        }

        private static void ShowSolution(ISolution solution)
        {
            var steps = new[] { 1, 3, 5, 8, 9, 2, 6, 7, 6, 8, 3, 1, 2, 3, 4, 5, 1, 1, 1, 2, 2, 2, 3, 3, 3 };

            Console.WriteLine(solution.Name);
            var path = solution.Solve(steps.ToList());
            DisplayPath(path);
        }

        private static void DisplayPath(IEnumerable<int> path)
        {
            foreach (var step in path)
            {
                Console.Write(step + "->");
            }
            Console.WriteLine();
        }
    }
}
