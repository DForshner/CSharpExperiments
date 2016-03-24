using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Tries to match products to listings

// I'm still poking on with this idea so the code is mess.  The basic idea so far is to:
// 1) Normalize the strings
// 2) Bucket/Group products and listings based on canonical product manufacturer names
// 3) For each manufacturer's pair of buckets try and match using an exact model name match.
// 4) For any listings that failed step 3 try and match model names within a Levenshtein edit distance of one.

namespace NormalizedSubStringRecordLinkage
{
    public class Product
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
    }

    public class Listing
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Title { get; set; }
    }

    public static class Matcher
    {
        private static readonly HashSet<char> ignoredChars = new HashSet<char> { '_', '-', '~', ',', '.', '/', '\\', ':' };
        private const string UNKNOWN = "UNKNOWN MANUFACTURER";
        private const int MIN_EDIT_DISTANCE = 1;

        public static IDictionary<Product, List<Listing>> FindBestMatch(IList<Product> products, IList<Listing> listings)
        {
            // Normalize

            foreach (var product in products)
            {
                product.Manufacturer = Normalize(product.Manufacturer);
                product.Model = Normalize(product.Model);
            }

            foreach (var listing in listings)
            {
                listing.Manufacturer = Normalize(listing.Manufacturer);
                listing.Title = Normalize(listing.Title);
            }

            // Bin products by manufacturer
            var productsByManufacturer = new Dictionary<string, List<Product>>();
            foreach (var product in products)
            {
                if (!productsByManufacturer.ContainsKey(product.Manufacturer))
                    productsByManufacturer.Add(product.Manufacturer, new List<Product>());
                productsByManufacturer[product.Manufacturer].Add(product);
            }

            // Block listings by exact match on manufacturer
            var listingsByManufacturer = productsByManufacturer.Keys.ToDictionary(x => x, x => new List<Listing>());
            listingsByManufacturer.Add(UNKNOWN, new List<Listing>());

            foreach (var listing in listings)
            {
                var tokens = listing.Manufacturer.Split((string[])null, StringSplitOptions.RemoveEmptyEntries); // null causes whitespace (Char.IsWhiteSpace = true) to be consider delimiters
                var foundMatch = false;
                foreach (var token in tokens)
                {
                    if (listingsByManufacturer.ContainsKey(token))
                    {
                        listingsByManufacturer[token].Add(listing);
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    // Failed to match on manufacturer so mark as unknown
                    listingsByManufacturer[UNKNOWN].Add(listing);
                }
            }

            // Round 1 - Attempt to match each listing to one of the manufacturer's products using exact model matches.
            var listingsByProduct = new Dictionary<Product, List<Listing>>();
            var unmatchedListingsByManufacturer = new Dictionary<string, List<Listing>>();
            foreach (var manufacturer in productsByManufacturer.Keys)
            {
                var manufacturerProducts = productsByManufacturer[manufacturer];
                var manufacturerListings = listingsByManufacturer[manufacturer];

                foreach (var listing in manufacturerListings)
                {
                    if (listing.Manufacturer == UNKNOWN)
                        continue; // Skip listings where we couldn't match the manufacturer

                    var foundMatch = false;
                    foreach (var product in manufacturerProducts)
                    {
                        if (listing.Title.Contains((product.Model)))
                        {
                            if (!listingsByProduct.ContainsKey(product))
                                listingsByProduct.Add(product, new List<Listing>());
                            foundMatch = true;
                            listingsByProduct[product].Add(listing);
                            break;
                        }
                    }

                    if (foundMatch) continue;

                    // Save unmatched listings
                    if (!unmatchedListingsByManufacturer.ContainsKey(manufacturer))
                        unmatchedListingsByManufacturer.Add(manufacturer, new List<Listing>());
                    unmatchedListingsByManufacturer[manufacturer].Add(listing);
                }
            }

            // Round 2 - Attempt to match any unmatched listings using fuzzy string match
            // TODO: As round 1 fails to match listings put them in a concurrent queue and do round 2 in another thread while round 1 is still running.  Combine dictionaries when finished.
            var failedToMatch = new List<Listing>();
            foreach (var pair in unmatchedListingsByManufacturer)
            {
                if (pair.Key == UNKNOWN)
                    continue; // Skip listings where we couldn't match the manufacturer

                var manufacturerProducts = productsByManufacturer[pair.Key];

                foreach (var listing in pair.Value)
                {
                    var tokens = listing.Title.Split((string[])null, StringSplitOptions.RemoveEmptyEntries); // null causes whitespace (Char.IsWhiteSpace = true) to be consider delimiters
                    var foundMatch = false;
                    foreach (var product in manufacturerProducts)
                    {
                        for (var i = 0; i < tokens.Length; i++)
                        {
                            // Find the edit distance for the current token
                            var editDistanceCurr = LevenshteinEditDistance(tokens[i], product.Model);

                            // Find the edit distance for the current token concatenated with the next token
                            // Handles cases where a separator added between model letters an a model number.  Ex: T5000 is listed as T-5000
                            var editDistanceCurrAndNext = (i < tokens.Length - 1) ? LevenshteinEditDistance(tokens[i] + tokens[i + 1], product.Model) : int.MaxValue;

                            if (editDistanceCurr <= MIN_EDIT_DISTANCE || editDistanceCurrAndNext <= MIN_EDIT_DISTANCE)
                            {
                                // Found probable match
                                if (!listingsByProduct.ContainsKey(product))
                                    listingsByProduct.Add(product, new List<Listing>());
                                foundMatch = true;
                                listingsByProduct[product].Add(listing);
                                break;
                            }
                        }

                        if (foundMatch)
                            break;
                    }

                    if (!foundMatch)
                        failedToMatch.Add(listing);
                }
            }

            return listingsByProduct;
        }

		// Based on https://blogs.msdn.microsoft.com/toub/2006/05/05/generic-levenshtein-edit-distance-with-c/
        private static int LevenshteinEditDistance(string patternA, string patternB)
        {
            Debug.Assert(patternA != null);
            Debug.Assert(patternB != null);

            // if one pattern has length zero then we would have to insert all of the other pattern's characters
            if (patternA.Length == 0) { return patternB.Length; }
            if (patternB.Length == 0) { return patternA.Length; }

            var a = patternA.ToCharArray();
            var b = patternB.ToCharArray();

            // Just store the current row and the next row, each of which has a length m+1 for O(m) space
            int curRow = 0;
            int nextRow = 1;
            var rows = new int[][] { new int[b.Length + 1], new int[b.Length + 1] };

            // Initialize the current row.
            for (int j = 0; j <= b.Length; ++j) { rows[curRow][j] = j; }

            // For each virtual row (since we only have physical storage for two)
            for (int i = 1; i <= a.Length; ++i)
            {
                // Fill in the values in the row
                rows[nextRow][0] = i;
                for (int j = 1; j <= b.Length; ++j)
                {
                    int dist1 = rows[curRow][j] + 1;
                    int dist2 = rows[nextRow][j - 1] + 1;
                    int dist3 = rows[curRow][j - 1] + (a[i - 1].Equals(b[j - 1]) ? 0 : 1);
                    rows[nextRow][j] = Math.Min(dist1, Math.Min(dist2, dist3));
                }

                // Swap the current and next rows
                if (curRow == 0)
                {
                    curRow = 1;
                    nextRow = 0;
                }
                else
                {
                    curRow = 0;
                    nextRow = 1;
                }
            }

            return rows[curRow][b.Length];
        }

        private static string Normalize(string toNormalize)
        {
            var txt = toNormalize.ToCharArray();
            for (var i = 0; i < txt.Length; i++)
            {
                if (ignoredChars.Contains(txt[i]))
                    txt[i] = ' ';

                txt[i] = char.ToLower(txt[i]);
            }
            return new string(txt);
        }
    }

    [TestClass]
    public class ProbabilisticRecordLinkageTests
    {
        [TestMethod]
        public void ExpectExactManufacturerAndModelNamesToMatchUp()
        {
            var products = new[]
            {
                new Product { Id = 1, Manufacturer = @"Sony", Model = @"DSC-W310" },
                new Product { Id = 2, Manufacturer = @"Samsung", Model = @"TL240" },
                new Product { Id = 3, Manufacturer = @"Nikon", Model = @"S6100." }
            }.ToList();

            var listings = new[]
            {
                new Listing { Id = 1, Manufacturer = @"Neewer Electronics Accessories", Title = @"LED Flash Macro Ring Light (48 X LED) with 6 Adapter Rings for For Canon/Sony/Nikon/Sigma Lenses" },
                new Listing { Id = 2, Manufacturer = @"Samsung", Title = @"Samsung TL240 - Digital camera - compact - 14.2 Mpix - optical zoom: 7 x - supported memory: microSD, microSDHC - gray" },
                new Listing { Id = 3, Manufacturer = @"Canon", Title = @"Canon PowerShot SX130IS 12.1 MP Digital Camera with 12x Wide Angle Optical Image Stabilized Zoom with 3.0-Inch LCD" },
                new Listing { Id = 4, Manufacturer = @"Sony", Title = @"Sony DSC-W310 12.1MP Digital Camera with 4x Wide Angle Zoom with Digital Steady Shot Image Stabilization and 2.7 inch LCD (Silver)" },
                new Listing { Id = 5, Manufacturer = @"Samsung", Title = @"Samsung TL240 - Digital camera - compact - 14.2 Mpix - optical zoom: 7 x - supported memory: microSD, microSDHC - black" },
                new Listing { Id = 6, Manufacturer = @"Sony", Title = @"Sony DSC-W310 12.1MP Digital Camera with 4x Wide Angle Zoom with Digital Steady Shot Image Stabilization and 2.7 inch LCD (Pink)" },
                new Listing { Id = 7, Manufacturer = @"Samsung", Title = @"3.5\"" Touch Screen LCD Samsung TL240/ST5000 Digital Point and Shoot Camera 14.2mp, 7x Optical Zoom, 720p HD Video, Orange" },
                new Listing { Id = 8, Manufacturer = @"Sony", Title = @"Sony DSC-W310S Digitalkamera (12 Megapixel, 28mm Weitwinkelobjektiv mit 4fach optischem Zoom, 6,9 cm (2,7 Zoll) LC-Display) silber" }
            }.ToList();

            var results = Matcher.FindBestMatch(products, listings);
            var sonyListings = results.Single(x => x.Key.Id == 1).Value.Select(x => x.Id);
            Assert.IsTrue(new[] { 4, 6, 8 }.SequenceEqual(sonyListings.OrderBy(x => x)));

            var samsungListings = results.Single(x => x.Key.Id == 2).Value.Select(x => x.Id);
            Assert.IsTrue(new[] { 2, 5, 7 }.SequenceEqual(samsungListings.OrderBy(x => x)));
        }

        [TestMethod]
        public void ExpectOffByOneCharModelNamesToMatchUp()
        {
            var products = new[]
            {
                new Product { Id = 1, Manufacturer = @"Sony", Model = @"DSC-W310" },
                new Product { Id = 2, Manufacturer = @"Samsung", Model = @"TL240" },
            }.ToList();

            var listings = new[]
            {
                new Listing { Id = 3, Manufacturer = @"Sony", Title = @"Sony DSCW310" },
                new Listing { Id = 4, Manufacturer = @"Samsung", Title = @"Samsung TL-240 " },
            }.ToList();

            var results = Matcher.FindBestMatch(products, listings);
            var sonyListings = results.Single(x => x.Key.Id == 1).Value.Single();
            Assert.AreEqual(3, sonyListings.Id);

            var samsungListings = results.Single(x => x.Key.Id == 2).Value.Single();
            Assert.AreEqual(4, samsungListings.Id);
        }
    }
}
