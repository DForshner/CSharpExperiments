using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Given a knapsack that can hold a fixed amount of weight and 
// a set of items with different values and weights determine
// the subset of items that can be can be put in the knapsack
// that would give the maxim value. 
//
// This is a recursive solution that uses a dictionary to memoize
// previously solved sub-problems.
//
// The knapsack and items are stored in the following file format:
// [knapsack_size][number_of_items]
// [value_1] [weight_1]
// [value_2] [weight_2]

namespace FindMaxValueKnapsackProblemRecursiveDictonary
{
    public struct Item 
    {
        public readonly int Value;
        public readonly int Weight;

        public Item(int value, int weight)
        {
            this.Value = value;
            this.Weight = weight;
        }
    }

    public class Knapsack
    {
        private readonly int MaxWeight;

        private IReadOnlyList<Item> items;

        public Knapsack(IReadOnlyList<Item> items, int maxWeight)
        {
            this.items = items;
            this.MaxWeight = maxWeight;
        }

        public int GetMaxValue()
        {
            // Sub problems stored with key <weight, item>
            var subProblems = new Dictionary<Tuple<int, int>, int>();

            // Initialize scenarios where no items are put in knapsack as zero value.
            for (var weight = 0; weight <= MaxWeight; weight++)
            {
                subProblems.Add(Tuple.Create(weight, 0), 0);
            }

            // Initialize scenarios where no weight is available as zero values.
            for (var item = 1; item < items.Count; item++)
            {
                subProblems.Add(Tuple.Create(0, item), 0);
            }

            return ProcessSubProblem(items.Count - 1, MaxWeight, subProblems);
        }

        private int ProcessSubProblem(int itemIdx, int weight, IDictionary<Tuple<int, int>, int> subProblems)
        {
            var key = Tuple.Create(weight, itemIdx);

            // Have we already solved this problem?
            if (subProblems.ContainsKey(key))
            {
                return subProblems[key];
            }

            Debug.Assert(itemIdx > 0 && itemIdx < items.Count, "Expected real item index");

            var valueIfExcludeCurrentItem = ProcessSubProblem(itemIdx - 1, weight, subProblems);

            var valueIfIncludeCurrentItem = 0;
            var currentItem = items[itemIdx];
            var residualWeight = weight - currentItem.Weight;

            // We can only include the current item if:
            // 1) The residual weight after taking it is positive (It fits in the current weight allowance).
            // 2) The residual weight after taking it is less then the maximum knapsack weight (It fits in the knapsack).
            if (residualWeight >= 0 && residualWeight <= MaxWeight)
            {
                valueIfIncludeCurrentItem = ProcessSubProblem(itemIdx - 1, residualWeight, subProblems) + currentItem.Value;
            }

            var solution = Math.Max(valueIfExcludeCurrentItem, valueIfIncludeCurrentItem);

            // Cache this solved sub-problem
            subProblems.Add(key, solution);

            return solution;
        }
    }

    public class Program 
    {
        static void Main(string[] args)
        {
            var inputs = ParseGraphFromFile(ReadFile());

            var knapsack = new Knapsack(inputs.Item2, inputs.Item1);

            var optimalSolution = knapsack.GetMaxValue();
            Console.WriteLine("\n\nOptimal Solution: " + optimalSolution);

            Console.WriteLine("\n[Press any key to exit]");
            Console.ReadKey();
        }

        private static Tuple<int, IReadOnlyList<Item>> ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of nodes and edges 
            var header = lines.First().Split(' ');
            var knapsackSize = Int32.Parse(header[0]);
            var numItems = Int32.Parse(header[1]);

            var itemStrings = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var items = new List<Item>(numItems);

            // for simplicity make the zero-ith item a zero value item so 
            // valid item indexes will range from 1 to numItems
            items.Add(new Item(0, knapsackSize + 1));

            foreach (var edge in itemStrings)
            {
                var details = edge.Data.Split(' ');
                var value = Int32.Parse(details[0]);
                var weight = Int32.Parse(details[1]);

                items.Add(new Item(value, weight));
            }

            if (numItems != items.Count - 1)
            {
                throw new Exception("The number of items read from file didn't match number of items specified in file header.");
            }

            return Tuple.Create(knapsackSize, (IReadOnlyList<Item>)items.AsReadOnly());
        }

        private static string ReadFile()
        {
            var data = string.Empty;
            try
            {
                using (var sr = new StreamReader("../../../knapsack_big.txt"))
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