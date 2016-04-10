using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// Exploring using the distribution of unique/common terms to classify different kinds of documents.
// Sample data is from the listings file from the Sortable code challenge.  I am interesting in ways
// to classify a listing a product or accessory for a post match pruning stage.
//
// Assuming:
// 1) A "typical" camera listing has one model number (fairly unique) and many non unique terms.
// 2) Accessories that can be used for multiple models have many unique terms.
// It should be possible to decide between a product and a product accessory by examining the distribution of term probabilities.
//
// Observations:
//
// 1) Had use a non linear scale (I used powers of 2) for the histogram buckets otherwise all the unique (small probability) words ended up in the same bucket.
//
// 2) It does seem to be possible to identify accessory listings that have a high ratio of unique terms.
//
//  [8] 310 digital camera video mask now rated to 65 feet
//  0 | 2 | 0 | 0 | 0 | 0 | 1 | 0 | 1 | 2 | 0 | 0 | 1 | 0 | 3 |
//  [119] optekas extreme travelers essentials kit by opteka package inlcudes excursion series c900 fullsize waterproof canvas bag 6501300mm and 500mm telephoto lenses heavy duty tripod and monopod and much more for pentax k10d k20d k100d k110d k200d ist digital slr cameras
//  0 | 1 | 0 | 5 | 1 | 2 | 5 | 1 | 4 | 1 | 9 | 3 | 5 | 3 | 0 |
//  [434] xvision ssl150 camera floodlight 150w
//  0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 1 | 2 |
//  [135] casecrown camo rugged canvas messenger bag for the canon digital rebel xsi 122 mp digital slr camera
//  0 | 3 | 1 | 2 | 0 | 1 | 2 | 1 | 2 | 0 | 0 | 1 | 1 | 2 | 1 |
//
// 3) High rate of false positives when listings are in other languages.  There are not many listings in other languages so every word in these listings gets marked as unique.
//  It may be possible to figure out what language the listings are by using identifying listings which have unusual character n-grams distributions but I don't
//  think there will be enough text per listing to do this reliably.
//
//  [578] jendigital jd 5200 z3 digitalkamera 50 2560 x 1920 32mb
//  0 | 0 | 0 | 1 | 1 | 0 | 1 | 0 | 0 | 2 | 4 | 0 | 1 | 0 | 0 |
//  [837] samsung nx100 noir optique ifn 2050mm flash ng15
//  0 | 0 | 0 | 0 | 2 | 1 | 1 | 0 | 0 | 0 | 1 | 1 | 2 | 0 | 0 |

namespace ClassifyingDocumentsUsingDistributionOfTermUniqueness
{
    public class ScoredListing
    {
        public string Listing { get; set; }
        public List<double> Probablities { get; set; }
    }

