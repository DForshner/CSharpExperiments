using Akka.Actor;
using System;
using System.Linq;

// Hello world using Akka Actors
// Compiled: Visual Studio 2013
// Requires: Akka.NET Nuget package

namespace AkkaActorHelloWorld
{
    public class AkkaActorHelloWorld
    {
        public class ProgramStarted
        {
            public string UserName { get; private set; }

            public ProgramStarted(string username)
            {
                UserName = username;
            }
        }

        public class SetOutput
        {
            public ActorRef Output { get; private set; }
            public ConsoleColor Color { get; private set; }

            public SetOutput(ActorRef output, ConsoleColor color)
            {
                Output = output;
                Color = color;
            }
        }

        public class HelloActor : ReceiveActor
        {
            private ConsoleColor _color = ConsoleColor.Red;
            private ActorRef _output;

            public HelloActor()
            {
                Receive<SetOutput>(x =>
                    {
                        _output = x.Output; 
                        _color = x.Color;

                    });
                        
                Receive<ProgramStarted>(x =>  { _output.Tell(new WriteToConsole("Hello " + x.UserName, _color)); });
            }
        }

        public class WriteToConsole
        {
            public ConsoleColor Color {get; private set; }
            public string Message { get; private set; }

            public WriteToConsole(string message, ConsoleColor color)
            {
                Color = color;
                Message = message;
            }
        }

        public class ConsoleActor : ReceiveActor
        {
            public ConsoleActor()
            {
                Receive<WriteToConsole>(x =>
                    {
                        Console.ForegroundColor = x.Color;
                        Console.WriteLine("Hello {0}", x.Message);
                    });
            }
        }

        public static void Main()
        {
            var system = ActorSystem.Create("HelloWorldManager");

            var consoleActor = system.ActorOf<ConsoleActor>("actor0");

            var helloActor1 = system.ActorOf<HelloActor>("actor1");
            helloActor1.Tell(new SetOutput(consoleActor, ConsoleColor.Blue));

            var helloActor2 = system.ActorOf<HelloActor>("actor2");
            helloActor2.Tell(new SetOutput(consoleActor, ConsoleColor.Green));

            var helloActor3 = system.ActorOf<HelloActor>("actor3");
            helloActor3.Tell(new SetOutput(consoleActor, ConsoleColor.Gray));

            var helloActor4 = system.ActorOf<HelloActor>("actor4");
            helloActor4.Tell(new SetOutput(consoleActor, ConsoleColor.White));

            foreach (var i in Enumerable.Range(1, 100))
            {
                helloActor1.Tell(new ProgramStarted("Blue World"));
                helloActor2.Tell(new ProgramStarted("Green User"));
                helloActor3.Tell(new ProgramStarted("Gray Dog"));
                helloActor4.Tell(new ProgramStarted("White Cat"));
            }

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);

            Console.ReadKey(true);
        }
    }
}