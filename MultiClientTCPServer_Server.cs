using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

// Simple TCP Server
// Note: Must run as administrator in visual studio.

namespace MultiClientTCPServer_Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            const int PORT = 8000;
            var server = new Server(PORT, true);
            server.Start();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    public class Server 
    {
        int connectionAttempts = 0;
        const string ADDRESS = "127.0.0.1";

        TcpListener listener;

        public Server(int portNumber, bool localHost = false)
        {
            var address = (localHost) ? IPAddress.Parse("127.0.0.1") : GetLocalHostAddress();
            var endpoint = new IPEndPoint(address, portNumber);

            this.listener = new TcpListener(endpoint);
            Console.WriteLine("Listening on {0}:{1}", address, portNumber); 
        }

        private IPAddress GetLocalHostAddress()
        {
            var name = Dns.GetHostName();
            var resolvedEntry = Dns.GetHostEntry(name);

            if (resolvedEntry.AddressList.Count() == 0)
                throw new Exception("Unable to get network addresses for host.");

            foreach (var address in resolvedEntry.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return address;
            }

            throw new Exception("Unable to find IPv4 address for host.");
        }

        public void Start()
        {
            this.listener.Start();
            WaitForConnections();
        }

        private void WaitForConnections()
        {
            var callback = new AsyncCallback(Accept); 
            this.listener.BeginAcceptTcpClient(callback, new Object());
        }

        private void Accept(IAsyncResult result)
        {
            Interlocked.Increment(ref connectionAttempts);
            var client = listener.EndAcceptTcpClient(result);
            Console.WriteLine("Connection: {0} ", client.Client.LocalEndPoint);

            var clientRequest = new RequestHandler(client);
            clientRequest.Handle();
        }
    }

    internal class RequestHandler
    {
        TcpClient client;

        public RequestHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Handle()
        {
            var stream = client.GetStream();

            while(client.Connected)
            {
                if (stream.DataAvailable)
                {
                    try
                    {
                        var buffer = new byte[client.ReceiveBufferSize];
                        stream.BeginRead(buffer, 0, buffer.Length, ReadCallBack, buffer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception Occurred: {0}", ex.Message);
                        stream.Close();
                        client.Close();
                    }
                }
            }

            Console.WriteLine("Client Disconnected");
            stream.Close();
            client.Close();
        }

        public void ReadCallBack(IAsyncResult result)
        {
            var stream = client.GetStream();
            try
            {
                int bytesRead = stream.EndRead(result);
                if (bytesRead == 0)
                {
                    stream.Close();
                    client.Close();
                    return;
                }

                var buffer = result.AsyncState as byte[];
                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received: {0}", data);

                var sendData = DoWork(data);
                Console.WriteLine("Sent: {0}", sendData);

                var sendBytes = Encoding.UTF8.GetBytes(sendData);
                stream.Write(sendBytes, 0, sendBytes.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred: {0}", ex.Message);
            }
        }

        public String DoWork(String str)
        {
            return String.Concat("[", str, "] =", str.Length);
        }
    }
}
