using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Trying to reproduce Shannon's entropy measurements for different length n-grams in the English language.
// Original Paper: http://languagelog.ldc.upenn.edu/myl/Shannon1950.pdf
// I used War and Peace from Project Guttenberg as my corpus but you will want to use something derived from multiple sources.

// Update: I found why I'm not getting the same results as the original paper. I'm calculating the isolated-symbols entropy which
// is not the same as Shannon's entropy estimate for English which uses conditional n-gram probabilities.
// See: https://stackoverflow.com/questions/9604460/how-to-find-out-the-entropy-of-the-english-language
// See: https://stackoverflow.com/questions/9564979/what-is-the-meaning-of-isolated-symbol-probabilities-of-english?rq=1
// I am getting a similar result to Rosetta Code: https://rosettacode.org/wiki/Entropy#C.23

// Output:
//-------------------------------- sanity check
// Average bits per char of "122333444": 1.89106111753319 ~= 1.84643934467102
//---------------------------------------------
//-------------------------------- unigram probability
// [token]                                Probability
// [ ]                            0.1826038
// [e]                            0.1017603
// [t]                            0.07308632
// [a]                            0.06643674
// [o]                            0.06226344
// [n]                            0.05945337
// [i]                            0.05626013
// [h]                            0.05403983
// [s]                            0.05258396
// [r]                            0.04791417
//---------------------------------------------
//-------------------------------- bigram probability
// [token]                                Probability
// [e ]                           0.03586495
// [ t]                           0.02825758
// [d ]                           0.02448941
// [he]                           0.02429185
// [th]                           0.02381377
// [ a]                           0.02245183
// [s ]                           0.02028996
// [t ]                           0.01880665
// [ h]                           0.01607567
// [in]                           0.01563341
//---------------------------------------------
//-------------------------------- H(x) Entropy
// n-gram length                          Entropy(avg. bits per symbol)
// 1                              4.09163647350399
// 2                              7.4179061902989
// 3                              10.0906402928607
// 4                              12.1810660904339
// 5                              13.9064960749554
// 6                              15.4021320618799
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
        /// Find probability of n-grams of length in the given text.
        /// </summary>
        public static IDictionary<string, float> FindProbabilityByNGram(int nGramLength, string txt)
        {
            var nGrams = GenerateNGrams(nGramLength, txt);
            var frequencyByNGram = CountTokenFrequency(nGrams);

            var normalizer = frequencyByNGram.Values.Sum();

            // Pn = freq / n
            return frequencyByNGram.ToDictionary(x => x.Key, x => (float)x.Value / normalizer);
        }

        /// <summary>
        /// Find entropy H(x) for all symbols
        /// See https:// en.wikipedia.org/wiki/Entropy_(information_theory)
        /// </summary>
        private static double FindEntropy(IDictionary<string, float> probabilityByNGram)
        {
            return -probabilityByNGram.Values
                .Select(x => x*Math.Log(x, 2)) // Average length of bits of entropy for symbol
                .Sum();
        }

        /// <summary>
        /// Sanity check against Rosetta Code
        /// https://rosettacode.org/wiki/Entropy#C.23
        /// </summary>
        private static void SanityCheck()
        {
            var sample = "1223334444";
            var testProb = FindProbabilityByNGram(1, sample);
            var testEntropy = FindEntropy(testProb);
            Console.WriteLine(@" Average bits per char of ""122333444"": {0} ~= 1.84643934467102", testEntropy);
        }

        private static void DisplayTop10Probability(IDictionary<string, float> probabilityByNGram)
        {
            Console.WriteLine(" [token]\t\t\t\tProbability");
            foreach (var result in probabilityByNGram.OrderByDescending(x => x.Value).Take(10))
            {
                Console.WriteLine(" [{0}]\t\t\t\t{1}", result.Key, result.Value);
            }
        }

        private static void DisplayEntropy(IDictionary<int, double> entropyByNGramLength)
        {
            Console.WriteLine(" n-gram length\t\t\t\tEntropy(avg. bits per symbol)");
            foreach (var result in entropyByNGramLength)
            {
                Console.WriteLine(" {0}\t\t\t\t{1}", result.Key, result.Value);
            }
        }

        public static void Run()
        {
            Console.WriteLine("-------------------------------- sanity check");
            SanityCheck();
            Console.WriteLine("---------------------------------------------");

            var original = Ingest("WarAndPeace.txt");

            var munged = Munge(original);

            var probabilityByNGram = Enumerable.Range(1, 6)
                .ToDictionary(x => x, x => FindProbabilityByNGram(x, munged));

            Console.WriteLine("-------------------------------- unigram probability");
            DisplayTop10Probability(probabilityByNGram[1]);
            Console.WriteLine("---------------------------------------------");

            Console.WriteLine("-------------------------------- bigram probability");
            DisplayTop10Probability(probabilityByNGram[2]);
            Console.WriteLine("---------------------------------------------");

            var entropyByNGramLength = probabilityByNGram
                .ToDictionary(x => x.Key, x => FindEntropy(x.Value));
            Console.WriteLine("-------------------------------- H(x) Entropy");
            DisplayEntropy(entropyByNGramLength);
            Console.WriteLine("---------------------------------------------");
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Experiment.Run();
        }
    }
}
