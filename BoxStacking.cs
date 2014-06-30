using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Box Stacking - Given a stack of boxes of different sizes compute the maximum height stack
// that can be created.  Boxes can only be stacked on top of each other if each box is larger in
// h/w/d then the box above it.
//
// Compiled : C# Visual Studio 2013

namespace BoxStacking 
{
    public struct Box
    {
        private int h;
        public int Height { get { return h; } }

        private int w;
        public int Width { get { return w; } }

        private int d;
        public int Depth { get { return d; } }

        public Box(int height, int width, int depth)
        {
            h = height;
            w = width;
            d = depth;
        }

        public static bool operator > (Box l, Box r)
        {
            return (l.Height > r.Height && l.Width > r.Width && l.Depth > r.Depth);
        }

        public static bool operator < (Box l, Box r)
        {
            return !(l > r);
        }
    }

    public class Tower
    {
        private List<Box> boxes = new List<Box>();
        private Dictionary<Box, IList<Box>> AlreadyTried;

        public void Add(Box box) 
        {
            boxes.Add(box); 
        }

        public IEnumerable<Box> GetMaxStack()
        {
            // Assume boxes have been added since the last time stack was called.
            AlreadyTried = new Dictionary<Box, IList<Box>>();

            return SearchMaxStack(new Box(0, 0, 0));
        }

        public IList<Box> SearchMaxStack(Box bottom)
        {
            // Check if we have already tried this box being the bottom. 
            if (AlreadyTried.ContainsKey(bottom))
                return AlreadyTried[bottom];

            IList<Box> currentMax = null;
            foreach (var box in boxes)
            {
                if (box > bottom)
                {
                    var potentialMax = SearchMaxStack(box);
                    // Check if the new stack is greater then the current max stack
                    if (currentMax == null || potentialMax.Sum(x => x.Height) > currentMax.Sum(x => x.Height))
                        currentMax = potentialMax;
                }
            }
               
            var finalStack = new List<Box>();

            // If the bottom is a real box include it as the bottom element.
            if (bottom.Height != 0 && bottom.Width != 0 && bottom.Depth != 0)
                finalStack.Add(bottom);

            // Return the current max stack found.
            if (currentMax != null)
                finalStack.AddRange(currentMax);

            // Store the maximum stack that was found for this bottom
            AlreadyTried.Add(bottom, finalStack);

            return finalStack;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var tower = new Tower();

            var a = new Box(1, 1, 1);
            var b = new Box(2, 2, 2);
            var c = new Box(2, 3, 3);
            Debug.Assert(a < b && b > a); 
            Debug.Assert(c < b && c > a); 

            tower.Add(a);
            tower.Add(b);
            tower.Add(c);
            tower.Add(new Box(1, 2, 3));
            tower.Add(new Box(3, 3, 3));
            tower.Add(new Box(5, 3, 1));
            tower.Add(new Box(2, 3, 5));
            tower.Add(new Box(5, 5, 5));

            Console.WriteLine("Stack:"); 
            var maxStack = tower.GetMaxStack();
            foreach (var box in maxStack)
                Console.WriteLine("{0} x {1} x {2}", box.Height, box.Width, box.Depth);
            Console.WriteLine("Total Height: {0}", maxStack.Sum(x => x.Height));

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);
        }
    }
}