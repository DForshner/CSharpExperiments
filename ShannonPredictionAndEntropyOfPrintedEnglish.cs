using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Trying to reproduce Shannon's entropy measurements for different length n-grams in the English language.
// Original Paper: http://languagelog.ldc.upenn.edu/myl/Shannon1950.pdf

// Results:
//---------------------------------------------
// n-gram Length      H(x) Entropy
// 1               4.09163647350399
// 2               7.4179061902989
// 3               10.0906402928607
// 4               12.1810660904339
//---------------------------------------------

namespace ShannonPredictionAndEntropyOfPrintedEnglish
{
    public class Experiment
    {
        private static string Ingest(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    while (reader.Peek() >= 0)
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Remove punctuation, digits, signs, double spaces, line end and lowercase everything
        /// </summary>
        private static string Munge(string original)
        {
            var temp = original.ToCharArray();
            var result = new List<char>(original.Length);
            bool lastCharWasSpace = false;
            foreach (var c in temp)
            {
                if (char.IsLetter(c))
                {
                    result.Add(char.ToLower(c));
                    lastCharWasSpace = false;
                }
                else if (char.IsWhiteSpace(c) && !lastCharWasSpace)
                {
                    // Also takes care of \r\n
                    result.Add(' ');
                    lastCharWasSpace = true;
                }
            }
            return new string(result.ToArray());
        }

        private static IEnumerable<string> GenerateNGrams(int nGramLength, string txt)
        {
            for (var i = 0; i < txt.Length - nGramLength; i++)
            {
                yield return txt.Substring(i, nGramLength);
            }
        }

        private static IDictionary<string, int> CountTokenFrequency(IEnumerable<string> tokens)
        {
            var result = new Dictionary<string, int>();
            foreach (var token in tokens)
            {
                if (!result.ContainsKey(token)) { result.Add(token, 0); }
                result[token]++;
            }
            return result;
        }

        /// <summary>
        /// Find bits of entropy per n-gram length for a given txt
        /// </summary>
        public static double FindEntropy(int nGramLength, string txt)
        {
            var nGrams = GenerateNGrams(nGramLength, txt);
            var frequencyByNGram = CountTokenFrequency(nGrams);

            var normalizer = frequencyByNGram.Values.Sum();

            // Pn = freq / n
            var probabilityByNGram = frequencyByNGram.ToDictionary(x => x.Key, x => (float)x.Value / normalizer);

            // Find entropy H(x)
            // See https:// en.wikipedia.org/wiki/Entropy_(information_theory)
            var entropy = -probabilityByNGram.Values
                .Select(x => x * Math.Log(x, 2))
                .Sum();

            return entropy;
        }

        public static IDictionary<int, double> FindEntropyByNGramLengthFromText()
        {
            var original = Ingest("WarAndPeace.txt");
            var munged = Munge(original);

            var entropyByNGramLength = Enumerable.Range(1, 4)
                .ToDictionary(x => x, x => FindEntropy(x, munged));

            return entropyByNGramLength;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine(" n-gram Length\t\tH(n) entropy");

            var entropy = Experiment.FindEntropyByNGramLengthFromText();
            foreach (var result in entropy)
            {
                Console.WriteLine(" {0}\t\t{1}", result.Key, result.Value);
            }

            Console.WriteLine("---------------------------------------------");
        }
    }
}
