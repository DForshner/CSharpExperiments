using System;
using System.Collections.Generic;

// Given a expression consisting of the symbols 0,1,&,| and ^ and a desired result
// count the number of ways of parenthesizing the expression so it evaluates to result.
//
// Compiled: Visual Studio 2013

namespace ParenthesizingCombinations
{
    public static class Program
    {

        public static int GetCombinations(String exp, bool result)
        {
            var alreadyProcessed = new Dictionary<Tuple<bool, int, int>, int>();
            return Calc(exp, result, 0, (exp.Length - 1), alreadyProcessed); 
        }

        public static int Calc(String exp, bool result, int s, int e, IDictionary<Tuple<bool, int, int>, int> alreadyProc)
        {
            // Check if we have already processed this segment of the expression.
            if (alreadyProc.ContainsKey(Tuple.Create(result, s, e)))
                return alreadyProc[Tuple.Create(result, s, e)];

            int c = 0;

            // Base case
            if (s == e)
            {
                // f(1,T) = 1
                if (exp[s] == '1' && result) 
                    c = 1;
                // f(0,F) = 1
                else if (exp[s] == '0' && !result) 
                    c = 1;
                // f(1,F), f(0,T), F(&,), F(|,), F(^,) ... = 0
                else c = 0;
            }
            else if (result)
            {
                for (int i = s + 1; i <= e; i += 2)
                {
                    char op = exp[i];
                    // f(e1&e2,T) = f(e1,T) * f(e2,T)
                    if (op == '&')
                    {
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                    }
                    // f(e1|e2,T) = f(e1,T)*f(e2,T) + f(e1,T)*f(e2,F) + f(e1,F)*f(e2,T)
                    else if (op == '|')
                    {
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                    }
                    // f(e1^e2,T) = f(e1,T) * f(e2,F) + f(e1,F) * f(e2,T)
                    else if (op == '^')
                    {
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                    }
                }
            }
            else
            {
                for (int i = s + 1; i <= e; i += 2)
                {
                    char op = exp[i];
                    // f(e1|e2,T) = f(e1,F)*f(e2,F) + f(e1,T)*f(e2,F) + f(e1,F)*f(e2,T)
                    if (op == '&')
                    {
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                    }
                    // f(e1|e2,F) = f(e1,F)*f(e2,F)
                    else if (op == '|')
                    {
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                    }
                    // f(e1^e2,F) = f(e1,T) * f(e2,T) + f(e1,F) * f(e2,F)
                    else if (op == '^')
                    {
                        c += Calc(exp, true, s, i - 1, alreadyProc) * Calc(exp, true, i + 1, e, alreadyProc);
                        c += Calc(exp, false, s, i - 1, alreadyProc) * Calc(exp, false, i + 1, e, alreadyProc);
                    }
                }
            }

            //Console.WriteLine("f({0},{1}) = {2}", exp.Substring(s, (e - s + 1)), result, c);

            // Store results to avoid re-processing.
            alreadyProc.Add(Tuple.Create(result, s,e), c);

            return c;
        }

        public static void Main()
        {
            Console.WriteLine("f(1^0|0|1, True) = " + GetCombinations("1^0|0|1", true));

            Console.WriteLine("Press Enter to exit.");
            Console.ReadKey(true);
        }
    }
}