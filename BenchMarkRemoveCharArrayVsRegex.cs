using System;
using System.Collections.Generic;
using System.Linq;

// Compare removing a character using Array.FindAll vs. a Regex Replace
//
// Compiled: C# Visual Studio 2013

namespace TernarySearchTree 
{
	public static class Program
	{
		private const int SAMPLES = 500000;

		public static void ArrayRemoveCharTest()
		{
			var results = new List<String>(SAMPLES);
			var rnd = new Random();

			var sp = Stopwatch.StartNew();
			for (int i = 0; i < SAMPLES ; i++)
			{
				var str = rnd.Next(0, int.MaxValue).ToString();
				var result = new string(Array.FindAll(str.ToArray(), x => x != '1'));
				results.Add(result);
			}
			Console.WriteLine(sp.Elapsed);
		}

		private static Regex RemoveOneRgx = new Regex("1"); 

		public static void RegexRemoveCharTest()
		{
			var results = new List<String>(SAMPLES);
			var rnd = new Random();

			var sp = Stopwatch.StartNew();
			for (int i = 0; i < SAMPLES ; i++)
			{
				var str = rnd.Next(0, int.MaxValue).ToString();
				var result = RemoveOneRgx.Replace(str, "");
				results.Add(result);
			}
			Console.WriteLine(sp.Elapsed);
		}

		public static void Main()
		{
			GC.Collect(3, GCCollectionMode.Forced, true);
			GC.WaitForPendingFinalizers();

			ArrayRemoveCharTest();
			RegexRemoveCharTest();

			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
		}
	}