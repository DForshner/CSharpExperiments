/// <summary>
/// Quick test of the speed of joining two strings using Format, Concat, and Join
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        for (var i = 0; i < 5; i++)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            string str1 = null, str2 = null, str3 = null;

            for (int j = 0; j < 1000000; j++)
            {
                str1 = null;
                str1 = string.Format("{0}${1}", "ThisIsA", "Test");
            }
            var a = sw.ElapsedMilliseconds;

            for (int j = 0; j < 1000000; j++)
            {
                str2 = null;
                str2 = string.Concat("ThisIsA", "$", "Test");
            }
            var b = sw.ElapsedMilliseconds;

            for (int j = 0; j < 1000000; j++)
            {
                str3 = null;
                str3 = string.Join("&", "ThisIsA", "Test");
            }

            var c = sw.ElapsedMilliseconds;

            System.Console.WriteLine("Format | Concat |  Join ");
            System.Console.WriteLine(a + "  |  " + (b - a) + "  |  " + (c - b));
            System.Console.WriteLine(str1 + "  |  " + str2 + "  |  " + str3);
        }
    }
}