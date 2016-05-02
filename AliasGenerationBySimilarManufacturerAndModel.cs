using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// I have a set of listings with manufacture names that are aliases for for the same manufacturer.
// I'm hoping to use the listings to generate a list of possible aliases for each canonical manufacturer name.

// For each listing, I'm attempting to find a similar canonical product record.
// The criteria I'm using for something being similar are:
// 1. Similar manufacture using character n-gram similarity
// 2. Similar model using shingles made from model parts
// If I find enough instances where a listing is similar to a canonical product,
// I'll consider the listing's manufacturer name to be an alias for the canonical product's manufacturer name.

// Results:
//
// Canonical ==> Canonical [Score]
// ---------------------------------------------------------------------------
// fujifilm ==> fuji [105902.9]
// kodak ==> eastman kodak company [73894.77]
// canon ==> canon canada [65381.81]
// fujifilm ==> fujifilm electronic imaging europe gmbh firstorder [63408.52]
// panasonic ==> panasonic deutschland gmbh [50757.55]
// sony ==> sony uk consumer electronics instock account [40392]
// fujifilm ==> fuji photo film europe gmbh [29654.01]
// fujifilm ==> fujifilm imaging systems [24186.75]
// kodak ==> kodak stock account [20809.09]
// fujifilm ==> fujifilm canada [13161.28]
// canon ==> canon uk ltd [12502.29]
// olympus ==> olympus canada [11968]

// My main goal was to relate listings with a manufacturer of fuji to fujifilm so it looks like this is going to work.
// I had to change the scoring method to use the inverse term probably because some of the model names have common numbers or words *(ex: "zoom")* which caused lots of false positives.

