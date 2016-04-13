using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

// Attempted to use document similarity to find entity aliases.  This code is attempting to use the MinHash technique to find similar documents
// than can be used to generate a list of aliases for the same entity.
//
// The problem I'm trying to solve is to link together multiple manufacturer names that are actually the same manufacturer
// Examples:
// {"title":"Fujifilm FinePix JV100 12 MP Digital Camera with 3x Optical Zoom and 2.7-Inch LCD (Black)",                            "manufacturer":"Fujifilm Canada"
// {"title":"Fujifilm Finepix S1800 12 Mega-Pixel, 18x Long Zoom Digital Camera",                                                   "manufacturer":"Fujifilm CA"
// {"title":"Fujifilm FinePix XP10 12 MP Waterproof Digital Camera with 5x Optical Zoom and 2.7-Inch LCD (Black)",                  "manufacturer":"FUJIFILM"
// {"title":"Fujifilm Finepix Z700EXR Digitalkamera (12 Megapixel, 5-fach opt.Zoom, 8,9 cm Display, Bildstabilisator) silber",      "manufacturer":"Fujifilm Imaging Systems"
// {"title":"Fujifilm FINEPIX Z90 Digitalkamera (14 Megapixel, 5-fach opt. Zoom, 7,6 cm (3 Zoll) Display) silber",                  "manufacturer":"FUJIFILM Electronic Imaging Europe GmbH - Firstorder"
// {"title":"Fujifilm FINEPIX JX280 Digitalkamera (14 Megapixel, 5-fach opt. Zoom, 6,9 cm (2,7 Zoll) Display) schwarz",             "manufacturer":"Fuji Photo Film Europe GmbH"
//
// Assuming:
// - Each entity has many documents associated with it.
// - Each entity may have multiple aliases.
//
// When documents are similar and they have different entities (manufacturer name) associated with them then it's likely those entities are actually the same entity.
//
// This approach didn't work for this problem.  I'm getting too many false negatives of the Fuji listings for this to be useful.
// - I don't think there's enough text per listing to get a good grouping of similar documents.
// - Using only unique tokens to generate the min hash helped.

namespace FindingEntityAliasesUsingMinHashApproximateDocumentSimilarity
{
    public class Listing
    {
        public string Text { get; set; }
        public string Manufacturer { get; set; }
    }

    public class HashedListing
    {
        public string Text { get; set; }
        public string Manufacturer { get; set; }
        public IList<int> MinHashes { get; set; }
    }

    public class Experiment
    {
        private static int NUM_HASHES = 100;

        private static IEnumerable<Listing> Ingest(string fileName)
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

                        var currencyFieldIdx = line.IndexOf(@"currency", manufactuerIndex, StringComparison.InvariantCultureIgnoreCase);

                        const int MANUFACURER_RIGHT_OFFSET = 15;
                        const int CURRENCY_LEFT_OFFSET = -18;
                        var manufacturer = line.Substring(manufactuerIndex + MANUFACURER_RIGHT_OFFSET, currencyFieldIdx - manufactuerIndex + CURRENCY_LEFT_OFFSET);

