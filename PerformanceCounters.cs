using System;
using System.Diagnostics;

// Windows Performance Counters 
//
// Note: Run as administrator if exception occurs when creating perf. counters.

namespace PerformanceCounters 
{
    public sealed class WeatherCheckPeformanceMetrics : IDisposable
    {
        private const string PerformanceCategoryName = "Weather Checker Perf";
        private const string PerformanceCategoryDesc = "Weather Checking Performance Counters";

        private readonly PerformanceCounter numberOfCats;
        private readonly PerformanceCounter numberOfDogs;
        private readonly PerformanceCounter RainDropsPerSecond;
        private readonly PerformanceCounter windowChecks;

        public WeatherCheckPeformanceMetrics()
        {
            if (!PerformanceCounterCategory.Exists(PerformanceCategoryName))
                CreatePerformanceCatagory();

            windowChecks = new PerformanceCounter(PerformanceCategoryName, "Weather Check - Window Checks", false);
            numberOfCats = new PerformanceCounter(PerformanceCategoryName, "Weather Check - Cats", false);
            numberOfDogs = new PerformanceCounter(PerformanceCategoryName, "Weather Check - Dogs", false);
            RainDropsPerSecond = new PerformanceCounter(PerformanceCategoryName, "Weather Check - Rain Drops/sec", false);

            windowChecks.Increment();
        }

        /// <summary>
        /// Creates new performance category 
        /// </summary>
        private static void CreatePerformanceCatagory()
        {
            var counters = new CounterCreationDataCollection();

            counters.Add(new CounterCreationData(
                    "Weather Check - Window Checks",
                    "Number of times I looked out the window.",
                    PerformanceCounterType.NumberOfItems32
                )
            );

            counters.Add(new CounterCreationData(
                    "Weather Check - Cats",
                    "Number of cats.",
                    PerformanceCounterType.NumberOfItems32
                )
            );

            counters.Add(new CounterCreationData(
                    "Weather Check - Dogs",
                    "Number of dogs.",
                    PerformanceCounterType.NumberOfItems32
                )
            );

            counters.Add(new CounterCreationData(
                    "Weather Check - Rain Drops/sec",
                    "Number of rain drops per second.",
                    PerformanceCounterType.RateOfCountsPerSecond32
                )
            );

            PerformanceCounterCategory.Create(PerformanceCategoryName, PerformanceCategoryDesc, 
                PerformanceCounterCategoryType.SingleInstance, counters);
        }

        public void OnCheckWindow()
        {
            windowChecks.Increment();
        }

        public void OnSeeDogs(int dogs)
        {
            numberOfDogs.IncrementBy(dogs);
        }

        public void OnSeeCats(int cats)
        {
            numberOfCats.IncrementBy(cats);
        }

        public void OnMeasureRainDrops(int numberOfDrops)
        {
            RainDropsPerSecond.IncrementBy(numberOfDrops);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;

                windowChecks.Dispose();
                numberOfCats.Dispose();
                numberOfDogs.Dispose();
                RainDropsPerSecond.Dispose();
            }
        }
    }


    public class WeatherChecker
    {
        private readonly WeatherCheckPeformanceMetrics metrics;
        Random rnd = new Random();

        public WeatherChecker() { }

        public WeatherChecker(WeatherCheckPeformanceMetrics metrics)
        {
            this.metrics = metrics;
        }

        public int FakeSomeData()
        {
            return rnd.Next(0, 10000);
        }

        public void CheckOutside()
        {
            if (metrics != null) 
                metrics.OnCheckWindow();

            MeasureCats();
            MeasureDogs();
            MeasureDrops();
        }

        private void MeasureCats()
        {
            var cats = FakeSomeData();
            Console.WriteLine("Measured {0} cats with cat-o-meter", cats);

            if (metrics != null)
                metrics.OnSeeCats(cats);
        }

        private void MeasureDogs()
        {
            var dogs = FakeSomeData();
            Console.WriteLine("Measured {0} dogs with dog-o-scope", dogs);

            if (metrics != null)
                metrics.OnSeeDogs(dogs);
        }

        private void MeasureDrops()
        {
            var drops = FakeSomeData();
            Console.WriteLine("Measured {0} rain drops with teaspoon", drops);

            if (metrics != null)
                metrics.OnMeasureRainDrops(drops);
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var metrics = new WeatherCheckPeformanceMetrics();
            var checker = new WeatherChecker(metrics);

            while (!Console.KeyAvailable)
                checker.CheckOutside();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();
        }
    }
}
