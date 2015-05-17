using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Given a knapsack that can hold a fixed amount of weight and 
// a set of items with different values and weights determine
// the subset of items that can be can be put in the knapsack
// that would give the maxim value. 
//
// This is an iterative solution that uses an 2d array to store
// previously solved sub-problems.
//
// Time Complexity: O(mn)
// Space Complexity: O(mn)
// (where m is # items and n is # of possible weights)
//
// The knapsack and items are stored in the following file format:
// [knapsack_size][number_of_items]
// [value_1] [weight_1]
// [value_2] [weight_2]

namespace KnapsackProblemIterativeWithArray
{
    public class Item 
    {
        public int Value { get; private set; }
        public int Weight { get; private set; }

        public Item(int value, int weight)
        {
            this.Value = value;
            this.Weight = weight;
        }
    }

    public class Knapsack
    {
        private readonly int _maxWeight;
        private readonly IReadOnlyList<Item> _items;

        public Knapsack(IReadOnlyList<Item> items, int maxWeight)
        {
            this._items = items;
            this._maxWeight = maxWeight;
        }

        public int GetMaxValue()
        {
            // Sub problems stored as [weight, item]
            var subProblems = new int[_maxWeight + 1, _items.Count];

            // Initialize column 0 where zero items are allowed in the knapsack as zero value.
            for (var weight = 0; weight <= _maxWeight; weight++)
            {
                subProblems[weight, 0] = 0;
            }

            // Initialize row 0 where only zero weight is allowed as zero values;
            for (var itemIdx = 0; itemIdx < _items.Count; itemIdx++)
            {
                subProblems[0, itemIdx] = 0;
            }

            for (var itemIdx = 1; itemIdx < _items.Count; itemIdx++)
            {
                for (var weightLimit = 1; weightLimit <= _maxWeight; weightLimit++)
                {
                    // For the current weight limit decide if the overall value can be increased
                    // if we include the current item or not.

                    var valueIfExcludeCurrentItem = subProblems[weightLimit, itemIdx - 1];

                    var valueIfIncludeCurrentItem = 0;
                    var currentItem = _items[itemIdx];
                    var residualWeight = weightLimit - currentItem.Weight;

                    // We can only include the current item if:
                    // 1) The residual weight after adding it is positive.
                    // 2) The residual weight after adding it is less then the maximum knapsack weight.
                    if (residualWeight >= 0 && residualWeight <= _maxWeight)
                    {
                        valueIfIncludeCurrentItem = (residualWeight >= 0) ? subProblems[residualWeight, itemIdx - 1] + currentItem.Value : 0;
                    }

                    subProblems[weightLimit, itemIdx] = Math.Max(valueIfExcludeCurrentItem, valueIfIncludeCurrentItem);
                }
            }

            return subProblems[_maxWeight, _items.Count - 1];
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

            return Tuple.Create(knapsackSize, (IReadOnlyList<Item>)items);
        }

        private static string ReadFile()
        {
            var data = string.Empty;
            try
            {
                using (var sr = new StreamReader("../../../knapsack1.txt"))
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