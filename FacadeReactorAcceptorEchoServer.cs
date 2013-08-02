using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// An echo server implemented using the facade, reactor, and acceptor (acceptor-connector) design patterns.
///
/// The reactor design pattern allows a simple coarse-grain concurrency by handling
/// multiple connection request events and data receive events with a single thread.
///
/// You can either start the server and run the ReactorEchoServerTestClient script or use telnet to interact with the server.
/// Ex: telnet localhost 3000
/// 
/// The server is setup to listen for events on ports 3000, 3001, and 3002.  It receives 
/// text until the enter key is pressed (\r\n) and then echoes the results back to the client.
/// 
/// .NET Framework Version: 4.5
/// C# Compiler Version: 11.0 (Visual Studio 2012)

namespace FacadeReactorAcceptorEchoServer
{
    public interface IListenerWrapperFacade
    {
        IConnectionWrapperFacade Accept();
        bool CheckForEvents();
        void Close();
    }

    /// <summary>
    /// Implements the wrapper facade design pattern to abstract out OS specific network implementation details of listening and accepting incoming connections.
    /// </summary>
    public class ListenerWrapperFacade : IListenerWrapperFacade
    {
        private readonly Socket listener;

        public ListenerWrapperFacade(string ipAddress, int port)
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listener.Bind(ipEndPoint);
            this.listener.Listen(10);
            Console.WriteLine("Server: {0} - Listening for incoming connections.", listener.LocalEndPoint.ToString());
        }

        public IConnectionWrapperFacade Accept()
        {
            var socket = listener.Accept();
            Console.WriteLine("Client: {0}, Server: {1} - Accepted client connection.", socket.LocalEndPoint.ToString(), socket.RemoteEndPoint.ToString());

            return new ConnectionWrapperFacade(socket);
        }

        public bool CheckForEvents()
        {
            return this.listener.Poll(10, SelectMode.SelectRead);
        }

