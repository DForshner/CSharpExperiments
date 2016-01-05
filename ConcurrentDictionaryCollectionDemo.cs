using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

// Demo of using concurrent dictionary

namespace ConcurrentDictionaryCollectionDemo
{
    public class Program 
    {
        static void Main(string[] args)
        {
            var toProcess = new ConcurrentQueue<char>();
            var letterFreq = new ConcurrentDictionary<char, int>();
            var tasks = new[]
            {
                Task.Run(() => ProduceLetters(toProcess)),
                Task.Run(() => CountFrequencyA(toProcess, letterFreq)),
                Task.Run(() => CountFrequencyB(toProcess, letterFreq))
            };

            Task.WaitAll(tasks);
        }

        static void ProduceLetters(ConcurrentQueue<char> toProcess)
        {
            var rnd = new Random(DateTime.UtcNow.Millisecond);
            const int numLetters = (int)'f' - (int)'a';
            while(true)
            {
                int r = rnd.Next(0, numLetters);
                var c = (char)((int)'a' + r);
                toProcess.Enqueue(c);
            }
        }

        /// <summary>
        /// Count frequency using AddOrUpdate and GetOrAdd methods.
        /// </summary>
        static void CountFrequencyA(ConcurrentQueue<char> toProcess, ConcurrentDictionary<char, int> freq)
        {
            while (true)
            {
                char c;
                if (!toProcess.TryDequeue(out c))
                {
                    continue;
                }
                int updatedValue = freq.AddOrUpdate(c, 1, (key, oldValue) => oldValue + 1);

                // Other thread could have updated count here

                int currentValue = freq.GetOrAdd(c, 0);
                if (updatedValue != currentValue)
                {
                    DisplayDataRace(c, updatedValue, currentValue);
                }
            }
        }


        /// <summary>
        /// Count frequency using Try#### methods
        /// </summary>
        static void CountFrequencyB(ConcurrentQueue<char> toProcess, ConcurrentDictionary<char, int> freq)
        {
            while (true)
            {
                char c;
                if (!toProcess.TryDequeue(out c))
                {
                    continue;
                }

                // Keep trying to update
                bool hasUpdated = false;
                while (!hasUpdated)
                {
                    int oldValue;
                    if (!freq.TryGetValue(c, out oldValue))
                    {
                        if (!freq.TryAdd(c, 0))
                        {
                            continue; // try to add to dictionary again.
                        }
                    }

                    int updatedValue = oldValue + 1;
                    if (!freq.TryUpdate(c, updatedValue, oldValue))
                    {
                        continue; // Try again
                    }

                    hasUpdated = true;

                    // Other thread could have updated count here

                    int currentValue;
                    if (!freq.TryGetValue(c, out currentValue))
                    {
                        currentValue = 0;
                    }

                    if (updatedValue != currentValue)
                    {
                        DisplayDataRace(c, updatedValue, currentValue);
                    }
                }
            }
        }

        static object _displayLock = new Object();
        private static void DisplayDataRace(char c, int updatedValue, int currentValue)
        {
            lock (_displayLock)
            {
                Console.WriteLine("[{0}] Updated Value: {1} Current Value: {2}", c.ToString(), updatedValue, currentValue);
            }
        }
    }
}