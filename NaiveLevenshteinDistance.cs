using System;

namespace NaiveLevenshteinDistance
{
    /// <summary>
    /// A naive implementation that calculates the Levenshtein Distance between two strings.
    /// Note: Inefficient because it repeatedly compares the same substrings.
    /// </summary>
    public class NaiveLevenshteinDistanceCalculator
    {
        public int Distance(string str1, string str2)
        {
            return DistanceHelper(str1, str1.Length, str2, str2.Length);
        }

        private static int DistanceHelper(string str1, int length1, string str2, int length2)
        {
            Console.WriteLine(String.Format("Comparing: {0} - {1}\n",
                (length1 > 0) ? str1[length1 - 1] : '\0',
                (length2 > 0) ? str2[length2 - 1] : '\0'));

            // Check base case
            if (length1 == 0 || length2 == 0)
                return 0;

            // Calculate distance of last character
            var cost = (str1[length1 - 1] == str2[length2 - 1]) ? 0 : 1;

            return Math.Min(
                Math.Min((DistanceHelper(str1, length1 - 1, str2, length2) + 1), // Delete last char from str1
                (DistanceHelper(str1, length1, str2, length2 - 1) + 1)), // Delete last char from str2
                (DistanceHelper(str1, length1 - 1, str2, length2 - 1) + cost)); // Delete last char from both
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var calculator = new NaiveLevenshteinDistanceCalculator();

            var a = "go";
            var b = "to";
            var abDist = calculator.Distance(a, b);
            Console.WriteLine("Distance between {0} and {1} is: {2}", a, b, abDist);

            var c = "kitten";
            var d = "sitting";
            var cdDist = calculator.Distance(c, d);
            Console.WriteLine("Distance between {0} and {1} is: {2}", c, d, cdDist);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();
        }
    }
}