    public class Experiment
    {
        public static IEnumerable<string> Ingest(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                using (var reader = new StreamReader(file))
                {
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();

                        // Parse listing from file
                        // "title\":\"LED Flash Macro Ring Light (48 X LED) with 6 Adapter Rings for For Canon/Sony/Nikon/Sigma Lenses\",\"manufacturer\"
                        var titleIdx = line.IndexOf(@"title", StringComparison.InvariantCultureIgnoreCase);
                        if (titleIdx < 0)
                            continue;

                        const int TITLE_LEFT_OFFSET = 8;
                        var manufactuerIndex = line.IndexOf(@"manufacturer", titleIdx + TITLE_LEFT_OFFSET, StringComparison.InvariantCultureIgnoreCase);
                        if (manufactuerIndex < 0)
                            continue;

                        const int TITLE_RIGHT_OFFSET = -13;
                        var listing = line.Substring(titleIdx + TITLE_LEFT_OFFSET, manufactuerIndex + TITLE_RIGHT_OFFSET);
                        yield return listing;
                    }
                }
            }
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
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    result.Add(char.ToLower(c));
                    lastCharWasSpace = false;
                }
                else if ((char.IsWhiteSpace(c) || c == '/') && !lastCharWasSpace)
                {
                    // Also takes care of \r\n
                    result.Add(' ');
                    lastCharWasSpace = true;
                }
            }
            return new string(result.ToArray());
        }

        private static int NUM_BUCKETS = 15;

        public static IList<double> GenerateHistogramAxis()
        {
            var histogram = Enumerable.Repeat(0D, NUM_BUCKETS).ToList();

            // Most of the term probabilities are very small so we use buckets with increasingly smaller range.
            var prob = 1D; // 100%
            for (var i = 0; i < histogram.Count; i++)
            {
                // Each bucket is 1/2 range of previous.
                prob /= 2; // 0.5, 0.25, 0.0125, ...
                histogram[i] = prob;
            }
            return histogram;
        }

        public static IList<int> GenerateHistogramBuckets()
        {
            return Enumerable.Repeat(0, NUM_BUCKETS).ToList(); // Make some buckets
        }

        public static void Run()
        {
            var listings = Ingest("listings.txt").Take(99999).ToList();

            var munged = listings.Select(Munge);
// Note: This is exploratory code so it isn't the greatest.

            // Get term frequency over all listings
            var freqByToken = new Dictionary<string, int>();
            var totalTokens = 0;
            foreach (var token in munged.SelectMany(x => x.Split(null)))
            {
                if (!freqByToken.ContainsKey(token))
                    freqByToken.Add(token, 0);
                freqByToken[token]++;
                totalTokens++;
            }

            DisplaySomeCommonWords(freqByToken);

            DisplaySomeUniqueWords(freqByToken);

            // Assign a probability for each term in the listings.
            var scored = new List<ScoredListing>();
            var maxProbability = 0D;
            foreach (var listing in munged)
            {
                var probablities = new List<double>();
                foreach (var token in listing.Split(null))
                {
                    probablities.Add((double)freqByToken[token] / totalTokens);
                }
                if (probablities.Max() > maxProbability) { maxProbability = probablities.Max(); }
                scored.Add(new ScoredListing { Listing = listing, Probablities = probablities });
            }

            var axisValues = GenerateHistogramAxis();
            DisplayHistogramAxis(axisValues);

            int count = 0;
            foreach (var listing in scored)
            {
                // Assign this listing's term probabilities to a histogram bucket
                var histogram = GenerateHistogramBuckets();
                Debug.Assert(histogram.Count == axisValues.Count);

                for(var i = 0; i < listing.Probablities.Count; i++)
                {
                    // We are normalizing against all other docs.  This may not be a good idea.
                    listing.Probablities[i] = listing.Probablities[i] / maxProbability;

                    // Try each bucket until probability fits in range
                    for (var j = 0; j < histogram.Count; j++)
                    {
                        if (axisValues[j] <= listing.Probablities[i])
                        {
                            histogram[(int)j]++;
                            break;
                        }
                    }
                }

                // These are just made up numbers to try classifying some listings
                var numTerms = histogram.Sum();
                var numUnique = histogram.Skip(10).Sum();
                var ratioUnique = (double)numUnique / numTerms;

                var numCommon = histogram.Take(5).Sum();
                var ratioCommon = (double)numCommon / numTerms;

                if (numUnique > 3 && ratioCommon > 0.05D && ratioUnique > 0.25D)
                {
                    // Show possible accessory listings.
                    Console.WriteLine("[{0}] {1}", count++, listing.Listing);
                    Console.WriteLine(String.Concat(histogram.Select(x => x.ToString() + " | ")));
                }
            }
        }

        private static void DisplayHistogramAxis(IList<double> axisValues)
        {
            Console.WriteLine("------------------------------- Axis");
            Console.WriteLine(String.Concat(axisValues.Select(x => x.ToString() + " | ")));
            Console.WriteLine("-------------------------------");
        }

        private static void DisplaySomeUniqueWords(Dictionary<string, int> freqByToken)
        {
            Console.WriteLine("------------------------------- Examples of unique words");
            var unique = freqByToken.OrderBy(x => x.Value).Take(15).Select(x => x.Key).ToList();
            foreach (var x in unique) { Console.WriteLine(x); }
            Console.WriteLine("-------------------------------");
        }

        private static void DisplaySomeCommonWords(Dictionary<string, int> freqByToken)
        {
            Console.WriteLine("------------------------------- Examples of common words");
            var common = freqByToken.OrderByDescending(x => x.Value).Take(15).Select(x => x.Key).ToList();
            foreach (var x in common) { Console.WriteLine(x); }
            Console.WriteLine("-------------------------------");
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