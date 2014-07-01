using System;
using System.Collections.Generic;

// Sort an array of strings so that anagrams are next to each other.
//
// Compiled: Visual Studio 2013

namespace AnagramSorter
{
    /// <summary>
    /// Implements IComparer which is used to provide additional comparison mechanism.
    /// </summary>
    public class AnagramComparer : IComparer<String>
    {
        public int Compare(String s1, String s2)
        {
            var s1Chars = s1.ToCharArray();
            Array.Sort(s1Chars);
            var s2Chars = s2.ToCharArray();
            Array.Sort(s2Chars);

            return new String(s1Chars).CompareTo(new String(s2Chars));
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var s1 = new List<String>() { "aaa", "abc", "bbb", "cba", "dbc", "bcd", "bca" };

            s1.Sort(new AnagramComparer());

            foreach (var s in s1)
                Console.Write(s + " ");
            Console.WriteLine();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);
        }
    }
}