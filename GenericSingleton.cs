using System;
using System.Diagnostics;

namespace GenericSingleton 
{
    /// <summary>
    /// Makes a single instance of a type globally available. 
    /// Generally considered an anti-pattern.
    /// </summary>
    public static class GenericSingleton<T> where T : class
    {
        private static T instance = null;

        public static void Initialize(T newInstance)
        {
            if (instance != null) { throw new Exception("Already initialized."); }
            instance = newInstance;
        }

        public static T GetInstance()
        {
            Debug.Assert(instance == null, "Expected singleton to be initialized.");
            return instance;
        }
    }
}