namespace AliasGenerationBySimilarManufacturerAndModel
{
    public class Product
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
    }

    public class Listing
    {
        public string Text { get; set; }
        public string Manufacturer { get; set; }
    }

    public class PossibleAlias
    {
        public string Canonical { get; set; }
        public string Alias { get; set; }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = 23 * hash + Canonical.GetHashCode();
            hash = 23 * hash + Alias.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PossibleAlias;
            if (other == null) { return false; }

            return this.Canonical == other.Canonical
                && this.Alias == other.Alias;
        }
    }

    public static class Cleaner
    {
        /// <summary>
        /// Remove punctuation, multiple spaces in a row, line endings, and lowercase everything
        /// </summary>
        public static string Munge(string original)
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
                else if (!lastCharWasSpace)
                {
                    result.Add(' ');
                    lastCharWasSpace = true;
                }
            }
            return new String(result.ToArray());
        }
    }

    [TestClass]
    public class CleanerTests
    {
        [TestMethod]
        public void WhenAllValid_ExpectNoChange()
        {
            var expected = Cleaner.Munge("1 This is a test 1");
            var result = Cleaner.Munge("1 This is a test 1");
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void ExpectNonAlphaOrNumericCharRemoved()
        {
            var expected = Cleaner.Munge("1 This is a test 1 ");
            var result = Cleaner.Munge("1. This=is-a test 1!!!");
            Assert.AreEqual(expected, result);
        }
    }

    /// <summary>
    /// Loads data from files
    /// </summary>
    public static class Loader
    {
        private static IEnumerable<Listing> IngestListings(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(file))
                {
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();

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

        private static IEnumerable<Product> IngestProducts(string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(file))
                {
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();

                        // "{\"product_name\":\"Sony_Cyber-shot_DSC-W310\",\"manufacturer\":\"Sony\",\"model\":\"DSC-W310\",\"family\":\"Cyber-shot\",\"announced-date\":\"2010-01-06T19:00:00.000-05:00\"}"
                        var manufactuerIndex = line.IndexOf(@"manufacturer", StringComparison.InvariantCultureIgnoreCase);
                        if (manufactuerIndex < 0)
                            continue; // Invalid record

                        const int MANUFACTURER_RIGHT_OFFSET = 15;
                        var modelFieldIdx = line.IndexOf(@"model", manufactuerIndex, StringComparison.InvariantCultureIgnoreCase);
                        const int MODEL_LEFT_OFFSET = -18;
                        var manufacturer = line.Substring(manufactuerIndex + MANUFACTURER_RIGHT_OFFSET, modelFieldIdx - manufactuerIndex + MODEL_LEFT_OFFSET);

                        var familyFieldIdx = line.IndexOf(@"family", manufactuerIndex, StringComparison.InvariantCultureIgnoreCase);
                        if (familyFieldIdx < 0)
                            continue; // Skip

                        const int MODEL_RIGHT_OFFSET = 8;
                        const int Family_LEFT_OFFSET = -11;
                        var model = line.Substring(modelFieldIdx + MODEL_RIGHT_OFFSET, familyFieldIdx - modelFieldIdx + Family_LEFT_OFFSET);

                        yield return new Product { Model = model, Manufacturer = manufacturer };
                    }
                }
            }
        }

        public static Listing Munge(Listing original)
        {
            return new Listing
            {
                Manufacturer = Cleaner.Munge(original.Manufacturer),
                Text = Cleaner.Munge(original.Text),
            };
        }

        public static Product Munge(Product original)
        {
            return new Product
            {
                Manufacturer = Cleaner.Munge(original.Manufacturer),
                Model = Cleaner.Munge(original.Model),
            };
        }

        public static IList<Product> IngestProducts()
        {
            return IngestProducts("products.txt").Select(Munge).ToList();
        }

        public static IList<Listing> IngestListings()
        {
            return IngestListings("listings.txt").Select(Munge).ToList();
        }

    }

    /// <summary>
    /// Create ngram/shingles
    /// </summary>
    public static class NGramBuilder
    {
        /// <summary>
        /// Generates character n-grams
        /// </summary>
        private static IEnumerable<string> CreateBiTriQuadCharacterNGrams(string str)
        {
            for (var i = 0; i < str.Length - 1; i++)
            {
                yield return new string(new char[] { str[i], str[i + 1] });
            }

            for (var i = 0; i < str.Length - 2; i++)
            {
                yield return new string(new char[] { str[i], str[i + 1], str[i + 2] });
            }

            for (var i = 0; i < str.Length - 3; i++)
            {
                yield return new string(new char[] { str[i], str[i + 1], str[i + 2], str[i + 3] });
            }
        }

        /// <summary>
        /// Generates word/token shingles
        /// </summary>
        private static IEnumerable<string> CreateUniBiTokenShingles(string str)
        {
            var tokens = str.Split(null); // null splits based on Unicode Char.IsWhiteSpace

            for (var i = 0; i < tokens.Length - 1; i++)
            {
                yield return tokens[i];
                yield return tokens[i] + tokens[i + 1];
            }

            if (tokens.Length > 0)
            {
                yield return tokens[tokens.Length - 1];
            }
        }

        public static IDictionary<string, List<string>> GenerateModelNameShingles(HashSet<string> commonTokens, IList<Product> products)
        {
            var modelShinglesByModelName = new ConcurrentDictionary<string, List<string>>();
            products
                .AsParallel()
                .ForAll(product =>
                {
                    var modelShingles = CreateUniBiTokenShingles(product.Model)
                        // Throw away products with a model that is too common.  Gets rid of "zoom" model.
                        .Where(x => !commonTokens.Contains(x))
                        .ToList();

                    if (modelShingles.Any())
                    {
                        modelShinglesByModelName.TryAdd(product.Model, modelShingles);
                    }
                });
            return modelShinglesByModelName;
        }

        public static IDictionary<string, List<string>> GenerateManufacturerNameNGrams(IList<Product> products)
        {
            var manuNameNGramsByCanonicalManuName = new ConcurrentDictionary<string, List<string>>();
            products
                .AsParallel()
                .ForAll(product => manuNameNGramsByCanonicalManuName.GetOrAdd(product.Manufacturer, x => CreateBiTriQuadCharacterNGrams(x).ToList()));
            return manuNameNGramsByCanonicalManuName;
        }
    }

    [TestClass]
    public class NGramBuilderTests
    {
        [TestMethod]
        public void ExpectBiTriQuadCharacterNGramsFromManufacturerName()
        {
            const string MANU_NAME = "abcd";
            var products = new[] { new Product { Manufacturer = MANU_NAME } }.ToList();
            var results = NGramBuilder.GenerateManufacturerNameNGrams(products)[MANU_NAME].OrderBy(x => x);

            Assert.IsTrue(results.AsQueryable().SequenceEqual(new[] { "ab", "abc", "abcd", "bc", "bcd", "cd" }.AsQueryable()));
        }

        [TestMethod]
        public void ExpectUniBiShinglesFromModelNames()
        {
            const string MODEL_NAME = "ant bee cat";
            var products = new[] { new Product { Model = MODEL_NAME } }.ToList();
            var common = new HashSet<string>();

            var results = NGramBuilder.GenerateModelNameShingles(common, products);

            Assert.AreEqual(3 + 2, results[MODEL_NAME].Count);
            Assert.IsTrue(results[MODEL_NAME].OrderBy(x => x).AsQueryable().SequenceEqual(new[] { "ant", "antbee", "bee", "beecat", "cat" }.AsQueryable()));
        }

        [TestMethod]
        public void ExpectCommonTokensExcluded()
        {
            var products = new[]
                {
                    new Product { Model = "A" },
                    new Product { Model = "B" },
                }.ToList();
            var common = new HashSet<string> { "A" };

            var results = NGramBuilder.GenerateModelNameShingles(common, products);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("B", results.Keys.Single());
        }
    }

    public static class TokenStatistics
    {
        /// <summary>
        /// Words with occurrence probabilities above this percentile are considered common and shouldn't be used for joins.
        /// </summary>
        const float COMMON_WORD_PERCENTILE = 0.90F;

        public static Dictionary<string, float> GenerateTokenProbabilitiesPerListing(IEnumerable<Listing> mungedListings)
        {
            var freqByToken = new ConcurrentDictionary<string, int>();
            var docCount = 0;
            mungedListings
                .AsParallel()
                .ForAll(mungedListing =>
                {
                    foreach (var token in mungedListing.Text.Split(null))
                    {
                        freqByToken.AddOrUpdate(token, 1, (key, val) => { return val + 1; });
                    }

                    Interlocked.Increment(ref docCount);
                });

            // Turn freq into probability per doc
            return freqByToken.ToDictionary(x => x.Key, x => (float)x.Value / docCount);
        }

        public static HashSet<string> GetCommonTokens(IDictionary<string, float> tokenFrequencies)
        {
            var sortedFreqs = tokenFrequencies.Values.OrderBy(x => x).ToList();
            var percentialCutoff = sortedFreqs[(int)((float)sortedFreqs.Count * COMMON_WORD_PERCENTILE)];
            return new HashSet<string>(tokenFrequencies.Where(x => x.Value > percentialCutoff).Select(x => x.Key));
        }
    }

    [TestClass]
    public class TokenStatisticsTests
    {
        [TestMethod]
        public void ExpectTokenProbablityPerDoc()
        {
            var docs = new[]
            {
                new Listing { Text = "A B C D E F" },
                new Listing { Text = "A B C D E F" },
                new Listing { Text = "A B C D" },
                new Listing { Text = "A B C D" },
                new Listing { Text = "A B" },
                new Listing { Text = "A B" },
                new Listing { Text = "" },
            };
            var result = TokenStatistics.GenerateTokenProbabilitiesPerListing(docs);

            Assert.AreEqual(6F / 7F, result["A"], 0.01D);
            Assert.AreEqual(2F / 7F, result["F"], 0.01D);
            Assert.IsFalse(result.ContainsKey("G"));
        }
    }

    /// <summary>
    /// Runs experiment
    /// </summary>
    public class Experiment
    {
        /// <summary>
        /// Words with scores above this percentile are considered good alias candidates.
        /// </summary>
        const float POSSIBLE_ALIAS_PERCENTILE = 0.50F;

        private static float CompareListingTextToProductModel(IDictionary<string, float> tokenProbablities, IDictionary<string, List<string>> modelShinglesByModelName, string listingText, string productModel)
        {
            List<string> modelShingles;
            if (!modelShinglesByModelName.TryGetValue(productModel, out modelShingles))
            {
                return 0f;
            }

            var textTokens = new HashSet<string>(listingText.Split(null)); // null splits based on Unicode Char.IsWhiteSpace
            var modelScore = 0f;
            foreach (var nGram in modelShingles)
            {
                if (textTokens.Contains(nGram))
                {
                    // Use the token's inverse probability for a score for now
                    // The more unlikely the token is the less likely this is an accidental match.
                    modelScore += tokenProbablities.ContainsKey(nGram) ? 1 / tokenProbablities[nGram] : 0;
                }
            }

            return modelScore;
        }

        /// <summary>
        /// </summary>
        private static int CompareManufacturerNameSimilarity(IDictionary<string, List<string>> manuNameNGramsByCanonicalManuName, string listingManuName, string productManuName)
        {
            List<string> manuNameNGrams;
            if (!manuNameNGramsByCanonicalManuName.TryGetValue(productManuName, out manuNameNGrams))
            {
                return 0;
            }

            var nameHit = 0;
            foreach (var nGram in manuNameNGrams)
            {
                if (listingManuName.Contains(nGram))
                {
                    nameHit++;
                }
            }

            return (100 * nameHit) / manuNameNGrams.Count;
        }

        private static void DisplayLikelyAliases(IDictionary<PossibleAlias, float> possibleAliasesByCanonical)
        {
            var sortedScores = possibleAliasesByCanonical.Select(x => x.Value).OrderBy(x => x).ToList();
            var percentileCutoff = sortedScores[(int)((float)(sortedScores.Count) * POSSIBLE_ALIAS_PERCENTILE)];

            Console.WriteLine("Canonical ==> Alias [Score]");
            Console.WriteLine("---------------------------------------------------------------------------");
            possibleAliasesByCanonical
                .Where(x => x.Value > percentileCutoff)
                .OrderByDescending(x => x.Value)
                .ToList()
                .ForEach(keyValue => Console.WriteLine("{0} ==> {1} [{2}]", keyValue.Key.Canonical, keyValue.Key.Alias, keyValue.Value));
        }

        public static void Run()
        {
            var listings = Task.Run(() => Loader.IngestListings());
            var tokenProbablities = listings.ContinueWith(x => TokenStatistics.GenerateTokenProbabilitiesPerListing(x.Result));
            var commonTokens = tokenProbablities.ContinueWith(x => TokenStatistics.GetCommonTokens(x.Result));

            var products = Task.Run(() => Loader.IngestProducts());
            var manuNameNGramsByCanonicalManuName = products.ContinueWith(x => NGramBuilder.GenerateManufacturerNameNGrams(x.Result));

            var modelShinglesByModelName = Task.WhenAll(commonTokens, products)
                .ContinueWith(x => NGramBuilder.GenerateModelNameShingles(commonTokens.Result, products.Result));

            // For each listing check all product records for a the best possible match
            var possibleAliasesByCanonical = new ConcurrentDictionary<PossibleAlias, float>();
            listings.Result
                .AsParallel()
                .ForAll(listing =>
                {
                    PossibleAlias bestMatch = null;
                    float bestModelMatchScore = float.MaxValue;

                    foreach (var product in products.Result)
                    {
                        // 1) Check that the listing and product manufacturer names are similar
                        var manuNameMatchScore = CompareManufacturerNameSimilarity(manuNameNGramsByCanonicalManuName.Result, listing.Manufacturer, product.Manufacturer);

                        // Want "fuji" to be an alias for "fujifilm"
                        const int PERCENT_MATCH_CUTOFF = 33;
                        if (manuNameMatchScore < PERCENT_MATCH_CUTOFF)
                            continue; // Names are too different
                        const int PERFECT_MATCH = 100;
                        if (manuNameMatchScore == PERFECT_MATCH && product.Manufacturer == listing.Manufacturer)
                            continue; // Same manufacturer name

                        // 2) Check that the listing text contains the the product model
                        var modelMatchScore = CompareListingTextToProductModel(tokenProbablities.Result, modelShinglesByModelName.Result, listing.Text, product.Model);

                        //if (listing.Manufacturer == "fuji" && modelMatchScore > 0.00000001f)
                            //Debugger.Break();

                        if (modelMatchScore < 0 + float.Epsilon)
                            continue; // No parts of the model name matched

                        // Keep track of the best product model match for the current listing
                        if (modelMatchScore < bestModelMatchScore)
                        {
                            var canonical = product.Manufacturer;
                            var alias = listing.Manufacturer;

                            bestModelMatchScore = modelMatchScore;
                            bestMatch = new PossibleAlias { Canonical = canonical, Alias = alias };
                        }
                    }

                    // Add the possible alias scores together.
                    if (bestMatch != null)
                    {
                        possibleAliasesByCanonical.AddOrUpdate(bestMatch, bestModelMatchScore, (key, old) => old + bestModelMatchScore);
                    }
                });

            DisplayLikelyAliases(possibleAliasesByCanonical);
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