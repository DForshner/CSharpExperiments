using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// This doesn't work but I'll hopefully take another look someday.  
// It gets down to a few failing clauses but never seems to reach zero.
//

// Solving the 2SAT problem using Papadimitriou's random walk algorithm.
//
// The graph is stored in the following format:
// [number_of_variables_and_clauses]
// [clause_1_variable_1] [clause_1_variable_2]
// [clause_2_variable_1] [clause_2_variable_2]

namespace Solve2SATWithPapadimitriouRandomWalk
{
    public class Clause
    {
        public readonly int A;
        public readonly int B;

        public Clause(int var1, int var2)
        {
            this.A = var1;
            this.B = var2;
        }

        public bool Evaluate(bool val1, bool val2)
        {
            if (A < 0)
            {
                val1 = !val1;
            }

            if (B < 0)
            {
                val2 = !val2;
            }

            return val1 || val2;
        }
    }

    public class PapadimitriouRandomWalk2SAT
    {
     
        private static bool CheckIfSatisfiable(IReadOnlyList<Clause> clauses)
        {
            var rnd = new Random(DateTime.Now.Millisecond);

            var maxIterations1 = Math.Log(clauses.Count) / Math.Log(2);
            Debug.Assert(maxIterations1 > 0);

            var maxLocalIterations = 2L * (long)clauses.Count * (long)clauses.Count;
            Debug.Assert(maxLocalIterations > 0);

            for (var trial = 0; trial <= maxIterations1; trial++)
            {
                // Try another solution
                var solution = CreateRandomSolution(clauses.Count + 1);
                Console.WriteLine("Trying new solution");

                var stuck = 0;
                for (var localTrial = 0; localTrial < maxLocalIterations; localTrial++)
                {
                    //var failingClauses = GetClausesThatFailCurrentSolution(clauses, solution);

                    var failing = new ConcurrentBag<int>();
                    Parallel.For(1, clauses.Count, (i) =>
                    {
                        var clause = clauses[i];
                        var a = solution[Math.Abs(clause.A)];
                        var b = solution[Math.Abs(clause.B)];
                        if (!clause.Evaluate(a, b))
                        {
                            failing.Add(i);
                        }
                    });
                    var failingClauses = failing.ToList();

                    if (!failingClauses.Any())
                    {
                        return true;
                    }
                    else if (failingClauses.Count == 1 && stuck++ > 100)
                    {
                        Console.Write("Giving up");
                        break;
                    }

                    var randomFailingIndex = rnd.Next(failingClauses.Count);
                    var randomFailingClause = clauses[failingClauses[randomFailingIndex]];
                    //Debug.Assert(!randomFailingClause.Evaluate(solution[Math.Abs(randomFailingClause.A)], solution[Math.Abs(randomFailingClause.B)]));

                    // Flip either the A or B variable 
                    if (rnd.Next(0, 2) == 0)
                    {
                        var a = Math.Abs(randomFailingClause.A);
                        solution[a] = !solution[a];
                    }
                    else
                    {
                        var b = Math.Abs(randomFailingClause.B);
                        solution[b] = !solution[b];
                    }
                }
            }

            return false;
        }

        private static List<int> GetClausesThatFailCurrentSolution(IReadOnlyList<Clause> clauses, bool[] solution)
        {
            var failing = new ConcurrentBag<int>();
            Parallel.For(1, clauses.Count, (i) =>
            {
                var clause = clauses[i];
                var a = solution[Math.Abs(clause.A)];
                var b = solution[Math.Abs(clause.B)];
                if (!clause.Evaluate(a, b))
                {
                    failing.Add(i);
                }
            });

            return failing.ToList();
        }

        private static bool[] CreateRandomSolution(int solutionSize)
        {
            var Rnd = new Random(DateTime.Now.Millisecond);
            var solution = new bool[solutionSize];
            for (var i = 1; i < solution.Count(); i++)
            {
                solution[i] = Rnd.Next(2) > 0;
            }
            return solution;
        }

        private static IReadOnlyList<Clause> ReadClausesFromFile(string fileName)
        {
            var data = ReadFile(fileName);
            var lines = data.Split('\n');

            var header = lines[0];
            var numberOfClauses = Int32.Parse(header);

            var clauses = new List<Clause>();
            for (var i = 1; i < lines.Count(); i++)
            {
                var line = lines[i];

                // Skip empty lines
                if (String.IsNullOrEmpty(line)) { continue; }

                var columns = line.Split(' ');
                var var1 = Int32.Parse(columns[0]);
                var var2 = Int32.Parse(columns[1]);

                clauses.Add(new Clause(var1, var2));
            }

            if (numberOfClauses != clauses.Count)
            {
                throw new Exception("The number of clauses read from file didn't match number of cities specified in file header.");
            }

            return clauses;
        }

        private static string ReadFile(string fileName)
        {
            const string PATH = "../../../";
            try
            {
                using (var sr = new StreamReader(PATH + fileName ))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return null;
        }

        public static void Main(string[] args)
        {
            // 1 = true
            // 2 = false?
            // 3 = false?

            for (var i = 4; i <= 6; i++)
            {
                var fileName = "2sat" + i.ToString() + ".txt";
                Console.WriteLine("file: " + fileName + " - Start");
                var canBeSatisfied = CheckIfSatisfiable(ReadClausesFromFile(fileName));
                Console.WriteLine(fileName + " - Result: " + canBeSatisfied);
            }

            Console.WriteLine("[Press any key to exit]");
            Console.ReadKey();
        }
    }
}
