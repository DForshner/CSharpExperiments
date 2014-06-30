using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// Demonstrates how an INumerator<T> dispose method gets called when done iterating.
//
// Based on code challenge: http://ayende.com/blog/167041/code-challenge-make-the-assertion-fire

namespace IEnumerableDisposeMethodCalledWhenIterationComplete
{
    class IEnumerableDisposeMethodCalledWhenIterationComplete
    {
        public static IEnumerable<int> Fibonnaci(CancellationToken token)
        {
            yield return 0;
            yield return 1;

            var prev = 0;
            var cur = 1;

            // Finally gets called when IEnumerator<T> finishes because its Dispose method gets called.
            try
            {
                while (token.IsCancellationRequested == false)
                {
                    var tmp = prev + cur;
                    prev = cur;
                    cur = tmp;
                    yield return tmp;
                }
            }
            finally
            {
                Debug.Assert(token.IsCancellationRequested);
            }
        }
         
        public static void Main()
        {
            // Fires assert and passes.
            FirstTry();

            // Fires assert and fails.
            SecondTry();

            Console.WriteLine("\n\nPress any to exit.\n");
            Console.ReadKey(true);
        }

        private static void SecondTry()
        {
            var token = new CancellationToken();

            Console.WriteLine("\nSecond:");
            foreach (var n in Fibonnaci(token))
            {
                Console.Write("{0},", n);
                if (n > 10) { break; }
            }
        }

        private static void FirstTry()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            Console.WriteLine("\nFirst:");
            foreach (var n in Fibonnaci(token))
            {
                Console.Write("{0},", n);
                if (n > 10) { source.Cancel(); }
            }
        }


    }
}
