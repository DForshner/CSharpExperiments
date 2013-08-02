using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// This program compares using a HashSet and List to store the postings for a simple inverted index.

namespace InvertedIndexCompareHashSetAndList
{
    /// <summary>
    /// Template pattern from the inverted index.
    /// </summary>
    /// <typeparam name="TDocID">Type of the docID</typeparam>
    public abstract class InvertedIndexTemplate<TDocID>
    {
        protected static readonly Regex splitWords = new Regex(@"[A-Za-z]+");

        public void Add(string text, TDocID docId)
        {
            foreach (var word in SplitWords(text))
            {
                if (!CheckIfWordExistsInIndex(word))
                    AddWordToIndex(word);

                if (CheckDocIDShouldBeAddedToWordPostings(docId, word))
                    AddDocIDToWordPostings(docId, word);
            }
        }

        protected abstract void AddWordToIndex(string word);
        protected abstract bool CheckDocIDShouldBeAddedToWordPostings(TDocID docId, string word);
        protected abstract void AddDocIDToWordPostings(TDocID docId, string word);

        public IList<TDocID> Search(string keywords)
        {
            IEnumerable<TDocID> docIDsWithMatches = null;

            foreach (var word in SplitWords(keywords))
            {
                if (!CheckIfWordExistsInIndex(word))
                    return new List<TDocID>(); // Word does not exist in the index.

                if (docIDsWithMatches == null)
                    docIDsWithMatches = GetPostingsForWord(word);
                else
                {
                    docIDsWithMatches = CheckCurrentWordPostingsAgainstEarlierWordPostings(docIDsWithMatches, word);

                    if (docIDsWithMatches == null)
                        return new List<TDocID>(); // This word does not exist in the same documents as the rest of the keywords.
                }
            }

            return docIDsWithMatches.ToList();
        }

        protected abstract IEnumerable<TDocID> CheckCurrentWordPostingsAgainstEarlierWordPostings(IEnumerable<TDocID> docIDsWithMatches, string word);
        protected abstract IEnumerable<TDocID> GetPostingsForWord(string word);
        protected abstract bool CheckIfWordExistsInIndex(string word);

        protected static IEnumerable<string> SplitWords(string text)
        {
            var words = splitWords.Matches(text);

            foreach (Match wordMatch in words)
                yield return wordMatch.Value;
        }
    }

    /// <summary>
    /// Inverted index implemented with a Dictionary and a HashSet.
    /// </summary>
    public class HashSetInvertedIndex<TDocID> : InvertedIndexTemplate<TDocID>
    {
        private readonly Dictionary<string, HashSet<TDocID>> wordToDocIDPostings = new Dictionary<string, HashSet<TDocID>>();

        protected override void AddWordToIndex(string word)
        {
            wordToDocIDPostings[word] = new HashSet<TDocID>();
        }

        protected override bool CheckDocIDShouldBeAddedToWordPostings(TDocID docId, string word)
        {
            return !wordToDocIDPostings[word].Contains(docId);
        }

        protected override void AddDocIDToWordPostings(TDocID docId, string word)
        {
            wordToDocIDPostings[word].Add(docId);
        }

        protected override bool CheckIfWordExistsInIndex(string word)
        {
            return wordToDocIDPostings.ContainsKey(word);
        }

        protected override IEnumerable<TDocID> GetPostingsForWord(string word)
        {
            return wordToDocIDPostings[word];
        }

        protected override IEnumerable<TDocID> CheckCurrentWordPostingsAgainstEarlierWordPostings(IEnumerable<TDocID> docIDsWithMatches, string word)
        {
            return docIDsWithMatches.Intersect(GetPostingsForWord(word));
        }
    }

    /// <summary>
    /// Inverted index implemented with a Dictionary and a list.
    /// </summary>
    public class ListInvertedIndex<TDocID> : InvertedIndexTemplate<TDocID>
    {
        private readonly Dictionary<string, List<TDocID>> wordToDocIDPostings = new Dictionary<string, List<TDocID>>();

        protected override void AddWordToIndex(string word)
        {
            wordToDocIDPostings[word] = new List<TDocID>();
        }

        protected override bool CheckDocIDShouldBeAddedToWordPostings(TDocID docId, string word)
        {
            return !wordToDocIDPostings[word].Contains(docId);
        }

        protected override void AddDocIDToWordPostings(TDocID docId, string word)
        {
            wordToDocIDPostings[word].Add(docId);
        }

        protected override bool CheckIfWordExistsInIndex(string word)
        {
            return wordToDocIDPostings.ContainsKey(word);
        }

        protected override IEnumerable<TDocID> GetPostingsForWord(string word)
        {
            return wordToDocIDPostings[word];
        }

        protected override IEnumerable<TDocID> CheckCurrentWordPostingsAgainstEarlierWordPostings(IEnumerable<TDocID> docIDsWithMatches, string word)
        {
            return docIDsWithMatches.Intersect(GetPostingsForWord(word));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var docs = GetDocs();

            var keywords = new List<string>() { "a", "the", "war", "the war", "peace", "war and peace", "sea", "air", "land" };

            TestLoadingAndSearchingIndex(docs, keywords, new HashSetInvertedIndex<int>(), "HashSetInvertedIndex");

            TestLoadingAndSearchingIndex(docs, keywords, new ListInvertedIndex<int>(), "ListInvertedIndex");

            Console.ReadKey();
        }

        private static void TestLoadingAndSearchingIndex(IList<string> docs, IList<string> keywords, InvertedIndexTemplate<int> invertedIndex, string indexTypeName)
        {
            double currentLoadTime = 0;
            double lastLoadTime = 0;
            double currentSearchTime = 0;
            double lastSearchTime = 0;

            for (var i = 0; i <= 5; i++)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                docs.Each((line, index) => { invertedIndex.Add(line, index); });

                stopwatch.Stop();
                currentLoadTime = stopwatch.Elapsed.TotalMilliseconds;
                
                stopwatch.Reset();
                stopwatch.Start();

                var searchResults = new List<int>();

                foreach (var keyword in keywords)
                    searchResults.AddRange(invertedIndex.Search(keyword));

                stopwatch.Stop();
                currentSearchTime = stopwatch.Elapsed.TotalMilliseconds;

                Console.WriteLine();
                Console.WriteLine("{0} - {1} records, {2} keywords, {3} total results", indexTypeName, docs.Count().ToString(), keywords.Count().ToString(), searchResults.Count().ToString());
                Console.WriteLine("Loading   - {0} - {1}", currentLoadTime, (lastLoadTime != 0) ? (currentLoadTime / lastLoadTime) : 0);
                Console.WriteLine("Searching - {0} - {1}", lastSearchTime, (lastLoadTime != 0) ? (currentSearchTime / lastSearchTime) : 0);

                lastLoadTime = currentLoadTime;
                lastSearchTime = currentSearchTime;

                docs = DoubleDocs(docs);
            }
        }

        private static IList<string> DoubleDocs(IList<string> originalDocs)
        {
            var docs = new List<string>();

            docs.AddRange(originalDocs);
            docs.AddRange(originalDocs);

            originalDocs = null;

            return docs;
        }

        /// <summary>
        /// Simulate getting documents and storing them in strings.
        /// </summary>
        private static IList<String> GetDocs()
        {
            const string f = "pg2600.txt";

            List<string> lines = new List<string>();

            using (StreamReader r = new StreamReader(f))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                    lines.Add(line);
            }

            return lines.ToList();
        }
    }

    public static class IEnumerableHelper
    {
        public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var index = 0;
            foreach (var e in ie) action(e, index++);
        }
    }
}
