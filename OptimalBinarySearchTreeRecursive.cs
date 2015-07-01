using System;
using System.Collections.Generic;

// Given an set of keys that occur in a BST and their relative access frequencies construct a BST
// to minimize the total cost of all searches.
//

namespace OptimalBinarySearchTreeRecursive
{
    public struct Key
    {
        public int Value;
        public int Frequency;

        public Key(int value, int frequency)
        {
            this.Value = value;
            this.Frequency = frequency;
        }
    }

    public static class OptimalBinarySearchTreeRecursive
    {
        public static int CalculateMinCostBinarySearchTree(IList<Key> keys)
        {
            // Keep track of already solved sub problems
            var subProblems = new Dictionary<Tuple<int, int>, int>();

            return getOptimalCost(keys, 0, keys.Count - 1, subProblems);
        }

        private static int getOptimalCost(IList<Key> keys, int i, int j, IDictionary<Tuple<int, int>, int> subProblems)
        {
            // Base Case - There are no more elements in this sub-array.
            if (j < i) { return 0; }
            // Base Case - There is only one element in this sub-array.
            if (j == i) { return keys[i].Frequency; }

            var key = Tuple.Create(i, j);

            // Memoization - Have we seen this sub problem before
            if (subProblems.ContainsKey(key))
            {
                return subProblems[key];
            }

            // Every search will go through the root doing one comparison so add
            // the sum of the frequencies between i and j
            var subArraySum = 0;
            for (var k = i; k <= j; k++)
            {
                subArraySum += keys[k].Frequency;
            }

            // Consider all possible elements of the sub-array as the root 
            // and find the min cost
            var minCost = Int32.MaxValue;
            for (var root = i; root <= j; ++root)
            {
                int proposedCost = getOptimalCost(keys, i, root - 1, subProblems) + getOptimalCost(keys, root + 1, j, subProblems);
                if (proposedCost < minCost) { minCost = proposedCost; }
            }

            // Return the minimum possible value
            var solution = subArraySum + minCost; 

            // Store the solved sub-problem
            subProblems.Add(key, solution);

            return solution;
        }

        static void Main(string[] args)
        {
            var keys = new List<Key>
            {
                new Key(1, 20),
                new Key(2, 5),
                new Key(3, 17),
                new Key(4, 10),
                new Key(5, 20),
                new Key(6, 3),
                new Key(7, 25)
            };

            var solution = CalculateMinCostBinarySearchTree(keys);

            Console.WriteLine("\nOptimal solution: " + solution.ToString());

            Console.WriteLine("\n[Press any key to exit]");
            Console.ReadKey();
        }
    }
}
