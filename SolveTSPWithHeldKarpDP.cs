using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//
// This doesn't work yet.  Hopefully I'll revisit it someday.
//

// Traveling salesman problem.  Given a set of cities find the minimum distance tour that
// visits each city once before returning to the starting point.
//
// Implemented using the Held–Karp dynamic programming algorithm.
//
// The city graph is stored in the following format:
// [number_of_cities]
// [city_1_x_coord] [city_1_y_coord]
// [city_2_x_coord] [city_2_y_coord]

namespace SolveTSPWithHeldKarpDP
{
    public class City
    {
        public readonly double X;
        public readonly double Y;

        public City(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Returns euclidean distance to another city.
        /// </summary>
        public double Distance(City destination)
        {
            return Math.Sqrt( Math.Pow(this.X - destination.X, 2) + Math.Pow(this.Y - destination.Y, 2) );
        }
    }

    public static class TravelingSalesmanProblem
    {
        private static List<HashSet<int>> generateSets(int setSize, int maxValue)
        {
            if (setSize > maxValue) { throw new ArgumentOutOfRangeException(); }

            var possibleValues = Enumerable.Range(0, maxValue + 1).ToList();

             // A temporary array to store all combination one by one
            var data = Enumerable.Repeat(-1, setSize).ToList(); 
 
            var combinations = new List<List<int>>();

            // Print all combination using temporary array 'data[]'
            generateCombinations(possibleValues, setSize, 0, data, 0, combinations);

            var sets = new List<HashSet<int>>();
            foreach (var combo in combinations)
            {
                sets.Add(new HashSet<int>(combo));
            }

            return sets;
        }

        private static void generateCombinations(List<int> arr, int r, 
            int index, List<int> data, int i, List<List<int>> combinations)
        {
            // Current combination is ready so add
            if (index == r)
            {
                var combo = new List<int>();
                for (int j=0; j<r; j++)
                {
                    combo.Add(data[j]);
                }
                combinations.Add(combo);
                return;
            }
 
            // When no more elements are there to put in data[]
            if (i >= arr.Count)
                return;
     
            // current is included, put next at next location
            data[index] = arr[i];
            generateCombinations(arr, r, index + 1, data, i + 1, combinations);
 
            // current is excluded, replace it with next (Note that
            // i+1 is passed, but index is not changed)
            generateCombinations(arr, r, index, data, i + 1, combinations);
        }

        private static double[,] CalculateCostMatrix(IReadOnlyList<City> cities)
        {
            var costMatrix = new double[cities.Count, cities.Count];
            for (var from = 0; from < cities.Count; from++)
            {
                for (var to = 0; to < cities.Count; to++)
                {
                    costMatrix[from, to] = cities[from].Distance(cities[to]);
                }
            }
            return costMatrix;
        }

        private static IReadOnlyList<City> ParseGraphFromFile(string data)
        {
            var lines = data.Split('\n');

            // First line is file is the number of cities
            var header = lines.First().Split(' ');
            var numCities = Int32.Parse(header[0]);

            var cityLines = lines
                .Select((x, i) => new { Data = x, Index = i })
                // Include all non-empty lines after the first line
                .Where(x => x.Index != 0 && x.Data != "");

            var cities = new List<City>(numCities);
            foreach (var line in cityLines)
            {
                var columns = line.Data.Split(' ');
                var x = Double.Parse(columns[0]);
                var y = Double.Parse(columns[1]);

                cities.Add(new City(x, y));
            }

            if (numCities != cities.Count)
            {
                throw new Exception("The number of cities read from file didn't match number of cities specified in file header.");
            }

            return cities.AsReadOnly();
        }

        private static string ReadFile()
        {
            try
            {
                using (var sr = new StreamReader("../../../tsp.txt"))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return null;
        }

        static void Main(string[] args)
        {
            var cities = ParseGraphFromFile(ReadFile());

            var costMatrix = CalculateCostMatrix(cities);

            var g = new Dictionary<Tuple<int, HashSet<int>>, double>();

            // S = 0 - Calculate distance from all cities to starting city.
            var startingCity = cities[0];
            for (var i = 0; i < cities.Count; i++)
            {
                var destination = cities[i];
                var distance = startingCity.Distance(destination);
                g.Add(Tuple.Create(i, new HashSet<int>()), distance);
            }

            // S = 1 - Calculate distance from all cities to starting city.

            // Iterate over every sub problem size
            for (var m = 0; m < cities.Count; m++)
            {
                // TODO: 
            }

            Console.WriteLine("\nDone");
            Console.ReadKey();
        }
    }
}
