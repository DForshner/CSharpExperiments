using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

// This demo explores an entity component system built using object orientated programming
// vs a data orientated approach.
//
// Compares three different ways of processing particles bouncing in a box:
//
// Traditional object orientated programming:
// 1) Entity with multiple components (entity component system).
//
// Data orientated design:
// 2) Entity that is an index into arrays of components.  Uses an event component with an updated flag to trigger processing.
// 3) Same as above but instead of using an event component uses a list of bounce events that is processed each iteration (event queue).
//
// Notes: 
// - Data orientated is faster even when the OOP objects are created close together in time which should have placed them
// close together in memory.
// - Having a component that is only processed for a small proportion of the entities appears to be slower.  I imagine there is break even point
// where above which it is more efficient for every entity to pass events in a component. 

namespace EntityComponentSystemObjectOrientatedVsDataOrientated
{
    public static class Settings 
    {
        public const int NUM_PARTICLES = 5000000;
        public const int GRID_SIZE = NUM_PARTICLES;
        public const int MAX_VELOCITY = 100;
        public const int BYTES_IN_MB = 1000000;
        public const int ITERATIONS = 50;
    }

    public struct Position
    {
        public int X;
        public int Y;
    }

    public struct Velocity
    {
        public int X;
        public int Y;
    }

    public static class SimulateTraditionalOOPEntityComponent
    {
        private abstract class Entity
        {
            public int Id;
            public IList<Component> Components;
            public abstract bool Update();
        }

        private abstract class Component
        {
            public abstract void Update(Entity entity);
        }

        private class MovementComponent : Component
        {
            public Position Position;
            public Velocity Velocity;

            public MovementComponent(int posX, int posY, int velX, int velY)
            {
                Position.X = posX;
                Position.Y = posY;
                Velocity.X = velX;
                Velocity.Y = velY;
            }

            public override void Update(Entity entity)
            {
                Position.X += Velocity.X;
                Position.Y += Velocity.Y;
            }
        }

        private class BounceComponent : Component
        {
            private MovementComponent _move;

            public BounceComponent(MovementComponent comp)
            {
                _move = comp;
            }

            public override void Update(Entity entity)
            {
                if (_move.Position.X < 0 || _move.Position.X > Settings.GRID_SIZE)
                {
                    _move.Velocity.X = -_move.Velocity.X;
                    (entity as Particle).bouncedLastUpdate = true;
                }
                else if (_move.Position.Y < 0 || _move.Position.Y > Settings.GRID_SIZE)
                {
                    _move.Velocity.Y = -_move.Velocity.Y;
                    (entity as Particle).bouncedLastUpdate = true;
                }
            }
        }

        private class Particle : Entity
        {
            public bool bouncedLastUpdate;

            public Particle(int id, IEnumerable<Component> components)
            {
                Id = id;
                Components = components.ToList();
            }

            /// <returns>True if particle bounced</returns>
            public override bool Update()
            {
                foreach (var comp in Components)
                {
                    comp.Update(this);
                }

                if (bouncedLastUpdate)
                {
                    bouncedLastUpdate = false;
                    return true;
                }

                return false;
            }
        }

        private static System.Diagnostics.Stopwatch s = new Stopwatch();

        public static void Run()
        {
            Particle[] particles = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(i =>
                {
                    var move = new MovementComponent(posX: i % Settings.GRID_SIZE, posY: i % Settings.GRID_SIZE, velX: i % Settings.MAX_VELOCITY + 1, velY: i % Settings.MAX_VELOCITY + 1);
                    var bounce = new BounceComponent(move);
                    return new Particle(i, new Component[] { move, bounce });
                })
                .ToArray();

            s.Start();
            s.Restart();
            for (var i = 0; i < Settings.ITERATIONS; ++i)
            {
                var bounces = 0;
                foreach (var particle in particles)
                {
                    if (particle.Update()) { ++bounces; }
                }

                Console.WriteLine("Time {0} Bounces: {1}", s.ElapsedMilliseconds, bounces);
            }
            s.Stop();

            Console.WriteLine("Average time per iteration: " + s.ElapsedMilliseconds / Settings.ITERATIONS);
        }
    }

    public static class SimulateComponentsWithEventComponent
    {
        private struct BounceEvent
        {
            public bool Updated;
            public bool FlipX;
            public bool FlipY;
        }

        private static System.Diagnostics.Stopwatch s = new Stopwatch();

        public static void Run()
        {
            Position[] positions = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(x => new Position { X = x % Settings.GRID_SIZE, Y = x % Settings.GRID_SIZE })
                .ToArray();

            Console.WriteLine("Positions [] size: {0} MB", Marshal.SizeOf(typeof(Position)) * positions.Length / Settings.BYTES_IN_MB);

            Velocity[] velocities = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(x => new Velocity { X = x % Settings.MAX_VELOCITY + 1, Y = x % Settings.MAX_VELOCITY + 1 })
                .ToArray();

            Console.WriteLine("Velocities [] size: {0} MB", Marshal.SizeOf(typeof(Velocity)) * positions.Length / Settings.BYTES_IN_MB);

            BounceEvent[] bounces = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(x => new BounceEvent())
                .ToArray();

            Console.WriteLine("bounceEvents [] size: {0} MB", Marshal.SizeOf(typeof(BounceEvent)) * positions.Length / Settings.BYTES_IN_MB);

            s.Start();
            s.Restart();
            for (var i = 0; i < Settings.ITERATIONS; ++i)
            {
                ProcessMovement(positions, velocities, bounces);
                var numBounces = CountBounces(bounces);
                ProcessBounces(velocities, bounces);

                Console.WriteLine("Time {0} Bounces: {1}", s.ElapsedMilliseconds, numBounces);
            }
            s.Stop();

            Console.WriteLine("Average time per iteration: " + s.ElapsedMilliseconds / Settings.ITERATIONS);
        }

