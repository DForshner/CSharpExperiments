using System;
using System.Threading;

/// <summary>
/// Simple program that creates a ping and a pong thread which alternately display “Ping” and “Pong” on the console.
/// Uses lock and monitor objects to synchronise.
/// .NET Framework Version: 4.5
/// C# Compiler Version: 11.0 (Visual Studio 2012)
/// </summary>

namespace PingPongThreads
{
    public class Ping
    {
        private Match game;

        public Ping(Match game)
        {
            this.game = game;
        }

        public void Begin()
        {
            while (game.Ping());
        }
    };


    public class Pong
    {
        private Match game;

        public Pong(Match game)
        {
            this.game = game;
        }

        public void Begin()
        {
            while (game.Pong());
        }
    };

    public class Match
    {
        enum Moves { Ping, Pong };

        Moves lastMove = Moves.Pong;
        int maximumTurns;
        int currentTurn = 1;

        public Match(int maximumTurns)
        {
            this.maximumTurns = maximumTurns;
        }

        public bool Ping()
        {
            lock (this)
            {
                if (this.currentTurn >= this.maximumTurns)
                    return false;

                if (this.lastMove == Moves.Ping)
                    WaitForPong();

                Console.WriteLine("Ping");
                this.currentTurn++;
                this.lastMove = Moves.Ping;
                Monitor.Pulse(this); // Move waiting thread is moved to the ready queue.

                return true;
            }
        }

        private void WaitForPong()
        {
            try
            {
                Monitor.Wait(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public bool Pong()
        {
            lock (this)
            {
                if (this.currentTurn >= this.maximumTurns)
                    return false;

                if (this.lastMove == Moves.Pong)
                    WaitForPing();

                Console.WriteLine("Pong");
                this.currentTurn++;
                this.lastMove = Moves.Pong;
                Monitor.Pulse(this); // Move waiting thread is moved to the ready queue.

                return true;
            }
        }

        private void WaitForPing()
        {
            try
            {
                Monitor.Wait(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ready... Set... Go!");
            Console.WriteLine();

            var game = new Match(6);

            Thread pingThread = StartPingThread(game);

            Thread pongThread = StartPongThread(game);

            pingThread.Join(); // Wait until pingThread finishes.

            pongThread.Join(); // Wait until pongThread finishes.

            Console.WriteLine("Done!");
        }

        private static Thread StartPongThread(Match game)
        {
            var pong = new Pong(game);

            Thread pongThread = new Thread(new ThreadStart(pong.Begin));

            pongThread.Start();

            // Wait for thread to start
            while (!pongThread.IsAlive);

            return pongThread;
        }

        private static Thread StartPingThread(Match game)
        {
            var ping = new Ping(game);

            Thread pingThread = new Thread(new ThreadStart(ping.Begin));

            pingThread.Start();

            // Wait for thread to start
            while (!pingThread.IsAlive);

            return pingThread;
        }
    }
}
