class Program
{
    static void Display(params int[] ints)
    {
        System.Console.WriteLine("----------------- int[] -----------------");

        for (int i = 0; i < ints.Length; i++)
            System.Console.WriteLine("{0}: {1}", i, ints[i]);
    }

    static void Display(params object[] objects)
    {
        System.Console.WriteLine("----------------- object[] -----------------");

        for (int i = 0; i < objects.Length; i++)
            System.Console.WriteLine("{0}: {1}", i, objects[i]);
    }

    public static void Main()
    {
        // Normal Form - Pass array explicitly.
        Display(new int[] { 5, 4, 3, 2, 1 });

        // Expanded Form - Compiler generates a call to the function. 
        Display(5, 4, 3, 2, 1);

        // Normal Form? - objects is an array with two elements.
        // Expanded Form - objects is an array with a single element which is an array of two strings.
        // The compiler checks for the normal form first and only considers expanded form if the normal form didn't apply.
        Display(new object[] { "A", "B" });

        Display(new object[] { new object[] { "A", "B" } });

        // The array can also be cast to force it to be passed as a single object.
        Display((object)new object[] { "A", "B" });
    }
}