        public void Close()
        {
            Console.WriteLine("Server: {1} - No longer listening for incoming connections.", listener.LocalEndPoint.ToString());
            listener.Close();
        }
    }

    public interface IConnectionWrapperFacade
    {
        string Read();
        void Write(string data);
        bool CheckForEvents();
        void Close();
    }

    /// <summary>
    /// Implements the wrapper facade design pattern to abstract out OS specific network implementation details of communicating with connected clients.
    /// </summary>
    public class ConnectionWrapperFacade : IConnectionWrapperFacade
    {
        private readonly Socket socket;

        public ConnectionWrapperFacade(Socket socket)
        {
            this.socket = socket;
        }

        /// <summary>
        /// Returns null if the connection was closed.
        /// </summary>
        public string Read()
        {
            int bufferSize = 0;
            byte[] buffer = new byte[25];

            try
            {
                bufferSize = socket.Receive(buffer);

                if (bufferSize == 0)
                {
                    Close();
                    return null;
                }
            }
            catch (Exception e)
            {
                Close();
                Console.WriteLine("Exception: " + e.Message);
            }

            return Encoding.ASCII.GetString(buffer, 0, bufferSize);
        }

        public void Write(string data)
        {
            try
            {
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] buffer = encoder.GetBytes(data);

				// TODO: Need to check for the race condition where the client disconnects before we can send
				// Check if socket.Send returned zero length.
                socket.Send(buffer);
            }
            catch (Exception e)
            {
                Close();
                Console.WriteLine("Exception: " + e.Message);
            }
        }

        public void Close()
        {
            Console.WriteLine("Server - Client closed connection.");
            socket.Close();
        }

        public bool CheckForEvents()
        {
            return socket.Poll(10, SelectMode.SelectRead);
        }
    }

    public interface IEventHandler
    {
        void HandleEvent();
        bool CheckForEvents();
    }

    /// <summary>
    /// Implements the acceptor design pattern to abstract connection establishment details out data event code.
    /// </summary>
    public class AcceptorEventHandler : IEventHandler
    {
        private readonly IReactor reactor;
        private ListenerWrapperFacade server;

        public AcceptorEventHandler(string ipAddress, int port, IReactor reactor)
        {     
            this.server = new ListenerWrapperFacade(ipAddress, port);
            this.reactor = reactor;
        }

        public void HandleEvent()
        {
            IConnectionWrapperFacade connection = server.Accept();

            var serviceHandler = new ServiceEventHandler(connection, reactor);

            reactor.RegisterHandle(serviceHandler);
        }

        public bool CheckForEvents()
        {
            return server.CheckForEvents();
        }
    }

    /// <summary>
    /// An asynchronous event handler that performs the application specific logic for data receive events.
    /// In this case it will receive text until the enter key is pressed (\r\n) and
    /// then echo the results back to the client.
    /// </summary>
    public class ServiceEventHandler : IEventHandler
    {
        private readonly IReactor reactor;
        private readonly IConnectionWrapperFacade connection;
        private string lineData;

        public ServiceEventHandler(IConnectionWrapperFacade connection, IReactor reactor)
        {
            this.connection = connection;
            this.reactor = reactor;
        }

        public void HandleEvent()
        {
            string data = connection.Read();

            if (data == null)
            {
                reactor.RemoveHandle(this);
                return;
            }

            lineData += data;
            Console.WriteLine("Text received: {0}", data);

            // Echo the line back if enter key detected
            if (CheckIfEnterKeyPressed())
            {
                connection.Write(lineData);
                lineData = null;
                return;
            }
        }

        private bool CheckIfEnterKeyPressed()
        {
            return lineData.IndexOf("\r\n") > -1;
        }

        public bool CheckForEvents()
        {
            return connection.CheckForEvents();
        }
    }

    public interface ISynchronousEventDemultiplexer
    {
        IEnumerable<IEventHandler> Select(IEnumerable<IEventHandler> handlers);
    }

    /// <summary>
    /// Event de-multiplexor returns handles that have events waiting to the reactor's event handler loop.
    /// </summary>
    public class SynchronousEventDemultiplexer : ISynchronousEventDemultiplexer
    {
        public IEnumerable<IEventHandler> Select(IEnumerable<IEventHandler> handlers)
        {
            var handlersWithEvents = new List<IEventHandler>();

            foreach (var handler in handlers)
            {
                if (handler.CheckForEvents())
                    handlersWithEvents.Add(handler);
            }

            return handlersWithEvents;
        }
    }

    public interface IReactor
    {
        void RegisterHandle(IEventHandler eventHandler);
        void RemoveHandle(IEventHandler eventHandler);
    }

    /// <summary>
    /// A single threaded asynchronous reactor has the main event loop that handles asynchronous events from the registered handles.
    /// </summary>
    public class Reactor : IReactor
    {
        private readonly ISynchronousEventDemultiplexer _synchronousEventDemultiplexer;
        private readonly List<IEventHandler> _handlers;

        public Reactor(ISynchronousEventDemultiplexer synchronousEventDemultiplexer)
        {
            _synchronousEventDemultiplexer = synchronousEventDemultiplexer;
            _handlers = new List<IEventHandler>();
        }

        public void RegisterHandle(IEventHandler eventHandler)
        {
            _handlers.Add(eventHandler);
        }

        public void RemoveHandle(IEventHandler eventHandler)
        {
            _handlers.Remove(eventHandler);
        }

        /// <summary>
        /// The main event loop for the reactor's thread of control.
        /// </summary>
        public void HandleEvents()
        {
            while (true)
            {
                IEnumerable<IEventHandler> handlersWithEvents = _synchronousEventDemultiplexer.Select(this._handlers);

                foreach (IEventHandler handler in handlersWithEvents)
                    handler.HandleEvent();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ISynchronousEventDemultiplexer synchronousEventDemultiplexer = new SynchronousEventDemultiplexer();

            var dispatcher = new Reactor(synchronousEventDemultiplexer);

            IEventHandler acceptHandler = new AcceptorEventHandler("127.0.0.1", 3000, dispatcher);
            IEventHandler acceptHandler2 = new AcceptorEventHandler("127.0.0.1", 3001, dispatcher);
            IEventHandler acceptHandler3 = new AcceptorEventHandler("127.0.0.1", 3002, dispatcher);

            dispatcher.RegisterHandle(acceptHandler);
            dispatcher.RegisterHandle(acceptHandler2);
            dispatcher.RegisterHandle(acceptHandler3);

            dispatcher.HandleEvents();
        }
    }

}