using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Use a sorted array to create a BST with minimal height.
//
// C# Visual Studio 2013

namespace CreateMinimalHeightBSTFromSortedArray
{
    public class Node
    {
        public Node Left { get; set; }
        public Node Right { get; set; }
        public int Value { get; set; }

        public Node(int value)
        {
            this.Value = value;
        }
    }

    public class Tree
    {
        private Node root;

        public Tree(int[] array)
        {
            root = Build(array, 0, array.Count() - 1);
        }

        /// <summary>
        /// Builds a minimal BST from a sorted (in-order) array of integers.
        /// </summary>
        private Node Build(int[] array, int start, int end)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(end < array.Count());

            if (start > end) // Base case
                return null;

            // Find the middle (root)
            int mid = (end + start) / 2;
            var node = new Node(array[mid]);

            node.Left = Build(array, start, mid - 1);
            node.Right = Build(array, mid + 1, end);

            return node;
        }

        /// <summary>
        /// Displays each node in order showing nodes with the same height on the same line.
        /// </summary>
        public void Display()
        {
            int previousLevel = 0;
            int level = 0;
            PrintLvl(level);

            var toVisit = new Queue<Tuple<int, Node>>();
            toVisit.Enqueue(new Tuple<int,Node>(level, root));

            while(toVisit.Any())
            {
                var curr = toVisit.Dequeue();
                level = curr.Item1;

                if (previousLevel != level)
                    PrintLvl(level);
                Console.Write(" (" + curr.Item2.Value + ") ");

                if (curr.Item2.Left != null)
                    toVisit.Enqueue(new Tuple<int, Node>(level + 1, curr.Item2.Left));
                if (curr.Item2.Right != null)
                    toVisit.Enqueue(new Tuple<int, Node>(level + 1, curr.Item2.Right));

                previousLevel = level;
            }

            Console.WriteLine();
        }

        private static void PrintLvl(int level)
        {
            Console.Write("\n Level: (" + level + ") => ");
        }
        
    }

    public static class Program
    {
        public static void Main()
        {
            var array = new int[] { 1, 2, 3, 4, 6, 7, 9, 10, 20, 30, 40, 66, 77, 88, 100 };
            var tree = new Tree(array);
            tree.Display(); 
            Console.WriteLine("Press [Enter Key] to exit.");
            Console.ReadLine();
        }
    }
}