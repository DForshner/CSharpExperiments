using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// Given a 2d height map of a landmass with both Atlantic and Pacific coasts find the continental divide.
// Time Complexity: O(n^2)

namespace FindContinentalDivideOf2DHeightMap
{
    /// <summary>
    /// Associates a single primitive value with each coordinate on a landmass
    /// </summary>
    public abstract class LandmassAspect<T> where T : struct
    {
        protected int _dim;
        protected List<List<T>> _values;

        public int Dim { get { return _dim;  } }

        public bool Contains(Coord p)
        {
            return (p.X >= 0 && p.X < _dim && p.Y >= 0 & p.Y < _dim);
        }

        public T this[Coord key]
        {
            get
            {
                Debug.Assert(Contains(key), "Expected coordinate to be inside landmass");
                return _values[key.X][key.Y];
            }
            set
            {
                Debug.Assert(Contains(key), "Expected coordinate to be inside landmass");
                _values[key.X][key.Y] = value;
            }
        }
    }

    public class HeightMap : LandmassAspect<int>
    {
        public HeightMap(List<List<int>> heights)
        {
            var dim = heights.Count;
            Debug.Assert(heights.All(x => x.Count == dim), "Expected square landmass");

            _dim = dim;
            _values = heights;
        }

    }

    public class MarkedLandmass : LandmassAspect<bool>
    {
        public MarkedLandmass(int dim)
        {
            _dim = dim;
            _values = new List<List<bool>>(dim);

            for (var i = 0; i < dim; i++)
            {
                var row = new List<bool>();
                for (var j = 0; j < dim; j++)
                {
                    row.Add(false);
                }
                _values.Add(row);
            }
        }

        public MarkedLandmass Union(MarkedLandmass other)
        {
            Debug.Assert(Dim == other.Dim, "Expected same size.");

            var union = new MarkedLandmass(Dim);
            for (var i = 0; i < union.Dim; ++i)
            {
                for (var j = 0; j < union.Dim; ++j)
                {
                    var curr = new Coord(i, j);
                    union[curr] = this[curr] && other[curr];
                }
            }
            return union;
        }
    }

    public struct Coord
    {
        public readonly int X;
        public readonly int Y;

        public Coord(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public static class DownhillFlowMarker
    {
        private static List<Tuple<int, int>> dirs = new List<Tuple<int, int>>
        {
            Tuple.Create(-1, 0),
            Tuple.Create(1, 0),
            Tuple.Create(0, 1),
            Tuple.Create(0, -1)
        };

        /// <summary>
        /// BFS to mark all points that can flow into the starting point.
        /// </summary>
        public static void MarkReachable(Coord start, HeightMap heights, MarkedLandmass markedLandmassToUpdate)
        {
            Debug.Assert(heights.Dim == markedLandmassToUpdate.Dim, "Expected same size");
            Debug.Assert(heights.Contains(start));
            Debug.Assert(markedLandmassToUpdate.Contains(start));

            var toProcess = new Queue<Coord>();
            toProcess.Enqueue(start);

            while(toProcess.Any())
            {
                var curr = toProcess.Dequeue();
                if (markedLandmassToUpdate[curr])
                {
                    // Already processed this point so skip it to prevent cycles
                    continue;
                }

                markedLandmassToUpdate[curr] = true;

                // Queue any un-visited neighbors that can flow into down into the current position.
                foreach(var dir in dirs)
                {
                    var neighbor = new Coord(curr.X + dir.Item1, curr.Y + dir.Item2);

                    if (heights.Contains(neighbor) // Inside landmass
                        && !markedLandmassToUpdate[neighbor] // Unvisited
                        && heights[neighbor] >= heights[curr]) // Can flow down into the current position
                    {
                        toProcess.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    public static class ContinentalDivideLocator
    {
        public static MarkedLandmass FindDivide(HeightMap heights)
        {
            var atlantic = new MarkedLandmass(heights.Dim);
            for (var i = 0; i < heights.Dim; ++i)
                DownhillFlowMarker.MarkReachable(new Coord(0, i), heights, atlantic);
            for (var i = 0; i < heights.Dim; ++i)
                DownhillFlowMarker.MarkReachable(new Coord(i, 0), heights, atlantic);

            var pacific = new MarkedLandmass(heights.Dim);
            for (var i = 0; i < heights.Dim; ++i)
                DownhillFlowMarker.MarkReachable(new Coord(heights.Dim - 1, i), heights, pacific);
            for (var i = 0; i < heights.Dim; ++i)
                DownhillFlowMarker.MarkReachable(new Coord(i, heights.Dim - 1), heights, pacific);

            return atlantic.Union(pacific);
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var heights = new HeightMap(new List<List<int>>
            {
                new List<int> { 1,2,3,4,5,5 },
                new List<int> { 1,2,3,5,4,3 },
                new List<int> { 1,2,4,5,3,2 },
                new List<int> { 1,3,5,3,2,1 },
                new List<int> { 4,5,3,2,1,1 },
                new List<int> { 5,4,3,2,1,1 },
            });

            var marked = ContinentalDivideLocator.FindDivide(heights);

            Display(marked);
        }

        private static void Display(MarkedLandmass marked)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < marked.Dim; ++i)
            {
                for (var j = 0; j < marked.Dim; ++j)
                {
                    sb.Append(marked[new Coord(i, j)] ? "#" : "-");
                }
                Console.WriteLine("[ {0} ]", sb.ToString());
                sb.Clear();
            }
        }
    }
}
