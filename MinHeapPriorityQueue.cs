using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// A heap based implementation of a min priority queue.
// This should provide O(log(n)) time complexity for initial inserts and deletes 
// and O(n) for the initial construction (heapify).

namespace MinHeapPriorityQueue
{
    public class MinPriorityQueue<TKey, TValue> where TKey : IComparable<TKey>
    {
        private List<Element<TKey, TValue>> _heap = new List<Element<TKey, TValue>>();
        private Func<TValue, TKey> _keySelector;

        [DebuggerDisplay("Key = {Key}")]
        private struct Element<TKey, TValue>
        {
            public readonly TKey Key;
            public readonly TValue Value;
            public Element(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        public MinPriorityQueue(Func<TValue, TKey> keySelector)
        {
            if (keySelector == null) { throw new ArgumentNullException(); }
            _keySelector = keySelector;
        }

        public MinPriorityQueue(IEnumerable<TValue> values, Func<TValue, TKey> keySelector)
        {
            if (values == null) { throw new ArgumentNullException(); }
            if (keySelector == null) { throw new ArgumentNullException(); }

            _keySelector = keySelector;

            foreach (var value in values)
            {
                var element = new Element<TKey, TValue>(keySelector(value), value);
                _heap.Add(element);
            }

            /// Recursively work from middle to root check that for each level the parent node
            /// is larger than its children.  When this is complete the entire tree will be ordered
            for (var i = _heap.Count / 2; i >= 0; i--)
            {
                Heapify(i);
            }
        }

        public void Enqueue(TValue value)
        {
            _heap.Add(new Element<TKey, TValue>(_keySelector(value), value));
            Swap(0, _heap.Count - 1);

            // The new root may violate the heap order property so heapify to restore order.
            Heapify(0);
        }

        public TValue PeekMin()
        {
            if (Empty) { return default(TValue); }
            return _heap[0].Value;
        }

        public TValue DequeueMin()
        {
            if (Empty) { return default(TValue); }

            // Remove from the end of the underlying list so it's O(1)
            var lastIdx = _heap.Count - 1;
            Swap(0, lastIdx);
            var element = _heap[lastIdx];
            _heap.RemoveAt(lastIdx);

            // The new root may violate the heap order property so heapify to restore order.
            Heapify(0);

            return element.Value;
        }

        public int Count { get { return _heap.Count; } }

        public bool Empty { get { return _heap.Count == 0; } }

        /// <summary>
        /// If the parent element at the current level is not smaller than both of its children swap
        /// and then work downwards through the levels ensuring the parent element is always smaller 
        /// then the child elements at each level.
        /// Complexity: O(log n)
        /// </summary>
        private void Heapify(int parentIdx)
        {
            int leftChildIdx = 2 * parentIdx + 1;
            int rightChildIdx = 2 * parentIdx + 2;
            int smallest = parentIdx;

            if (leftChildIdx < _heap.Count && _heap[leftChildIdx].Key.CompareTo(_heap[parentIdx].Key) < 0)
            {
                smallest = leftChildIdx;
            }

            if (rightChildIdx < _heap.Count && _heap[rightChildIdx].Key.CompareTo(_heap[smallest].Key) < 0)
            {
                smallest = rightChildIdx;
            }

            if (smallest != parentIdx)
            {
                // Move the smaller child into the parent's position
                Swap(parentIdx, smallest);
                Heapify(smallest);
            }
        }

        private void Swap(int x, int y)
        {
            var temp = _heap[x];
            _heap[x] = _heap[y];
            _heap[y] = temp;
        }
    }

    public static class PriorityQueueExtensions
    {
        public static MinPriorityQueue<TKey, TValue> ToPriorityQueue<TKey, TValue>(
            this IEnumerable<TValue> values, Func<TValue, TKey> keySelector) where TKey : IComparable<TKey>
        {
            return new MinPriorityQueue<TKey, TValue>(values, keySelector);
        }
    }

    [TestClass]
    public class HeapSortTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhenConstructWithNullKeySelector_ExpectException()
        {
            new MinPriorityQueue<string, string>(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WhenConstructWithNullValueList_ExpectException()
        {
            new MinPriorityQueue<string, string>(null, null);
        }

        private class Foo 
        {
            public string A;
            public string B;
        }

        [TestMethod]
        public void WhenEmptyQueue_ExpectNullReturned()
        {
            var heap = new MinPriorityQueue<string, Foo>(x => x.A);
            heap.Enqueue(new Foo { A = "1", B = "2" });
            heap.Enqueue(new Foo { A = "3", B = "4" });

            Assert.AreEqual("2", heap.DequeueMin().B);
            Assert.AreEqual("4", heap.DequeueMin().B);
            Assert.AreEqual(null, heap.DequeueMin());
            Assert.AreEqual(null, heap.DequeueMin());
        }

        [TestMethod]
        public void WhenInsertOutOfOrder_ExpectReturnedInAscendingKeyValue()
        {
            var values = new [] 
            {
                Tuple.Create(5, "E"),
                Tuple.Create(2, "B"),
                Tuple.Create(1, "A"),
                Tuple.Create(4, "D"),
                Tuple.Create(3, "C"),
            };
            var heap = values.ToPriorityQueue(x => x.Item1);

            Assert.AreEqual("A", heap.DequeueMin().Item2);
            Assert.AreEqual("B", heap.DequeueMin().Item2);
            Assert.AreEqual("C", heap.DequeueMin().Item2);
            Assert.AreEqual("D", heap.DequeueMin().Item2);
            Assert.AreEqual("E", heap.DequeueMin().Item2);
        }

        [TestMethod]
        public void WhenDuplicateKeyValues_ExpectElementsReturnedInOrder()
        {
            var values = new [] 
            {
                Tuple.Create(100, "100"),
                Tuple.Create(200, "200"),
                Tuple.Create(100, "100"),
                Tuple.Create(0, "0"),
                Tuple.Create(100, "100"),
            };
            var heap = values.ToPriorityQueue(x => x.Item1);

            Assert.AreEqual("0", heap.DequeueMin().Item2);
            Assert.AreEqual("100", heap.DequeueMin().Item2);
            Assert.AreEqual("100", heap.DequeueMin().Item2);
            Assert.AreEqual("100", heap.DequeueMin().Item2);
            Assert.AreEqual("200", heap.DequeueMin().Item2);
        }
    }
}