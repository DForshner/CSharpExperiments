using System;
using System.Collections.Generic;
using System.Linq;

// A Ternary Search Tree is a type of prefix tree that can be used as an associated map for incremental
// string search.
//
// TODO: Get suggestions based on prefix
// TODO: Combine with unit tests
//
// Compiled: C# Visual Studio 2013

namespace TernarySearchTree 
{
    public class Node
    {
        public char Value { get; private set; }
        public bool IsEnd { get; set;}
        public Node Left { get; set; }
        public Node Middle { get; set; }
        public Node Right { get; set; }

        public Node(char value)
        {
            this.Value = value;
        }
    }

    public class TernarySearchTree
    {
        private Node root;
 
        public bool Any()
        {
            return (root == null);
        }

        public void Clear()
        {
            root = null;
        }

        public void Insert(string word)
        {
            root = Insert(root, word.ToCharArray(), 0);
        }

        public Node Insert(Node r, char[] word, int ptr)
        {
            if (r == null) // Char is new
                r = new Node(word[ptr]);

            if (word[ptr] < r.Value) // Char is less than current go left.
            {
                r.Left = Insert(r.Left, word, ptr);
            }
            else if (word[ptr] > r.Value) // Char is greater than current go right.
            {
                r.Right = Insert(r.Right, word, ptr);
            }
            else // Char is new or equal to current node.
            {
                if (CharsLeftInWord(word, ptr))
                    r.Middle = Insert(r.Middle, word, ptr + 1);
                else
                    r.IsEnd = true; // A word ends on this node.
            }
            return r;
        }

        private static bool CharsLeftInWord(char[] word, int ptr)
        {
            return ptr + 1 < word.Length;
        }

        public void Delete(String word)
        {
            Delete(root, word.ToCharArray(), 0);
        }

        private void Delete(Node r, char[] word, int ptr)
        {
            if (r == null) 
                return;

            if (word[ptr] < r.Value)
            {
                Delete(r.Left, word, ptr);
            }
            else if (word[ptr] > r.Value)
            {
                Delete(r.Right, word, ptr);
            }
            else // Char equals current
            {
                if (r.IsEnd && LastCharOfWord(word, ptr))
                    r.IsEnd = false; // Delete the word that ends here
                else if (CharsLeftInWord(word, ptr))
                    Delete(r.Middle, word, ptr + 1);
            }
        }

        public bool Search(String word)
        {
            return Search(root, word.ToCharArray(), 0);
        }

        private bool Search(Node r, char[] word, int ptr)
        {
            if (r == null)
                return false;

            if (word[ptr] < r.Value)
            {
                return Search(r.Left, word, ptr);
            }
            else if (word[ptr] > r.Value)
            {
                return Search(r.Right, word, ptr);
            }
            else // Char equals current
            {
                if (r.IsEnd && LastCharOfWord(word, ptr))
                    return true;
                else if (LastCharOfWord(word, ptr))
                    return false;
                else
                    return Search(r.Middle, word, ptr + 1);
            }
        }

        private static bool LastCharOfWord(char[] word, int ptr)
        {
            return ptr == word.Length - 1;
        }

        public IEnumerable<String> GetWords()
        {
            var wordsInTree = new List<string>();
            Traverse(wordsInTree, root, "");
            return wordsInTree;
        }

        public override String ToString()
        {
            var words = GetWords();
            return (words.Any()) ? words.Aggregate((i, j) => i + j) : String.Empty; 
        }

        public IEnumerable<String> Suggest(String prefix)
        {
            var wordsInTree = new List<string>();
            Traverse(wordsInTree, root, prefix);
            return wordsInTree;
        }

        public void Traverse(IList<String> wordsInTree, Node r, String s)
        {
            if (r == null)
                return;

            Traverse(wordsInTree, r.Left, s);

            s = s + r.Value;
            if (r.IsEnd)
                wordsInTree.Add(s);

            Traverse(wordsInTree, r.Middle, s);
            
            Traverse(wordsInTree, r.Right, s.Substring(0, s.Length - 1));
        }

        public IEnumerable<Tuple<int, char>> GetCharsInLevelOrder()
        {
            var level = 0;
            var chars = new Queue<Node>();
            chars.Enqueue(root);
            var count = chars.Count;

            while (chars.Any())
            {
                var curr = chars.Dequeue();
                yield return new Tuple<int, char>(level, curr.Value);

                if (curr.Left != null)
                    chars.Enqueue(curr.Left);
                if (curr.Middle != null)
                    chars.Enqueue(curr.Middle);
                if (curr.Right!= null)
                    chars.Enqueue(curr.Right);

                if (--count == 0) // Current level complete
                {
                    level++;
                    count = chars.Count; 
                }
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var tree = new TernarySearchTree();

            Console.WriteLine("1) Inserting words.");
            tree.Insert("Cat");
            tree.Insert("Cats");
            tree.Insert("CaCa");
            tree.Insert("Cannot");

            Console.WriteLine("\n ---------- Level Order ---------- \n");
            var level = -1;
            foreach (var c in tree.GetCharsInLevelOrder())
            {
                if (c.Item1 != level)
                {
                    Console.Write("\n");
                    level = c.Item1;
                }

                Console.Write(c.Item2 + " ");
            }
            Console.WriteLine("\n ------------------------------- \n");

            Console.WriteLine("Tree: " + tree.ToString());
            Console.WriteLine(tree.Search("Cats"));
            Console.WriteLine(tree.Search("Cabbit"));
            Console.WriteLine(tree.Search("Cannot"));

            Console.WriteLine("\n ---------- Suggest ---------- \n");
            foreach (var word in tree.Suggest("Can"))
            {
                Console.Write(word + ",");
            }
            Console.WriteLine("\n ------------------------------- \n");

            Console.WriteLine("2) Deleting Cat");
            tree.Delete("Cat");
            Console.WriteLine(tree.Search("Cat"));
            Console.WriteLine("Tree: " + tree.ToString());

            Console.WriteLine("3) Clearing");
            tree.Clear();
            Console.WriteLine(tree.Any());
            Console.WriteLine(tree.Search("Cats"));
            Console.WriteLine("Tree: " + tree.ToString());

            Console.WriteLine("Press [Enter Key] to exit.");
            Console.ReadLine();
        }
    }
}