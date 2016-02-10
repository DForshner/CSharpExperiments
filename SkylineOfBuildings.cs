using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Given a set of overlapping building find the coordinates of the resulting skyline.

namespace SkylineOfBuildings
{
    public struct Building
    {
        public readonly int LowerLeftX;
        public readonly int Width;
        public readonly int Height;

        public Building(int lowerLeftX, int width, int height)
        {
            LowerLeftX = lowerLeftX;
            Width = width;
            Height = height;
        }
    }

    public struct HeightChangeEvent
    {
        public readonly int X;
        public readonly int Height;
        public readonly bool IsStart;

        public HeightChangeEvent(int x, int height, bool isStart)
        {
            X = x;
            Height = height;
            IsStart = isStart;
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

    public class FakeMaxPriorityQueue
    {
        private SortedList<int, int> _values = new SortedList<int, int>();

        public int GetMax()
        {
            if (IsEmpty) { throw new InvalidOperationException(); }
            return _values.Last().Key;
        }

        public void Enqueue(int value)
        {
            if (!_values.ContainsKey(value))
            {
                _values.Add(value, 0);
            }
            _values[value] += 1;
        }

        public void Dequeue(int value)
        {
            _values[value] -= 1;
            if (_values[value] == 0)
            {
                _values.Remove(value);
            }
        }

        public bool IsEmpty { get { return !_values.Any(); } }
    }

    public static class SkylineBuilder
    {
        public static IEnumerable<Coord> Generate(IEnumerable<Building> buildings)
        {
            var eventsByTime = BuildEventStream(buildings)
                .OrderBy(x => x.X)
                .ToList();

            return ProcessEvents(eventsByTime);
        }

        public static IEnumerable<Coord> ProcessEvents(IList<HeightChangeEvent> eventsByTime)
        {
            // Process unique X coordinate that have an associate event.
            var activeHeights = new FakeMaxPriorityQueue();
            int currEventIdx = 0;
            while (currEventIdx < eventsByTime.Count)
            {
                int heightBeforeEvents = !activeHeights.IsEmpty ? activeHeights.GetMax() : 0;

                // Process all events at the current X
                var currX = eventsByTime[currEventIdx].X;
                while (currEventIdx < eventsByTime.Count && eventsByTime[currEventIdx].X == currX)
                {
                    var e = eventsByTime[currEventIdx];
                    if (e.IsStart)
                    {
                        activeHeights.Enqueue(e.Height);
                    }
                    else
                    {
                        activeHeights.Dequeue(e.Height);
                    }

                    currEventIdx++;
                }

                int heightAfterEvents = !activeHeights.IsEmpty ? activeHeights.GetMax() : 0;
                if (heightBeforeEvents != heightAfterEvents)
                {
                    yield return new Coord(currX, heightBeforeEvents);
                    yield return new Coord(currX, heightAfterEvents);
                }
            }
        }

        /// <summary>
        /// Build a list of events by treating the left side of each building as a
        /// start of height event and the right side as end of height event.
        /// </summary>
        private static IEnumerable<HeightChangeEvent> BuildEventStream(IEnumerable<Building> buildings)
        {
            foreach (var building in buildings)
            {
                yield return new HeightChangeEvent(building.LowerLeftX, building.Height, true);
                yield return new HeightChangeEvent(building.LowerLeftX + building.Width, building.Height, false);
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var buildings = CreateBuildings();
            var skyline = SkylineBuilder.Generate(buildings).ToList();
            DisplaySkyline(skyline);
        }

        private static List<Building> CreateBuildings()
        {
            var buildings = new List<Building>()
            {
                // [---------------------]
                // [--*-*----------------]
                // [-**------------------]
                // [-*--*----------------]
                new Building(1, 2, 1),
                new Building(2, 2, 2),

                // [---------------------]
                // [-------*-*-----------]
                // [------**-**----------]
                // [------*---*----------]
                new Building(6, 1, 1),
                new Building(7, 2, 2),
                new Building(8, 2, 1),

                // [---------------------]
                // [-----------*-***-----]
                // [---------------------]
                // [-----------*-***-----]
                new Building(11, 2, 2),
                new Building(12, 1, 2),
                new Building(14, 1, 2),
            };
            return buildings;
        }

        private static void DisplaySkyline(IEnumerable<Coord> coordinates)
        {
            // Sort in same order as row/col are displayed and proceed in lock-step
            var sortedByRow = coordinates
                .OrderByDescending(x => x.Y)
                .ThenBy(x => x.X)
                .ToList();

            var sb = new StringBuilder();
            int currCoordIdx = 0;
            const int ROWS = 3;
            for (int y = ROWS; y >= 0; --y)
            {
                sb.Clear();
                sb.Append("[");

                const int COLS = 20;
                for (int x = 0; x <= COLS; ++x)
                {
                    // Is there a skyline coordinate for the current row/col?
                    if (currCoordIdx < sortedByRow.Count
                        && sortedByRow[currCoordIdx].X == x
                        && sortedByRow[currCoordIdx].Y == y)
                    {
                        sb.Append("*");
                        currCoordIdx++;
                    }
                    else
                    {
                        sb.Append("-");
                    }
                }

                // Display current row-
                sb.Append("]");
                Console.WriteLine(sb.ToString());
            }
        }
    }
}
