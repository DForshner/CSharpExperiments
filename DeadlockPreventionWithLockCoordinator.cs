using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// Design a lock coordinator that hands out locks as long as there are no cycles.
// Notes: Avoided using Linq

namespace DeadlockPreventionWithLockCoordinator
{
    public class Lock { }

    public class LockCoordinator
    {
        private class Node
        {
            private enum State { UNVISITED, VISTING, VISITED }
            private IList<Node> children = new List<Node>();
            private int id;
            private Lock innerLock;
            private int maxLockId;

            public Node(int id, int maxLocks)
            {
                this.id = id;
                this.maxLockId = maxLocks;
            }

            public void Add(Node node) { children.Add(node); }

            public void Remove(Node node) { children.Remove(node); }

            /// <summary>
            /// Check for cycle by doing a DFS.
            /// </summary>
            public bool HasCycle(IDictionary<int, bool> visitedNodes)
            {
                var visited = new State[maxLockId];
                for (var i = 0; i < visited.Length; i++)
                    visited[i] = State.UNVISITED;

                return HasCycle(visited, visitedNodes);
            }

            private bool HasCycle(State[] visited, IDictionary<int, bool> visitedNodes)
            {
                if (visitedNodes.ContainsKey(id))
                    visitedNodes[id] = true;

                if (visited[id] == State.VISTING)
                {
                    // We are visiting this node for the second time so there is a cycle.
                    return true; 
                }
                else if (visited[id] == State.UNVISITED)
                {
                    // We have not visited this node before.
                    visited[id] = State.VISTING;

                    // Search this nodes children.
                    foreach (var node in children)
                        if (node.HasCycle(visited, visitedNodes))
                            return true;

                    visited[id] = State.VISITED;
                }

                // No cycles where found
                return false;
            }

            public Lock GetLock()
            {
                if (innerLock == null) { innerLock = new Lock(); }
                return innerLock;
            }

            public int GetLockID() { return id; }
        }

        private Node[] locks;
        private IDictionary<int, LinkedList<Node>> ownerLockOrdering = new Dictionary<int, LinkedList<Node>>();
        private object declareLock = new Object();

        public LockCoordinator(int numLocks)
        {
            if (numLocks <= 0) { throw new ArgumentOutOfRangeException(); } 

            this.locks = new Node[numLocks];
            for (var i = 0; i < numLocks; i++)
                this.locks[i] = new Node(i, numLocks);
        }

        /// <summary>
        /// Returns true if the requested lock order will not cause deadlocks.
        /// </summary>
        public bool Declare(int ownerId, int[] resourceIdsInOrder)
        {
            if (resourceIdsInOrder == null) { throw new ArgumentNullException(); } 
            Debug.Assert(new Func<bool>(() => { foreach (var id in resourceIdsInOrder) { if (id > locks.Length) { return false; } }; return true; })(), 
                "Request resource Id exceeds maximum lock Id.");

            // Only one thread can declare a lock ordering at a time.
            lock (declareLock)
            {
                AddNodeLinks(resourceIdsInOrder);

                // If we have cycle destroy this resource list and return false.
                if (HasCycle(resourceIdsInOrder))
                {
                    DeleteNodeLinks(resourceIdsInOrder);
                    return false;
                }

                // Assign the requested lock order to the ownerId so
                // its lock requests can be checked against its declared order. 
                AddLockRequestOrder(ownerId, resourceIdsInOrder);

                return true;
            }
        }

        private bool HasCycle(int[] resourceIdsInOrder)
        {
            // None of the requested nodes have been visited yet.
            var visitedNodes = new Dictionary<int, bool>();
            foreach (var resource in resourceIdsInOrder)
                visitedNodes.Add(resource, false);

            // Perform a DFS on each node in the requested resource list.
            foreach (var id in resourceIdsInOrder)
            {
                Debug.Assert(visitedNodes.ContainsKey(id), "Expected visited nodes to contain all resource Ids.");
                if (!visitedNodes[id])
                {
                    if (locks[id].HasCycle(visitedNodes))
                        return true;
                }
            }

            return false;
        }

        private void AddNodeLinks(int[] resourceIdsInOrder)
        {
            for (var i = 1; i < resourceIdsInOrder.Length; i++)
            {
                Node prev = locks[resourceIdsInOrder[i - 1]];
                Node next = locks[resourceIdsInOrder[i]];
                prev.Add(next);
            }
        }

        private void DeleteNodeLinks(int[] resourceIdsInOrder)
        {
            for (var i = 1; i < resourceIdsInOrder.Length; i++)
            {
                var p = locks[resourceIdsInOrder[i - 1]];
                var n = locks[resourceIdsInOrder[i]];
                p.Remove(n);
            }
        }

        private void AddLockRequestOrder(int ownerId, int[] resourceIdsInOrder)
        {
            var list = new LinkedList<Node>();
            for (var i = 0; i < resourceIdsInOrder.Length; i++)
            {
                Node resource = locks[resourceIdsInOrder[i]];
                list.AddLast(resource);
            }
            ownerLockOrdering.Add(ownerId, list);
        }

        /// <summary>
        /// Returns the requested lock as long as the locks are being requested in order.
        /// </summary>
        public Lock GetLock(int ownerId, int resourceId)
        {
            LinkedList<Node> list;
            if (!ownerLockOrdering.TryGetValue(ownerId, out list))
                throw new Exception("Owner has not declared any lock order requests.");

            var head = list.First.Value;
            if (head.GetLockID() == resourceId)
            {
                list.RemoveFirst();
                if (list.First == null) { ownerLockOrdering.Remove(ownerId); }
                return head.GetLock();
            }

            throw new Exception("Owner has requested a non-existent lock or made an out of order request.");
        }
    }

    [TestClass]
    public class LockCoordinatorTests
    {
        [TestMethod]
        public void WhenRequestLocksWithCycle_ExpectFails()
        {
            var factory = new LockCoordinator(4);
            Assert.IsTrue(factory.Declare(1, new int[] { 3, 2, 1, 0 }));
            Assert.IsFalse(factory.Declare(2, new int[] { 2, 3 }));
        }

        [TestMethod]
        public void WhenRequestLocksInOrder_ExpectOK()
        {
            var factory = new LockCoordinator(4);
            factory.Declare(1, new int[] { 0, 1, 2, 3 });
            factory.Declare(2, new int[] { 2, 3 });

            Assert.IsNotNull(factory.GetLock(1, 0));
            Assert.IsNotNull(factory.GetLock(1, 1));
            Assert.IsNotNull(factory.GetLock(2, 2));
            Assert.IsNotNull(factory.GetLock(1, 2));
            Assert.IsNotNull(factory.GetLock(2, 3));
            Assert.IsNotNull(factory.GetLock(1, 3));
        }

        [TestMethod]
        public void WhenRequestLocksInReverseOrder_ExpectOK()
        {
            var factory = new LockCoordinator(4);
            Assert.IsTrue(factory.Declare(1, new int[] { 3, 2, 1, 0 }));
            Assert.IsTrue(factory.Declare(2, new int[] { 3, 2 }));

            Assert.IsNotNull(factory.GetLock(1, 3));
            Assert.IsNotNull(factory.GetLock(1, 2));
            Assert.IsNotNull(factory.GetLock(2, 3));
            Assert.IsNotNull(factory.GetLock(1, 1));
            Assert.IsNotNull(factory.GetLock(2, 2));
            Assert.IsNotNull(factory.GetLock(1, 0));
        }
    }
}