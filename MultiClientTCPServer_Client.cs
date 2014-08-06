using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Simple TCP Server

namespace MultiClientTCPServer_Client
{
    public class Program
    {
        const int PORT = 8000;
        const string ADDRESS = "127.0.0.1";

        static void Test(int clientId)
        {
            var client = new Client(ADDRESS, PORT);
    
            for (var i = 0; i < 100; i++)
            {
                client.Send(String.Format("{0} - Test #{0} from client {1}", clientId, i));
                var recv = client.Receive();
                Console.WriteLine("{0} - Received: {1}", clientId, recv);
                Thread.Sleep(100);
            }

        }

        static void Main(string[] args)
        {
            // Give time for the server to start up when both are in same VS solution.
            Thread.Sleep(1000);

            const int NUM_CLIENTS = 100;

            // Start thread per client
            var threads = new List<Thread>(NUM_CLIENTS);
            for (var i = 0; i < NUM_CLIENTS; i++)
            {
                var thread = new Thread(() => Test(i));
                thread.Start();
                threads.Add(thread);
            }

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    public class Client
    {
        TcpClient client;

        public Client(String address, int portNumber)
        {
            this.client = new TcpClient();
            var ipAddress = IPAddress.Parse(address);
            var endPoint = new IPEndPoint(ipAddress, portNumber);
            try
            {
                client.Connect(endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred: {0}", ex.Message);
                client.Close();
            }
        }

        public void Send(String sendData)
        {
            if (sendData == null) { return; }
            if (!client.Connected) { return; }

            var sendBytes = Encoding.UTF8.GetBytes(sendData);
            var stream = client.GetStream();
            stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();
        }

        public void Close()
        {
            client.Close();
        }

        public String Receive()
        {
            if (!client.Connected) { return null; }

            var stream = client.GetStream();
            var sb = new StringBuilder();
            for (var i = 0; i < 10; i++)
            {
                if (stream.DataAvailable)
                {
                    var buffer = new byte[client.ReceiveBufferSize];
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    var recvString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("-> {0}", recvString);
                    sb.Append(recvString);
                }
            }

            return sb.ToString();
        }
    }
}
