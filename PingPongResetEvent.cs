using System;
using System.Threading;

namespace PingPongResetEvent
{
    public class Match
    {
        public int Rounds { get; set; }
        public Player Player1 { get; private set; }
        public Player Player2 { get; private set; }

        public Match(Player player1, Player player2, int rounds)
        {
            this.Player1 = player1;
            this.Player2 = player2;
            this.Rounds = rounds;
        }

        public void Begin()
        {
            var player1Thread = new Thread(new ThreadStart(() => { Player1.SetupMatch(this, Player2); }));
            var player2Thread = new Thread(new ThreadStart(() => { Player2.SetupMatch(this, Player1); }));

            player1Thread.Start();
            player2Thread.Start();

            var completeActions = new ManualResetEvent[] { Player1.GameComplete, Player2.GameComplete };

            Player1.Start();

            WaitHandle.WaitAll(completeActions);
        }
    }

    public class Player
    {
        private string _message;
        public AutoResetEvent MoveComplete { get; private set; }
        public ManualResetEvent GameComplete { get; private set; }

        public Player(string message)
        {
            this._message = message;
            this.MoveComplete = new AutoResetEvent(false);
            this.GameComplete = new ManualResetEvent(false);
        }

        public void Start()
        {
            MoveComplete.Set();
        }

        public void SetupMatch(Match match, Player opponent)
        {
            while (match.Rounds > 1)
            {
                opponent.MoveComplete.WaitOne();

                Console.WriteLine(_message + "(" + match.Rounds.ToString() + ")");

                match.Rounds--;

                MoveComplete.Set();
            }

            GameComplete.Set();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var player1 = new Player("Pong!");
            var player2 = new Player("Ping!");
            var match = new Match(player1, player2, 3);

            Console.WriteLine("Ready...Set...Go!");

            match.Begin();

            Console.WriteLine("Done!");
        }
    }
}