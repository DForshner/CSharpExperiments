using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// Covariance and contravariance enable implicit reference conversion. 
// Covariance: An object that is instantiated with a more derived type can be assigned to an object of a less derived type.
// Contravariance : An object that is instantiated with a less derived type can be assigned to an object of a more derived type.

namespace CovarianceAndContravariance
{
    [TestClass]
    public class CovarianceAndContravarianceTests 
    {
        public class Base { }

        public class Derived : Base{ }

        public static class Variant
        {
            // We can pass an object into a method as long as the type of the parameters is
            // less specific than the type of the object being passed in.
            public static void Covarient(Base b) { }

            // We can return an object from a method a long as the type of the object is more
            // specific than the return type;
            public static object Convariant() { return new Base(); }
        }

        [TestMethod]
        public void DemoVariant()
        {
            Variant.Covarient(new Derived());
            var obj = Variant.Convariant();

            Assert.IsTrue(obj is Object);
            Assert.IsTrue(obj is Base);
            Assert.IsFalse(obj is Derived);
        }

        // Be default generics break the rules because the type is fixed so the type
        // can be used as either a return type or a parameter.
        public class Invariant<T>
        {
            T Run(T t) { return t; }
        }

        [TestMethod]
        public void DemoNonVariant()
        {
            var obj = new Invariant<Base>();

            // The type system won't allow a Invariant<Base> to be Invariant<Object>
            Assert.IsFalse(obj is Invariant<Object>);
            Assert.IsTrue(obj is Invariant<Base>);
            Assert.IsFalse(obj is Invariant<Derived>);
        }

        // Mark the type with out to allow it to be returned out of a method.
        public interface ICovariant<out T>
        {
            T Method();
        }

        // Mark the type with in to allow it to used in a parameter.
        public interface IContravariant<in T>
        {
            void Method(T t);
        }

        public class CoAndContravariant<T> : ICovariant<T>, IContravariant<T>
        {
            public T Method() { return default(T); }
            public void Method(T t) { }
        }

        [TestMethod]
        public void DemoGenericCoAndConvariant()
        {
            var obj = new CoAndContravariant<Base>();

            Object a = obj.Method();
            //obj.Method(new Object()); // Won't work

            Assert.IsTrue(obj is ICovariant<Object>);
            Assert.IsFalse(obj is IContravariant<Object>);

            Base c = obj.Method();
            obj.Method(new Base());

            Assert.IsTrue(obj is IContravariant<Base>);
            Assert.IsTrue(obj is IContravariant<Base>);

            //Derived c = obj.Method(); // Won't work
            obj.Method(new Derived());

            Assert.IsFalse(obj is ICovariant<Derived>);
            Assert.IsTrue(obj is IContravariant<Derived>);
        }
    }
}