        private static void ProcessMovement(Position[] positions, Velocity[] velocities, BounceEvent[] bounces)
        {
            for (var i = 0; i < Settings.NUM_PARTICLES; ++i)
            {
                positions[i].X = positions[i].X + velocities[i].X;
                positions[i].Y = positions[i].Y + velocities[i].Y;

                if (positions[i].X < 0 || positions[i].X > Settings.GRID_SIZE)
                {
                    bounces[i].Updated = true;
                    bounces[i].FlipX = true;
                }
                else if (positions[i].Y < 0 || positions[i].Y > Settings.GRID_SIZE)
                {
                    bounces[i].Updated = true;
                    bounces[i].FlipY = true;
                }
            }
        }

        private static int CountBounces(BounceEvent[] bounces)
        {
            var numEvents = 0;
            for (var i = 0; i < Settings.NUM_PARTICLES; ++i)
            {
                if (bounces[i].FlipX)
                {
                    ++numEvents;
                }

                if (bounces[i].FlipY)
                {
                    ++numEvents;
                }
            }

            return numEvents;
        }

        private static void ProcessBounces(Velocity[] velocities, BounceEvent[] bounces)
        {
            for (var i = 0; i < Settings.NUM_PARTICLES; ++i)
            {
                if (!bounces[i].Updated)
                {
                    continue;
                }

                if (bounces[i].FlipX)
                {
                    bounces[i].FlipX = false;
                    velocities[i].X = -velocities[i].X;
                }

                if (bounces[i].FlipY)
                {
                    bounces[i].FlipY = false;
                    velocities[i].Y = -velocities[i].Y;
                }

                bounces[i].Updated = false;
            }
        }
    }

    public static class SimulateComponentsWithEventQueue
    {
        private struct BounceEventWithId
        {
            public int Id;
            public bool FlipX;
            public bool FlipY;
        }

        private static System.Diagnostics.Stopwatch s = new Stopwatch();

        public static void Run()
        {
            Position[] positions = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(pos => new Position { X = pos % Settings.GRID_SIZE, Y = pos % Settings.GRID_SIZE })
                .ToArray();

            Console.WriteLine("Positions [] size: {0} MB", Marshal.SizeOf(typeof(Position)) * positions.Length / Settings.BYTES_IN_MB);

            Velocity[] velocities = Enumerable.Range(0, Settings.NUM_PARTICLES)
                .Select(x => new Velocity { X = x % Settings.MAX_VELOCITY + 1, Y = x % Settings.MAX_VELOCITY + 1 })
                .ToArray();

            Console.WriteLine("Velocities [] size: {0} MB", Marshal.SizeOf(typeof(Velocity)) * positions.Length / Settings.BYTES_IN_MB);

            IList<BounceEventWithId> bounces = new List<BounceEventWithId>();

            s.Start();
            s.Restart();
            for (var i = 0; i < Settings.ITERATIONS; ++i)
            {
                ProcessMovement(positions, velocities, bounces);

                // Note: Bounce events should have been added in increasing order.

                ProcessBounces(velocities, bounces);

                Console.WriteLine("Time {0} Bounces: {1}", s.ElapsedMilliseconds, bounces.Count);

                bounces.Clear();
            }
            s.Stop();

            Console.WriteLine("Average time per iteration: " + s.ElapsedMilliseconds / Settings.ITERATIONS);
        }

        private static void ProcessMovement(Position[] positions, Velocity[] velocities, IList<BounceEventWithId> bounces)
        {
            for (var i = 0; i < Settings.NUM_PARTICLES; ++i)
            {
                positions[i].X = positions[i].X + velocities[i].X;
                positions[i].Y = positions[i].Y + velocities[i].Y;
                    
                if (positions[i].X < 0 || positions[i].X > Settings.GRID_SIZE)
                {
                    bounces.Add(new BounceEventWithId { Id = i, FlipX = true });
                }
                else if (positions[i].Y < 0 || positions[i].Y > Settings.GRID_SIZE)
                {
                    bounces.Add(new BounceEventWithId { Id = i, FlipY = true });
                }
            }
        }

        private static void ProcessBounces(Velocity[] velocities, IList<BounceEventWithId> bounces)
        {
            foreach(var bounceEvent in bounces)
            {
                if (bounceEvent.FlipX)
                {
                    velocities[bounceEvent.Id].X = -velocities[bounceEvent.Id].X;
                }
                else if (bounceEvent.FlipY)
                {
                    velocities[bounceEvent.Id].Y = -velocities[bounceEvent.Id].Y;
                }
            }
        }
    }

    public static class Program 
    {
        static void Main(string[] args)
        {
            GC.Collect();
            Console.WriteLine("\n[Press any key to start SimulateTraditionalOOPEntityComponent demo]");
            Console.ReadKey();

            SimulateTraditionalOOPEntityComponent.Run();

            GC.Collect();
            Console.WriteLine("\n[Press any key to start SimulateComponentsWithEventComponent demo]");
            Console.ReadKey();

            SimulateComponentsWithEventComponent.Run();

            GC.Collect();
            Console.WriteLine("\n[Press any key to start SimulateComponentsWithEventQueue demo]");
            Console.ReadKey();

            SimulateComponentsWithEventQueue.Run();

            Console.WriteLine("\n[Press any key to exit]");
            Console.ReadKey();
        }
    }
}
