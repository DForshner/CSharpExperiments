using System;
using System.Diagnostics;
using System.Linq;

// Naive benchmark of summing over and array of structs, array of classes
// and and array of classes that aren't laid out contiguously in memory.
//
// Compiled: Visual Studio 2013

namespace ArraySumBenchmark
{
    public static class Program
    {
        private const int SIZE = 10000000;

        struct MyStruct
        {
            public int A;
            public int B;

            public MyStruct(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        class MyClass
        {
            public readonly int A;
            public readonly int B;

            public MyClass(int a, int b)
            {
                this.A = a;
                this.B = b;
            }
        }

        public static void Main()
        {
            var sequentialOrder = Enumerable.Range(0, SIZE).ToArray();

            var randomOrder = new int[SIZE];
            sequentialOrder.CopyTo(randomOrder, 0);

            Shuffle(randomOrder);

            MyStruct[] structs = MakeArrayOfStructs();

            Action<int[]> SumStructs = (int[] readOrder) =>
            {
                long sum = 0;
                foreach(var index in readOrder)
                    sum += structs[index].A + structs[index].B;
            };

            Console.WriteLine("\n-- Access Array of Structs Sequentially --");
            Measure(() => SumStructs(sequentialOrder));

            Console.WriteLine("\n-- Access Array of Structs Randomly --");
            Measure(() => SumStructs(randomOrder));

            MyClass[] classes = MarkArrayOfClasses();

            Action<int[]> SumClasses = (readOrder) =>
            {
                long sum = 0;
                foreach(var index in readOrder)
                    sum += classes[index].A + classes[index].B;
            };

            Console.WriteLine("\n-- Access Array of Classes Sequentially --");
            Measure(() => SumClasses(sequentialOrder));

            Console.WriteLine("\n-- Access Array of Classes Randomly --");
            Measure(() => SumClasses(randomOrder));

            var nonContiguousClasses = MakeArrayOfNonContiguousClasses();

            Action<int[]> SumNonContiguousClasses = (readOrder) =>
            {
                long sum = 0;
                foreach(var index in readOrder)
                    sum += nonContiguousClasses[index].A + nonContiguousClasses[index].B;
            };

            Console.WriteLine("\n-- Access Array of Non-Contigous Classes Sequentially --");
            Measure(() => SumNonContiguousClasses(sequentialOrder));

            Console.WriteLine("\n-- Access Array of Non-Contigous Classes Randomly --");
            Measure(() => SumNonContiguousClasses(randomOrder));

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Fisher-Yates Shuffle ~O(n)
        /// </summary>
        private static void Shuffle<T>(T[] arrayToShuffle)
        {
            var rnd = new Random();
            var i = arrayToShuffle.Length;
            while (i > 1)
            {
                var j = rnd.Next(--i);
                var temp = arrayToShuffle[i];
                arrayToShuffle[i] = arrayToShuffle[j];
                arrayToShuffle[j] = temp;
            }
        }

        private const int ITERATIONS = 5;

        private static void Measure(Action action)
        {
            var watch = new Stopwatch();
            long elapsed = 0;
            GC.Collect();

            foreach (var i in Enumerable.Range(0, ITERATIONS))
            {
                watch.Start();
                action();
                watch.Stop();
                elapsed += watch.ElapsedMilliseconds;
                watch.Reset();
            }

            Console.WriteLine("Elapsed time: {0}", elapsed / ITERATIONS);
        }

        private static MyStruct[] MakeArrayOfStructs()
        {
            MyStruct[] structs; // Reference
            // Create array of structs on heap and point reference to it.
            structs = Enumerable.Repeat(new MyStruct(1, 3), SIZE).ToArray();
            return structs;
        }

        private static MyClass[] MarkArrayOfClasses()
        {
            MyClass[] classes; // Reference 
            // Create array of class references on heap and point each reference to instance of class on heap. 
            classes = Enumerable.Repeat(new MyClass(1, 3), SIZE).ToArray();
            return classes;
        }

        /// <summary>
        /// Attempt to create an array of references to non-contiguous classes by
        /// allocating junk objects between the objects that are referenced in the final array
        /// </summary>
        private static MyClass[] MakeArrayOfNonContiguousClasses()
        {
            MyClass[] junk1 = new MyClass[SIZE]; // Reference 
            MyClass[] junk2 = new MyClass[SIZE];
            MyClass[] classes = new MyClass[SIZE];

            foreach (var i in Enumerable.Range(0, SIZE))
            {
                junk1[i] = new MyClass(0, 2);
                junk2[i] = new MyClass(1, 1);
                classes[i] = new MyClass(junk1[i / 2].A + junk2[i / 3].A, junk1[i / 4].B + junk2[i / 5].B);
            }

            return classes;
        }
    }
}