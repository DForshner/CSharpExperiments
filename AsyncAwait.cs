using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace GenericConsoleApplication
{
    class NoAsyncAwait
    {
        public long ContinuedStartMethod { get; set; }
        public long FinishedWorker { get; set; }
        private Stopwatch stopwatch = new Stopwatch();

        public void Start()
        {
            stopwatch.Start();
            Task.Run(new Action(Worker));
            ContinuedStartMethod = stopwatch.ElapsedMilliseconds;
            Thread.Sleep(500);
        }

        public void Worker()
        {
            Thread.Sleep(1000);
            ContinuedStartMethod = stopwatch.ElapsedMilliseconds;
        }
    }

    class AsyncAwait
    {
        Stopwatch stopwatch = new Stopwatch();

        public async void Start()
        {
            stopwatch.Start();
            await Task.Run(new Action(Worker));
        }

        public void Worker()
        {
            Thread.Sleep(1000);
        }
    }

    #region Tests

    [TestClass]
    public class SimpleStratagyFactoryTests
    {
        #region SETUP

        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //    // Simulate initializing the factory once per app start
        //    SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>.Map(ProductTypes.TypeA, (x) => ProductA.Create(x));
        //    SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>.Map(ProductTypes.TypeB, (x) => ProductB.Create(x));
        //    SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>.Map(ProductTypes.TypeC, (x) => ProductC.Create(x));
        //}

        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //    SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>.ClearMappings();
        //}

        #endregion SETUP

        [TestMethod]
        public void Create_WhenNoAsyncAwait_ExpectStartMethodAndWorkerAreConcurrent()
        {
            // Arrange
            var testObject = new NoAsyncAwait();
            // Act
            testObject.Start();
            // Assert
            Assert.IsTrue(testObject.FinishedWorker > testObject.ContinuedStartMethod);
        }

        //[TestMethod]
        //public void Create_WhenCreateWithDependancy_ExpectDependancyPassedToFactoryProducts()
        //{
        //    // Arrange
        //    var factory = new SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>(new Dependancy());
        //    // Act
        //    var result = factory.Create(ProductTypes.TypeB);
        //    // Assert
        //    Assert.AreEqual(215, result.DoSomething());
        //}

        //[TestMethod]
        //[ExpectedException(typeof(Exception))]
        //public void Create_WhenTryCreateAnUnmappedKey_ExpectException()
        //{
        //    // Arrange
        //    var factory = new SimpleStratagyFactory<AbstractProduct, ProductTypes, Dependancy>(new Dependancy());

        //    // Call my test cleanup to wipe out the factory mappings
        //    MyTestCleanup();

        //    // Act & Assert
        //    var result = factory.Create(ProductTypes.TypeA);
        //}
    }

    #endregion Tests
}