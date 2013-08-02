using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// An echo server implemented with the:
/// Facade Design Pattern
/// Acceptor Design Pattern (acceptor-connector)
/// Half-Sync/Half-Async Design Pattern (Multi threaded (Sync) / Single threaded (Async) )
/// 
/// The Half-Sync/Half-Async design pattern allows synchronous longer duration/blocking tasks to be performed by a multiple
/// threads, while allowing lower level asynchronous non-blocking tasks to be performed by a single dedicated thread.
///
/// You can either start the server and run the ReactorEchoServerTestClient script or use telnet to interact with the server.
/// Ex: telnet localhost 3000
/// 
/// The server is setup to listen for events on ports 3000, 3001, and 3002.  It receives 
/// text until the enter key is pressed (\r\n) and then echoes the results back to the client.
/// 
/// .NET Framework Version: 4.5
/// C# Compiler Version: 11.0 (Visual Studio 2012)

namespace FacadeAcceptorHalfSyncHalfAsyncEchoServer
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
        byte[] Read();
        byte[] Write(byte[] data);
        byte[] Write(string data);
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
        public byte[] Read()
        {
            int bufferSize = 0;
            byte[] buffer = new byte[25];

            try
            {
                bufferSize = socket.Receive(buffer);

                if (bufferSize == 0)
                    return null;

                Array.Resize(ref buffer, bufferSize); // Truncates array to buffer size
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return null;
            }

            return buffer;
        }

        /// <summary>
        /// Helper method that converts a string to a byte[] before sending.
        /// </summary>
        public byte[] Write(string data)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            Byte[] bytes = encoding.GetBytes(data);

            return Write(bytes);
        }

        /// <summary>
        /// Returns null if the connection was closed.
        /// </summary>
        public byte[] Write(byte[] buffer)
        {
            int bufferSize = 0;

            try
            {
                bufferSize = socket.Send(buffer);

                if (bufferSize == 0)
                    return null;

                return buffer;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return null;
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
        void Close();
    }

    /// <summary>
    /// Implements the acceptor pattern which is used to abstract connection establishment details from the data receive event code.
    /// 
    /// When a client connection request arrives, the EchoReactor will automatically call the appropriate method
    /// of the EchoAcceptor to handle the input.
    /// </summary>
    public class EchoAcceptorHandler : IEventHandler
    {
        private readonly IReactor reactor;
        private ListenerWrapperFacade server;
        private IHalfSyncPool echoTask;

        public EchoAcceptorHandler(string ipAddress, int port, IReactor reactor, IHalfSyncPool echoTask)
        {
            this.server = new ListenerWrapperFacade(ipAddress, port);
            this.reactor = reactor;
            this.echoTask = echoTask;
        }

        /// <summary>
        /// Accepts the connection and registers an EchoServiceHandler in the EchoReactor.
        /// </summary>
        public void HandleEvent()
        {
            IConnectionWrapperFacade connection = server.Accept();

            var serviceHandler = new EchoServiceHandler(connection, reactor, echoTask);

            reactor.RegisterHandle(serviceHandler);
        }

        public bool CheckForEvents()
        {
            return server.CheckForEvents();
        }

        public void Close()
        {
            server.Close();
        }
    }

    /// <summary>
    /// An asynchronous event handler that performs the application specific logic for data receive events.
    /// In this case it asynchronously receives text until the message is identified (enter key is pressed \r\n) 
    /// and stores the results in the echoTasks message queue for later synchronous processing by a pool of threads.
    /// 
    /// Note: Because this is asynchronous a given message may require multiple trips through the reactor to 
    /// read each chunk of client data via a single non-blocking read() each time.
    /// </summary>
    public class EchoServiceHandler : IEventHandler
    {
        private readonly IReactor reactor;
        private readonly IConnectionWrapperFacade connection;
        private readonly IHalfSyncPool echoTask;
        private string currentMessage;

        public EchoServiceHandler(IConnectionWrapperFacade connection, IReactor reactor, IHalfSyncPool echoTask)
        {
            this.connection = connection;
            this.reactor = reactor;
            this.echoTask = echoTask;
        }

        public void HandleEvent()
        {
            byte[] data = connection.Read();

            if (data == null)
            {
                Close(); // Client has disconnected
                return;
            }

            currentMessage += Encoding.ASCII.GetString(data, 0, data.Length);

            if (CheckIfEnterKeyPressed())
            {
                echoTask.EnqueueMessage(new Message() { Text = currentMessage, Connection = this.connection, EventHandler = this });
                Console.WriteLine("Message enqueued: {0}", currentMessage);
                currentMessage = null;
            }
        }

        private bool CheckIfEnterKeyPressed()
        {
            return currentMessage.IndexOf("\r\n") > -1;
        }

        public bool CheckForEvents()
        {
            return connection.CheckForEvents();
        }

        public void Close()
        {
            connection.Close();
            reactor.RemoveHandle(this);
        }
    }

    public interface IReactor
    {
        void RegisterHandle(IEventHandler eventHandler);
        void RemoveHandle(IEventHandler eventHandler);
    }

    /// <summary>
    /// A single threaded asynchronous reactor has the main event loop that handles asynchronous events from the registered handles.
    /// This class performs the "half-async" portion of half-sync/half-async pattern.
    /// </summary>
    public class EchoReactor : IReactor
    {
        private readonly ISynchronousEventDemultiplexer _synchronousEventDemultiplexer;
        private readonly List<IEventHandler> _handlers;

        public EchoReactor(ISynchronousEventDemultiplexer synchronousEventDemultiplexer)
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
        /// The main event loop for the reactor's single thread of control.
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

    /// <summary>
    /// Message that gets stored on the blocking message queue.
    /// </summary>
    public class Message
    {
        public string Text { get; set; }
        public IConnectionWrapperFacade Connection { get; set; }
        public IEventHandler EventHandler { get; set; }
    }

    public interface IHalfSyncPool
    {
        void EnqueueMessage(Message message);
    }

    /// <summary>
    /// Performs the "half-sync" portion of the server.
    /// Initializes a number of threads and starts them running the thread loop.
    /// </summary>
    public class HalfSyncPool : IHalfSyncPool
    {
        BlockingMessageQueue blockingQueue = new BlockingMessageQueue();
        IList<Thread> threads = new List<Thread>();

        public HalfSyncPool(int threadPoolSize)
        {
            for (int i = 0; i < threadPoolSize; i++)
            {
                Thread t = new Thread(() => HalfSyncPool.ThreadLoop(this.blockingQueue));
                t.Start();
                threads.Add(t);
            }
        }

        /// <summary>
        /// If there are messages available the thread dequeues the message containing the client input that were put 
        /// into the synchronized request queue by the ‘half-async’ reactor.  If there are no messages the thread will block until
        /// new messages are stored on the blocking queue.
        /// 
        /// The thread then sends the thread id of the server thread handling the request and the original client input back to the client.
        /// If the client disconnects before the send operation can take place the event handler is closed down.
        /// </summary>
        public static void ThreadLoop(BlockingMessageQueue blockingQueue)
        {
            while (true)
            {
                var message = blockingQueue.BlockingDequeue();

                string result = "Thread ID: " + Thread.CurrentThread.ManagedThreadId.ToString() + " Text: " + message.Text;

                Console.WriteLine(result);
                var sentBytes = message.Connection.Write(result);

                if (sentBytes == null)
                    message.EventHandler.Close(); // Client has disconnected;
            }
        }

        /// <summary>
        /// Stores message on the half sync pools message queue.
        /// </summary>
        public void EnqueueMessage(Message message)
        {
            blockingQueue.Enqueue(message);
        }
    }

    /// <summary>
    /// Simple blocking message queue to move messages that is used to move messages from the async to the sync layer.
    /// </summary>
    public class BlockingMessageQueue
    {
        private object lockObject = new object();
        private List<Message> messageQueue = new List<Message>();

        /// <summary>
        /// Stores message on the end of the queue.
        /// </summary>
        public void Enqueue(Message message)
        {
            lock (lockObject)
            {
                messageQueue.Add(message);
                Monitor.PulseAll(lockObject);
            }
        }

        /// <summary>
        /// If messages exists it removes the first message from queue.  Otherwise it blocks the calling thread.
        /// </summary>
        public Message BlockingDequeue()
        {
            lock (lockObject)
            {
                while (messageQueue.Count == 0)
                    Monitor.Wait(lockObject);

                Message message = messageQueue.FirstOrDefault();

                if (message == null)
                    throw new Exception("Error: Blocking queue is empty");

                messageQueue.Remove(message);

                return message;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            const int NUMBER_OF_THREADS_TO_SPAWN = 5;

            var synchronousEventDemultiplexer = new SynchronousEventDemultiplexer();
            var echoReactor = new EchoReactor(synchronousEventDemultiplexer);

            // Create an object EchoTasks that spawns a pool of some number of threads ( > 1).      
            var echoTasks = new HalfSyncPool(NUMBER_OF_THREADS_TO_SPAWN);

            // Creates an EchoAcceptor instance and associate it with the EchoTasks.  
            var echoAcceptor1 = new EchoAcceptorHandler("127.0.0.1", 3000, echoReactor, echoTasks);
            var echoAcceptor2 = new EchoAcceptorHandler("127.0.0.1", 3001, echoReactor, echoTasks);
            var echoAcceptor3 = new EchoAcceptorHandler("127.0.0.1", 20002, echoReactor, echoTasks);

            // Registers the EchoAcceptor instance with the EchoReactor
            echoReactor.RegisterHandle(echoAcceptor1);
            echoReactor.RegisterHandle(echoAcceptor2);
            echoReactor.RegisterHandle(echoAcceptor3);

            // Run the reactor's event loop to wait for connections/data to arrive from a client.
            echoReactor.HandleEvents();
        }
    }

}