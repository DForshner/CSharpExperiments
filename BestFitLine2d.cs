using System;
using System.Collections.Generic;
using System.Diagnostics;

// Find the line that passes through the most number of points on a 2D plane. 
//
// Now with more console graphics! 
//
// Compiled: Visual Studio 2013

namespace BestFitLine2D 
{
    public class Point
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class Line : IEquatable<Line>
    {
        public const double EPSILON = 0.5;

        public double Slope { get; private set; } 
        public double Intercept { get; private set; } 

        public Line(Point a, Point b)
        {
            // Check if line has slope
            var run = a.X - b.X;
            if (Math.Abs(run) > EPSILON)
            {
                var rise = a.Y - b.Y;
                Slope = rise / run;
                Intercept = a.Y - Slope * a.X; 
            }
            else
            {
                Slope = Double.PositiveInfinity;
                Intercept = a.X;
            }
        }

        public double FlooredScope
        {
            get 
            {
                if (Double.IsPositiveInfinity(Slope))
                    return Slope;

                var r = (int) Slope / EPSILON;
                return (double) r * EPSILON;
            }
        }

        public Point GetPoint(double x)
        {
            if (!Double.IsInfinity(Slope))
                return new Point(x , Slope * x + Intercept);

            return new Point(x , Intercept);
        }

        public bool Equals(Line other)
        {
            if (!(this.Slope == Double.PositiveInfinity && other.Slope == Double.PositiveInfinity)
                && (Math.Abs(this.Slope - other.Slope) > EPSILON))
                return false;

            if (Math.Abs(this.Intercept - other.Intercept) > EPSILON)
                return false;

            return true;
        }
    }

    public class BestFit
    {
        private Dictionary<double, LinkedList<Line>> slopeToLines = new Dictionary<double, LinkedList<Line>>();

        public void AddLine(Line lineToAdd)
        {
            var scope = lineToAdd.FlooredScope;
            if (!slopeToLines.ContainsKey(scope))
                slopeToLines.Add(scope, new LinkedList<Line>());
            slopeToLines[scope].AddLast(lineToAdd);
        }

        public Line FindBestFit(IEnumerable<Point> points)
        {
            int bestCount = 0;
            Line bestLine = null;
            foreach (var a in points)
            {
                foreach (var b in points)
                {
                    var line = new Line(a, b);
                    AddLine(line);
                    var count = CountEquivalentLines(line);
                    if (count > bestCount)
                    {
                        bestLine = line;
                        bestCount = count;
                    }
                }
            }

            return bestLine;
        }

        private int CountEquivalentLines(Line lineToCheck)
        {
            return CountEquivalentLines(lineToCheck, lineToCheck.FlooredScope)
                + CountEquivalentLines(lineToCheck, lineToCheck.FlooredScope + Line.EPSILON)
                + CountEquivalentLines(lineToCheck, lineToCheck.FlooredScope - Line.EPSILON);
        }

        private int CountEquivalentLines(Line lineToCheck, double slope)
        {
            if (!slopeToLines.ContainsKey(slope))
                return 0;
 
            int count = 0;
            foreach (var line in slopeToLines[slope])
                if (line.Equals(lineToCheck))
                    count++;

            return count;
        }
    }

    public static class Program
    {
        private const int HEIGHT = 40;
        private const int WIDTH = 40;

        public static IEnumerable<Point> ToPoints(List<int> points)
        {
            Debug.Assert(points.Count % 2 == 0, "Expected even number of points.");

            for (var i = 0; i < points.Count; i += 2)
                yield return new Point(points[i], points[i + 1]);
        }

        public static void Main()
        {
            Console.SetWindowSize(WIDTH, HEIGHT);
            Console.ForegroundColor = ConsoleColor.Green;

            //var points = ToPoints(new List<int>() { -5,5,-1,-1,-1,1,1,-1,1,1,3,3,7,7 });

            ConsoleKeyInfo key = new ConsoleKeyInfo();
            while (key.Key != ConsoleKey.Escape)
            {
                var points = GetRandomPoints();

                Console.Clear();
                DrawXAxis();
                DrawYAxis();

                var bestFit = new BestFit();
                var bestLine = bestFit.FindBestFit(points);
                Draw(bestLine);
                Draw(points);

                key = Console.ReadKey(true);
            }
        }

        private static List<Point> GetRandomPoints()
        {
            var rand = new Random(DateTime.Now.Millisecond);

            var points = new List<Point>();
            for (var i = 0; i <= 20; i++)
            {
                var x = (rand.Next(WIDTH)) - (WIDTH / 2); 
                Debug.Assert(x < WIDTH / 2);
                var y = (rand.Next(HEIGHT)) - (HEIGHT / 2);
                Debug.Assert(y < HEIGHT / 2);
                points.Add(new Point(x,y));
            }

            return points;
        }

        private static void Draw(IEnumerable<Point> points)
        {
            foreach (var point in points)
                DrawChar(point.X, point.Y, 'X');
        }

        private static void DrawYAxis()
        {
            for (var i = -(HEIGHT / 2); i <= (HEIGHT / 2); i++)
                DrawChar(0, i, '|');
        }

        private static void DrawXAxis()
        {
            for (var i = -(WIDTH / 2); i <= (WIDTH / 2); i++)
                DrawChar(i, 0, '-');
        }

        private static void Draw(Line line)
        {
            for (var i = -(WIDTH / 2); i <= (WIDTH / 2); i++)
            {
                var y = (!Double.IsInfinity(line.Slope)) ?
                    (int)(line.Slope * i + line.Intercept) :
                    (int)line.Intercept;

                DrawChar(i, y, '-');
            }
        }

        private static void DrawChar(double x, double y, char c)
        {
            Console.SetCursorPosition((WIDTH + 1) / 2 + Floor(x), ((HEIGHT + 1) / 2) - Floor(y));
            Console.Write(c);
        }

        private static int Floor(double i)
        {
            var r = (int) (i / Line.EPSILON);
            return (int) ( (double) r * Line.EPSILON);
        }
    }
}