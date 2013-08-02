using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// Test client for the FacadeReactorAcceptorEchoServer
/// Note: Can also test FacadeAcceptorHalfSyncHalfAsyncEchoServer
///  
/// First it starts three threads that connect on ports 3000, 3001, and 3002.
/// Each thread sends a line (text + enter key) to the server and waits for it
/// to be echoed back.  It does this 5 times per thread to test concurrency.
///
/// .NET Framework Version: 4.5
/// C# Compiler Version: 11.0 (Visual Studio 2012)

namespace FacadeReactorAcceptorEchoServerTestClient
{
    public class TestClient
    {
        private IPEndPoint serverEndPoint;
        private TcpClient client;
        private ASCIIEncoding encoder;
        private NetworkStream stream;

        public TestClient(IPEndPoint serverEndPoint)
        {
            this.client = new TcpClient();
            this.encoder = new ASCIIEncoding();
            this.serverEndPoint = serverEndPoint;
     
            client.Connect(serverEndPoint);

            Console.WriteLine("Connected client: {0}, server: {1}", client.Client.LocalEndPoint.ToString(), client.Client.RemoteEndPoint.ToString());

            this.stream = client.GetStream();
        }

        public void StartTest()
        {
            for (int i = 0; i <= 10; i++)
            {
                SendTestDataToServer(i, stream, encoder);
                ReceiveEchoedResponseFromServer(i, stream);
            }

            client.Close();
        }

        private void SendTestDataToServer(int testNumber, NetworkStream clientStream, ASCIIEncoding encoder)
        {
            string sentData = String.Format("This is test {0} for client: {1}\r\n", testNumber.ToString(), client.Client.LocalEndPoint.ToString());       
            byte[] buffer = encoder.GetBytes(sentData);

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        private static void ReceiveEchoedResponseFromServer(int testNumber, NetworkStream clientStream)
        {
            string receivedData = null;
            var sendBuffer = new Byte[200];
            int bytesRec;

            while (true)
            {
                bytesRec = clientStream.Read(sendBuffer, 0, sendBuffer.Length);
                receivedData += Encoding.ASCII.GetString(sendBuffer, 0, bytesRec);
                if (CheckIfEnterKeyPressed(receivedData))
                    break;
            }

            Console.WriteLine("Test {0} - echoed results: {0}", testNumber.ToString(), receivedData);
        }

        private static bool CheckIfEnterKeyPressed(string receivedData)
        {
            return receivedData.IndexOf("\r\n") > -1;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");

            Thread client1Thread = StartClientThread(new IPEndPoint(localIPAddress, 3000));
            Thread client2Thread = StartClientThread(new IPEndPoint(localIPAddress, 3000));
            Thread client3Thread = StartClientThread(new IPEndPoint(localIPAddress, 3000));

            Thread client4Thread = StartClientThread(new IPEndPoint(localIPAddress, 3001));
            Thread client5Thread = StartClientThread(new IPEndPoint(localIPAddress, 3001));
            Thread client6Thread = StartClientThread(new IPEndPoint(localIPAddress, 3001));

            client1Thread.Join();
            client2Thread.Join();
            client3Thread.Join();

            client4Thread.Join();
            client5Thread.Join();
            client6Thread.Join();
        }

        private static Thread StartClientThread(IPEndPoint serverEndPoint)
        {
            var testClient1 = new TestClient(serverEndPoint);
            var clientThread = new Thread(new ThreadStart(testClient1.StartTest));
            clientThread.Start();
            return clientThread;
        }


    }
}
