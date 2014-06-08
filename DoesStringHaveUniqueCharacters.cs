using System;
using System.Globalization;
using System.Text;

// Determine if a string has unique characters.
// Assume the string is in Unicode.
//
// C# - Visual Studio 2013 

namespace DoesStringHaveUniqueCharacters 
{
    public class UniqueCharDetector
    {
        //http://stackoverflow.com/questions/5924105/how-many-characters-can-be-mapped-with-unicode 
        // 17 planes x 65636 characters per plane -- 2048 surrogates - 66 non-characters == 1,111,998
        // But as of Unicode 6.0 only 109,384 characters are assigned.
        private const int MAX_ASSIGNED_UNICODE_CHARS = 109384;

        private const int bitsInInt = sizeof(int) * 8; 

        private int[] homeBrewBitVector;

        public UniqueCharDetector()
        {
            homeBrewBitVector = new int[MAX_ASSIGNED_UNICODE_CHARS / bitsInInt];
        }

        public bool IsUnique(String str)
        {
            if (str.Length > MAX_ASSIGNED_UNICODE_CHARS)
                return false;

            foreach(var c in str)
            {
                var key = (int)c;
                var majorIndex = key / bitsInInt;
                var minorIndex = key % bitsInInt;
                Console.WriteLine("{0} U+{1:x4} {2} -> stored at {3} {4}", c, key, key, majorIndex, minorIndex);

                if ((homeBrewBitVector[majorIndex] & (1 << minorIndex)) > 0)
                    return false;

                homeBrewBitVector[majorIndex] |= (1 << minorIndex); 
            }

            return true;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var test = "The cats\u1044RAreT8kinOverTheInterWebs!\u0066";
            var detector = new UniqueCharDetector();
            detector.IsUnique(test);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}