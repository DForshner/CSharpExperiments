using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeWebServer
{
    public class Program
    {
        /// <summary>
        /// This has to be run as admin.
        /// </summary>
        public static void Main()
        {
            var serverTask = new ServerTask();

            var service = serverTask.Start(@"http://*:3000/");
            
            service.Wait();
        }
    }

    public class ServerTask
    {
        private readonly HttpListener listener = new HttpListener();

        public async Task Start(string address)
        {
            this.listener.Prefixes.Add(address);
            this.listener.Start();

            while (true)
            {
                HttpListenerContext context = await this.listener.GetContextAsync().ConfigureAwait(false);

                var response = GetResponseBasedOnURL(context);

                await context.Response.OutputStream.WriteAsync(ASCIIEncoding.ASCII.GetBytes(response), 0, response.Length);
                context.Response.Close();
            }
        }

        private string GetResponseBasedOnURL(HttpListenerContext context)
        {
            string methodName = GetMethodNameFromURL(context);

            if (methodName == string.Empty)     
                return Index();

            var method = this.GetType().GetMethod(methodName);

            if (method == null)
                return Error();

            return method.Invoke(this, null) as String;
        }

        private string GetMethodNameFromURL(HttpListenerContext context)
        {
            if (context.Request.Url.Segments.Count() <= 1)
                return string.Empty;

            return context.Request.Url.Segments[1].Replace("/", "");
        }

        public string Index()
        {
            return ReadFile("Index.html");
        }

        public string GetTableData()
        {
            return ReadFile("Data.txt");
        }

        public string Error()
        {
            return "Unknown URL";
        }

        private static string ReadFile(String fileName)
        {
            String line = string.Empty;

            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                    line = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return line;
        }
    }
}
