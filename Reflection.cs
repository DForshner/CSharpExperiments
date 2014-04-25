//
// Reflection demos
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace ReflectionDemos
{
    public class Base
    {
        public string GetName()
        {
            return this.GetType().Name;
        }

        public static string GetStaticName()
        {
            return MethodBase.GetCurrentMethod().DeclaringType.Name;
        }
    }

    public class Derived : Base
    {
        public string GetDerivedName()
        {
            return this.GetType().Name;
        }

        public string GetBaseClassName()
        {
            return this.GetType().BaseType.Name;
        }

        // The new keyword overrides the base classes' static method.  Otherwise static methods
        // can only be defined once per inheritance chain.
        public new static string GetStaticName()
        {
            return MethodBase.GetCurrentMethod().DeclaringType.Name;
        }
    }

    [TestClass]
    public class ReflectionDemos
    {
        [TestMethod]
        public void WhenBase_ExpectBaseClassName()
        {
            Assert.AreEqual("Base", new Base().GetName());
            Assert.AreEqual("Base", Base.GetStaticName());
        }

        [TestMethod]
        public void WhenDerived_ExpectDerivedClassName()
        {
            Assert.AreEqual("Derived", new Derived().GetName());
            Assert.AreEqual("Derived", new Derived().GetDerivedName());
            Assert.AreEqual("Derived", Derived.GetStaticName());
        }

        [TestMethod]
        public void WhenDerived_ExpectCanSeeBaseClassName()
        {
            Assert.AreEqual("Base", new Derived().GetBaseClassName());
        }
    }
}