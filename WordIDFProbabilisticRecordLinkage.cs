using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Tries to match offer ads to want ads based on weighted words in common.
// 1) The inverse document frequency (IDF) of each word across all want ads is used as its weight/score.
// 2) Each offer ad is compared to every want ad and when they both share a word is in common the matches score is increased by the IDF value.
// 3) The maximum score is tracked to find the best match.

// This doesn't appear to be great approach.  When filtering out common words it's easy to remove words like 'cat' and 'dog' from the
// feature vectors even when they are useful in separating which ads are for dogs and which are for cats.

namespace WordIDFProbabilisticRecordLinkage
{
    public class Doc
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    public class Match
    {
        public int WantId { get; set; }
        public int OfferId { get; set; }
        public double Score { get; set; }
    }

    public static class Matcher
    {
        const double COMMON_WORD_PERCENTILE = 0.15D;  // 15%

        public static IList<Match> FindBestMatch(IList<Doc> wantedAds, IList<Doc> offerAds)
        {
            var wantAdWordIDF = InverseDocFrequencyCalculator.Count(wantedAds, x => x.Text);
            var commonWordIDFCutoff = GetApproxPercentile(wantAdWordIDF, COMMON_WORD_PERCENTILE);

            var wordWeights = new List<double>();
            var idxByWord = new Dictionary<string, int>();
            foreach(var pair in wantAdWordIDF)
            {
                if (pair.Value < commonWordIDFCutoff)
                {
                    continue; // Don't use common words as features
                }

                wordWeights.Add(pair.Value);
                idxByWord.Add(pair.Key, wordWeights.Count - 1);
            }

            // Features are stored as bool array indexed by [adId][wordId] where true means the word (with wordId) is present in Ad (with adId)
            var wantAddFeatures = CreateFeatureMatrix(wantedAds, wordWeights, idxByWord);
            var offerAddFeatures = CreateFeatureMatrix(offerAds, wordWeights, idxByWord);

            // For each offer ad try and find the best matching want ad by finding common words and created as scored based on the word's uniqueness (IDF).
            var toReturn = new List<Match>();
            for (var offerAdIdx = 0; offerAdIdx < offerAddFeatures.Count; offerAdIdx++)
            {
                double bestWantAdScore = 0D;
                int bestWantAdIdx = 0;
                for (var wantAdIdx = 0; wantAdIdx < wantAddFeatures.Count; wantAdIdx++)
                {
                    // Calculate a score for how well the want ad matches the offer ad
                    var score = 0D;
                    for (var i = 0; i < wordWeights.Count; i++)
                    {
                        if (wantAddFeatures[wantAdIdx][i] && offerAddFeatures[offerAdIdx][i])
                        {
                            score += wordWeights[i];
                        }
                    }

                    // Keep track of the max score
                    if (score > bestWantAdScore)
                    {
                        bestWantAdScore = score;
                        bestWantAdIdx = wantAdIdx;
                    }
                }

                toReturn.Add(new Match { WantId = wantedAds[bestWantAdIdx].Id, OfferId = offerAds[offerAdIdx].Id, Score = bestWantAdScore });
            }

            return toReturn;
        }

        private static List<List<bool>> CreateFeatureMatrix(IList<Doc> ads, IList<double> wordWeights, IDictionary<string, int> idxByWord)
        {
            var features = new List<List<bool>>();
            foreach (var doc in ads)
            {
                features.Add(Enumerable.Repeat<bool>(false, wordWeights.Count).ToList());

                var words = doc.Text.Split(null); // null splits based on Unicode Char.IsWhiteSpace
                foreach (var word in words)
                {
                    if (idxByWord.ContainsKey(word))
                    {
                        var idx = idxByWord[word];
                        features.Last()[idx] = true;
                    }
                }
            }
            return features;
        }

        private static double GetApproxPercentile(IDictionary<string, double> wordIDF, double percentile)
        {
            Debug.Assert(percentile >= 0 && percentile <= 1D);

            var idfs = wordIDF.Values.ToList();
            idfs.Sort();
            var cutoffIdx = (double)idfs.Count * percentile;
            return idfs[(int)cutoffIdx];
        }
    }

    public static class InverseDocFrequencyCalculator
    {
        public static IDictionary<string, double> Count<T>(IEnumerable<T> docs, Func<T, String> selector)
        {
            var wordFrequency = new Dictionary<string, int>();
            var numDocs = 0;
            foreach (var doc in docs)
            {
                numDocs += 1;

                var line = selector(doc);
                var tokens = line.Split(null); // null splits based on Unicode Char.IsWhiteSpace

                var uniqueWordsPerDoc = new HashSet<string>();
                foreach (var token in tokens)
                {
                    if (!uniqueWordsPerDoc.Contains(token))
                    {
                        uniqueWordsPerDoc.Add(token);
                    }
                }

                foreach (var word in uniqueWordsPerDoc)
                {
                    if (!wordFrequency.ContainsKey(word))
                    {
                        wordFrequency.Add(word, 0);
                    }
                    wordFrequency[word] += 1;
                }
            }

            var inverseDocFrequency = new Dictionary<string, double>();
            foreach (var term in wordFrequency)
            {
                inverseDocFrequency.Add(term.Key, Math.Log((double)numDocs / (double)term.Value));
            }

            return inverseDocFrequency;
        }
    }

