using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// Convert an infix string expression to a postfix (reverse polish notation) expression
// using Edsger Dijkstra's shunting yard algorithm.
// Note: This could also be achieved by a post-order traversal of an expression tree. 

namespace InfixToPostfixNotationUsingShuntingYard
{
    public class ExpressionConverter 
    {
        private static string allowed = "[a-z0-9-" + Regex.Escape(@" ()^*\+") + "]*";

        /// <summary>
        /// Converts infix expression to a postfix expression. 
        /// </summary>
        /// <param name="infixExpression">Accepts operators: ^+-*/ operands: a-z </param>
        /// <returns>Postfix expression</returns>
        public static string ToPostfix(string infixExpression)
        {
            if (infixExpression == null) { throw new ArgumentNullException(); }
            if (Regex.Match(infixExpression, allowed).Length != infixExpression.Length)
            {
                throw new ArgumentException("Invalid character present in infix expression.");
            }

            var postfixExpression = new StringBuilder();
            var stack = new Stack<char>();

            // Break into single character tokens
            var tokens = infixExpression.Select(c => c);

            // Read infix expression from left to right
            foreach(var token in tokens) 
            {
                if (token == ' ')
                {
                    // Common case: Ignore spaces
                }
                else if (IsOperator(token))
                {
                    // Pop all operators which are higher or equal precedence and append them to expression
                    while (stack.Any() && Priority(stack.Peek()) >= Priority(token))
                    {
                        postfixExpression.Append(stack.Pop());
                    }
                    stack.Push(token);
                } 
                else if (token == '(')
                {
                    stack.Push(token);
                }
                else if (token == ')')
                {
                    // Pop all operators and operands until an opening parenthesis is reached
                    while (stack.Any() && stack.Peek() != '(')
                    {
                        postfixExpression.Append(stack.Pop());
                    }
                    stack.Pop();  // Remove '(' from stack
                }
                else // operand
                {
                    postfixExpression.Append(token);
                }
            }

            // Pop any remaining operators from the stack
            while(stack.Any())
            {
                postfixExpression.Append(stack.Pop());
            }

            return postfixExpression.ToString();
        }

        /// <summary>
        /// Could also use a static dictionary.
        /// </summary>
        private static int Priority(char token)
        {
            switch (token)
            {
                case '^':
                    return 3;
                    break;
                case '/':
                    return 2;
                    break;
                case '*':
                    return 2;
                    break;
                case '+':
                    return 1;
                    break;
                case '-':
                    return 1;
                    break;
                default:
                    return 0;
            }
        }

        private static bool IsOperator(char token)
        {
            return token == '+' || token == '-' || token == '*' || token == '/' || token == '^';
        }
    }

    [TestClass]
    public class ConverterTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhenNullInput_ExpectException()
        {
            ExpressionConverter.ToPostfix(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void WhenInvalidOperand_ExpectException()
        {
            ExpressionConverter.ToPostfix("A");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void WhenInvalidOperator_ExpectException()
        {
            ExpressionConverter.ToPostfix("%");
        }

        [TestMethod]
        public void WhenValidInfix_ExprectCorrectPostfix()
        {
            var testCases = new[]
            {
                new { Value = "a + b", Expected = "ab+" },
                new { Value = "a - b", Expected = "ab-" },
                new { Value = "(a - b)", Expected = "ab-" },
                new { Value = "a+b*c-d", Expected = "abc*+d-" },
                new { Value = "(a - b) * c", Expected = "ab-c*" },
            };

            foreach (var testCase in testCases)
            {
                var result = ExpressionConverter.ToPostfix(testCase.Value);
                Assert.AreEqual(testCase.Expected, result); 
            }
        }
    }
}