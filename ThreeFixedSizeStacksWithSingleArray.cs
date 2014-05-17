using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// Use a single array to implement three fixed sized stacks.
//
// Compiled: Visual Studio 2013

namespace ThreeFixedSizeStacksWithSingleArray 
{
    public interface IStack<T>
    {
        void Push(int stackNum, T item);
        T Pop(int stackNum);
        T Peek(int stackNum);
        bool IsEmpty(int stackNum);
    }

    public class FixedMultiStack<T> : IStack<T>
    { 
        private readonly int size;
        private readonly T[] buffer;
        private int[] index = new int[] {-1, -1, -1};

        public FixedMultiStack(int size)
        {
            this.size = size;
            this.buffer = new T[size * 3];
        }

        public void Push(int stackNum, T item)
        {
            if (stackNum < 0) { throw new Exception("Invalid stack number."); }
            if (IsFull(stackNum)) { throw new Exception("Stack is full."); }

            this.index[stackNum]++;
            this.buffer[TopOfStack(stackNum)] = item;
        }

        public T Pop(int stackNum)
        {
            if (stackNum < 0) { throw new Exception("Invalid stack number."); }
            if (IsEmpty(stackNum)) { throw new Exception("Stack is empty."); }

            var item = this.buffer[TopOfStack(stackNum)];
            this.buffer[TopOfStack(stackNum)] = default(T); // Clear reference
            this.index[stackNum]--;

            return item;
        }

        private int TopOfStack(int stackNum)
        {
            return stackNum * size + index[stackNum];
        }

        public T Peek(int stackNum)
        {
            if (stackNum < 0) { throw new Exception("Invalid stack number."); }
            if (IsEmpty(stackNum)) { throw new Exception("Stack is empty."); }

            return this.buffer[TopOfStack(stackNum)];
        }

        public bool IsEmpty(int stackNum)
        {
            return (index[stackNum] == -1);
        }

        public bool IsFull(int stackNum)
        {
            return (index[stackNum] + 1 == size);
        }
    }

    [TestClass]
    public class StackTests
    {
        [TestMethod]
        public void IsEmpty_WhenNew_ExpectEmpty()
        {
            var sut = new FixedMultiStack<int>(2);
            Assert.IsTrue(sut.IsEmpty(1));
        }

        [TestMethod]
        public void IsEmpty_WhenStackIsFull_ExpectTrue()
        {
            var sut = new FixedMultiStack<int>(2);
            sut.Push(1, 1);
            sut.Push(1, 2);
            Assert.IsTrue(sut.IsFull(1));
        }

        [TestMethod]
        public void IsFull_WhenNew_ExpectNotFull()
        {
            var sut = new FixedMultiStack<int>(2);
            Assert.IsFalse(sut.IsFull(1));
        }

        [TestMethod]
        public void IsFull_WhenStackIsFull_ExpectTrue()
        {
            var sut = new FixedMultiStack<int>(2);
            sut.Push(1, 1);
            sut.Push(1, 2);
            Assert.IsTrue(sut.IsFull(1));
        }

        [TestMethod]
        public void PushPop_WhenPushItemToStack_ExpectSameItemsFromPop()
        {
            var sut = new FixedMultiStack<int>(3);
            sut.Push(1, 10);
            sut.Push(1, 20);
            sut.Push(1, 30);
            Assert.AreEqual(30, sut.Pop(1));
            Assert.AreEqual(20, sut.Pop(1));
            Assert.AreEqual(10, sut.Pop(1));
        }

        [TestMethod]
        public void PushPop_WhenPushItemToDifferentStacks_ExpectCorrectItemsFromPop()
        {
            var sut = new FixedMultiStack<int>(3);
            sut.Push(0, 0);
            sut.Push(1, 10);
            sut.Push(2, 20);
            sut.Push(1, 11);
            sut.Push(2, 21);
            sut.Push(2, 22);
            Assert.AreEqual(11, sut.Pop(1));
            Assert.AreEqual(10, sut.Pop(1));
            Assert.AreEqual(22, sut.Pop(2));
            Assert.AreEqual(21, sut.Pop(2));
            Assert.AreEqual(20, sut.Pop(2));
            Assert.AreEqual(0, sut.Pop(0));
        }
    }
}