    [TestClass]
    public class ProbabilisticRecordLinkageTests
    {
        [TestMethod]
        public void ExpectMatchesOffersToWantAds()
        {
            var wanted = new[]
            {
                new Doc { Id = 1, Text = @"WANT an English Bulldog, Shar Pei cross dog or puppy We bought one years ago and are looking for another." },
                new Doc { Id = 2, Text = @"hi im looking for a collie shepard class dog that i can turn into my new service animal the reasion for the collie shepard cross is cause they are super loyal and " },
                new Doc { Id = 3, Text = @"We are looking for a medium size dog to join our family (1 yrs+). We have two kids and a cat, so the dog must be kid/cat friendly." },
                new Doc { Id = 4, Text = @"im looking for a Saint Bernard or huskey shepherd mix, if any breeders have a litter of puppies being born on Augest or Septembre." },
                new Doc { Id = 5, Text = @"looking to make payments on a chihuahua or other small breed.but need at least 2 months time to give you the money in full before the pups can leave." },

                new Doc { Id = 6, Text = @"Meet Pekoe. An adorable long-haired orange tabby with a big personality."},
                new Doc { Id = 7, Text = @"Looking for a male Maine Coon kitten. I have always wanted a Maine Coon. I have a very friendly black lab who is great with cats." },
                new Doc { Id = 8, Text = @"Hi We are looking for adult ragdoll cat, ages between 1-4 yrs will be preferred! Please send us pictures and adoption fee! Thanks" },
                new Doc { Id = 9, Text = @"Looking for a siamese cat, preferably de-clawed. Must be good with small dogs." },
                new Doc { Id = 10, Text = @"I really like cat. Cat better be younger than 8 months. Send me message if you can satisfy my desire. Thanks." },
            }.ToList();
            wanted.ForEach(x => x.Text = x.Text.ToLowerInvariant());

            var offers = new[]
            {
                new Doc { Id = 1, Text = @"Brand new! Custom made!! Approximately 4' tall. 2' wide. 4 levels with 2 scratch posts made with Sisal rope to entice scratching. Will deliver if requested!" },
                new Doc { Id = 2, Text = @"My girlfriends cat bit me for petting it wrong, so now I want to give it away while she's at work and tell her it ran away. Free to first person who wants it" },

                new Doc { Id = 3, Text = @"☺ chihuahua puppies from a loving home looking for loving home. Raised around larger dog and lots of visitors very well socialized they have been vet checked and needled." },
                new Doc { Id = 4, Text = @"hi i am offering a 4 yr old rotti, he is good with people and children, but will stand his ground towards other dogs. he will not tolerate cats." },
                new Doc { Id = 5, Text = @"Pure bred rottweiler pups ready to go. Tails docked. Both parents on site. Please call or text as emails may not be answered. " },

                new Doc { Id = 6, Text = @"Siamese Cat ""Ivy"" Regrettably I have to give up my cat, Ivy. She is a 6 year old siamese cat who is super friendly and loves attention." },
                new Doc { Id = 7, Text = @"Beautiful Maine coon for sale 500$ comes with papers shots up to date selling because my little sister is allergic to it, most loving cat ever 9 months follows you around like a dog." },
                new Doc { Id = 8, Text = @"one kitten sold :) 3 available 4 beautiful babies their eyes are 95 % open ..2 weeks old Dad is Pixie bob and mom is Maine Coon" }
            }.ToList();
            offers.ForEach(x => x.Text = x.Text.ToLowerInvariant());

            var results = Matcher.FindBestMatch(wanted, offers);

            Assert.IsTrue(results.Any(x => x.WantId == 7 && x.OfferId == 8));
        }
    }

    [TestClass]
    public class InverseDocFrequencyCalculatorTests
    {
        private class Fake
        {
            public string Data { get; set; }
        }

        [TestMethod]
        public void ExpectFindsInverseDocumentFrequency()
        {
            var docs = new[]
                {
                    new Fake { Data = "A B C D E F" },
                    new Fake { Data = "A B C D E F" },
                    new Fake { Data = "A B C D" },
                    new Fake { Data = "A B C D" },
                    new Fake { Data = "A B" },
                    new Fake { Data = "A B" },
                    new Fake { Data = "" },
                };
            var result = InverseDocFrequencyCalculator.Count(docs, x => x.Data);
            Assert.AreEqual(Math.Log(7D / 6D), result["A"], 0.01D);
            Assert.AreEqual(Math.Log(7D / 2D), result["F"], 0.01D);
            Assert.IsFalse(result.ContainsKey("G"));
        }
    }
}