                        yield return new Listing { Text = listing, Manufacturer = manufacturer };
                    }
                }
            }
        }

        /// <summary>
        /// Remove punctuation, digits, signs, double spaces, line end and lowercase everything
        /// </summary>
        private static Listing Munge(Listing original)
        {
            var temp = original.Text.ToCharArray();
            var result = new List<char>(original.Text.Length);
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
            return new Listing { Text = new String(result.ToArray()), Manufacturer = original.Manufacturer };
        }

        private static IEnumerable<string> CreateUnigramsAndBigrams(string[] tokens)
        {
            for (var i = 0; i < tokens.Length - 1; i++)
            {
                yield return tokens[i];
                yield return tokens[i] + tokens[i + 1];
            }
        }

        public static void Run()
        {
            var listings = Ingest("listings.txt").Take(20000).ToList();

            var minHashMasks = GenerateMinHashXORMasks();

            var mungedListings = listings
                .AsParallel()
                .Select(Munge);

            var uniqueTokens = GenerateP90UniqueTokens(mungedListings);

            var hashedListings = mungedListings
                .AsParallel()
                .Select(x => MinHashListing(x, minHashMasks, uniqueTokens))
                .ToList();

            hashedListings
                .AsParallel()
                .ForAll(outerListing =>
                {
                    foreach (var innerListing in hashedListings)
                    {
                        if (outerListing == innerListing) { continue; } // Don't compare with self
                        if (outerListing.Manufacturer == innerListing.Manufacturer) { continue; } // Already same entity

                        var approxSimilarity = ScoreApproximateSimilarity(outerListing, innerListing);
                        if (approxSimilarity > 0.95

                            // I really only care about Fuji listings for now
                            && innerListing.Text.IndexOf("Fu", StringComparison.InvariantCultureIgnoreCase) > 0)
                        {
                            Display(outerListing, innerListing, approxSimilarity);
                        }
                    }
                });
        }

        private static HashSet<string> GenerateP90UniqueTokens(ParallelQuery<Listing> mungedListings)
        {
            var freqByToken = GenerateTokenFrequencyMap(mungedListings);

            var p90Idx = (int)((float)freqByToken.Count * 0.9F); // Close enough to p90 index
            var p90Freqency = freqByToken.Values.OrderBy(x => x).ElementAt(p90Idx); // Use QuickSelect if lots of values
            var uniqueTokens = new HashSet<string>();
            freqByToken
                .Where(pair => pair.Value > p90Freqency)
                .ToList()
                .ForEach(pair => uniqueTokens.Add(pair.Key));

            return uniqueTokens;
        }

        private static ConcurrentDictionary<string, int> GenerateTokenFrequencyMap(ParallelQuery<Listing> mungedListings)
        {
            var freqByToken = new ConcurrentDictionary<string, int>();
            var totalTokens = 0L;
            mungedListings
                .AsParallel()
                .ForAll(mungedListing =>
                {
                    foreach (var token in mungedListing.Text.Split(null))
                    {
                        freqByToken.AddOrUpdate(token, 1, (key, val) => { return val + 1; });
                        Interlocked.Increment(ref totalTokens);
                    }
                });

            return freqByToken;
        }

        private static object consoleLock = new Object();
        private static void Display(HashedListing outerListing, HashedListing innerListing, float similarity)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0} ----------------------------------------------------------- [ {1} % ]", Thread.CurrentThread.ManagedThreadId, Math.Round(similarity * 100, 2));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(innerListing.Text);
                Console.WriteLine(outerListing.Text);
            }
        }

        private static float ScoreApproximateSimilarity(HashedListing outerListing, HashedListing innerListing)
        {
            Debug.Assert(outerListing.MinHashes.Count == innerListing.MinHashes.Count, "Expected same number of hashes.");

            var similar = 0;
            for(var i = 0; i < innerListing.MinHashes.Count; i++)
            {
                if (innerListing.MinHashes[i] == outerListing.MinHashes[i])
                {
                    similar++;
                }
            }
            return (float)similar / innerListing.MinHashes.Count;
        }

        /// <summary>
        /// Instead of creating n different hashing algorithms we are just going to XOR
        /// the default hash algorithm with a random number of the same number of bits.
        /// See: https://stackoverflow.com/questions/19701052/how-many-hash-functions-are-required-in-a-minhash-algorithm/19711615#19711615
        /// </summary>
        /// <returns></returns>
        private static List<int> GenerateMinHashXORMasks()
        {
            var rnd = new Random(DateTime.UtcNow.Millisecond);
            return Enumerable.Repeat(0, NUM_HASHES).Select(x => rnd.Next()).ToList();
        }

        private static HashedListing MinHashListing(Listing listing, IList<int> minHashMasks, HashSet<string> uniqueTokens)
        {
            var tokens = listing.Text.Split(null)
                .Where(x => uniqueTokens.Contains(x));

            //var ngrams = CreateUnigramsAndBigrams(tokens);

            var listingMinHashes = new int[minHashMasks.Count];
            for (var hashIdx = 0; hashIdx < minHashMasks.Count; hashIdx++)
            {
                // Find the minimum hash value out of all the n-grams for the current hash function.
                var minHash = Int32.MaxValue;
                foreach (var token in tokens)
                {
                    var hash = token.GetHashCode() ^ minHashMasks[hashIdx];
                    if (hash < minHash) { minHash = hash; }
                }
                listingMinHashes[hashIdx] = minHash;
            }

            return new HashedListing { Text = listing.Text, Manufacturer = listing.Manufacturer, MinHashes = listingMinHashes.ToList() };
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