using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

//
// Login with forms authentication and access page.
//

namespace WebClientWithFormsAuthentication
{
    class Program
    {
        public class CookieAwareWebClient : WebClient
        {
            public CookieAwareWebClient()
            {
                CookieContainer = new CookieContainer();
            }
            public CookieContainer CookieContainer { get; private set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = (HttpWebRequest)base.GetWebRequest(address);
                request.CookieContainer = CookieContainer;
                return request;
            }
        }

        static void Main(string[] args)
        {
            using (var client = new CookieAwareWebClient())
            {
                Console.WriteLine("Authenticating");
                // Authenticate
                //var byteResponse = client.UploadValues("http://localhost/MyMvcApp/Account/Login", values);
                var byteResponse = client.DownloadData("http://localhost:54541/Account/Login");

                // Get anti-forgery token
                var responseString = Encoding.ASCII.GetString(byteResponse);

                //var reg = new Regex(@"name=""__RequestVerificationToken"" type=""hidden"" value=""([A-Za-z0-9+=/\-]+?)""");
                var reg = new Regex(@"__RequestVerificationToken"" type=""hidden"" value=""([A-Za-z0-9+=/\-_]+?)""");

                //var reg = new Regex("__RequestVerificationToken=(?<CRSF_Token>[^;]+)");
                var match = reg.Match(responseString);

                if (!match.Success)
                {
                    Console.WriteLine("Unable to acquire anti-forgery token.");
                    return;
                }

                var hiddenAntiForgeryToken = match.ToString();
                var startMarker = @"value="""; 
                var start = hiddenAntiForgeryToken.IndexOf(startMarker) + startMarker.Length;
                var end = hiddenAntiForgeryToken.LastIndexOf('"');
                var antiForgeryToken = hiddenAntiForgeryToken.Substring(start, (end - start));
                client.CookieContainer.Add(new Cookie("__RequestVerificationToken", antiForgeryToken, "/", "localhost"));

                client.ResponseHeaders["Set-Cookie"] = "CRSF_Token";

                var authCookie = client.CookieContainer.GetCookies(new Uri("http://localhost:54541/"));
                for (var i = 0; i < authCookie.Count; i++)
                {
                    var cookie = authCookie[i];
                    var CookieName = cookie.Name;
                    var CookieValue = cookie.Value;
                        
                    if (!String.IsNullOrEmpty(CookieName) && !String.IsNullOrEmpty(CookieValue))
                        client.CookieContainer.Add(new Cookie(CookieName, CookieValue, "/", "localhost"));
                }
                    
                Console.WriteLine("Logging in");
                var values = new NameValueCollection
                {
                    { "UserName", "john" },
                    { "Password", "secret" },
                };

                //client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                var byteResponse2 = client.UploadValues("http://localhost:54541/Account/Login/", values);
                var responseString2 = Encoding.ASCII.GetString(byteResponse2); //debugging

                Console.WriteLine("Accessing page");
                // If the previous call succeeded we now have a valid authentication cookie
                // so we could download the protected page
                string result = client.DownloadString("http://domain.loc/testpage.aspx");
            }
        }
    }
}
