using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rule110CellularAutomaton
{
    public class Cell
    {
        private static Dictionary<Tuple<bool, bool, bool>, bool> _stuff =
            new Dictionary<Tuple<bool, bool, bool>, bool>()
            {
                { Tuple.Create(true, true, true), false },
                { Tuple.Create(true, true, false), true},
                { Tuple.Create(true, false, true), true},
                { Tuple.Create(true, false, false), false },
                { Tuple.Create(false, true, true), true},
                { Tuple.Create(false, true, false), true},
                { Tuple.Create(false, false, true), true},
                { Tuple.Create(false, false, false), false }
            };

        public bool IsAlive { get; set; }

        public Cell(bool isAlive)
        {
            IsAlive = isAlive;
        }

        public Cell(Cell upperLeft, Cell above, Cell upperRight)
        {
            var key = Tuple.Create(upperLeft.IsAlive, above.IsAlive, upperRight.IsAlive);
            Debug.Assert(_stuff.ContainsKey(key));
            IsAlive = _stuff[key];
        }
    }

    public class Row : IEnumerable<Cell>
    {
        private readonly List<Cell> _cells;

        public int Count { get { return _cells.Count; } }

        public Row(ICollection<Cell> cells)
        {
            Debug.Assert(cells != null);
            _cells = cells.ToList();
        }

        /// <summary>
        /// Create new row based on parent
        /// </summary>
        public Row(Row parent)
        {
            Debug.Assert(parent != null);
            _cells = Enumerable.Range(0, parent.Count)
                .Select(i => new Cell(parent.GetUpperLeft(i), parent.GetUpper(i), parent.GetUpperRight(i)))
                .ToList();
        }

        public IEnumerator<Cell> GetEnumerator()
        {
            return _cells.GetEnumerator();
        }

        private IEnumerator GetEnumerator1()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }

        public override String ToString()
        {
            return String.Join("", _cells.Select(x => x.IsAlive ? "#" : " "));
        }

        public Cell GetUpperLeft(int idx)
        {
            return (idx >= 1 && idx <= _cells.Count - 1) ? _cells[idx - 1] : new Cell(false);
        }

        public Cell GetUpper(int idx)
        {
            return (idx >= 0 && idx <= _cells.Count - 1) ? _cells[idx] : new Cell(false);
        }

        public Cell GetUpperRight(int idx)
        {
            return (idx >= 0 && idx <= _cells.Count - 2) ? _cells[idx + 1] : new Cell(false);
        }
    }

    [TestClass]
    public class Rule110CellularAutomatonTests
    {
        [TestMethod]
        public void Cell_WhenCreated_ExpectIsAliveFollowsRule110Patterns()
        {
            var testCases = new[] {
                new { upperLeft = true, above = true, upperRight = true, expected = false },
                new { upperLeft = true, above = true, upperRight = false, expected = true},
                new { upperLeft = true, above = false, upperRight = true, expected = true},
                new { upperLeft = true, above = false, upperRight = false, expected = false},
                new { upperLeft = false, above = true, upperRight = true, expected = true},
                new { upperLeft = false, above = true, upperRight = false, expected = true},
                new { upperLeft = false, above = false, upperRight = true, expected = true},
                new { upperLeft = false, above = false, upperRight = false, expected = false },
            };
            foreach(var testCase in testCases)
            {
                var sut = new Cell(new Cell(testCase.upperLeft), new Cell(testCase.above), new Cell(testCase.upperRight));
                Assert.AreEqual(testCase.expected, sut.IsAlive,
                    String.Format("{0} {1} {2} = {3}", testCase.upperLeft, testCase.above, testCase.upperRight, testCase.expected));
            }
        }

        [TestMethod]
        public void Row_WhenGetUpperLeftOutOfBounds_ExpectDeadCell()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsFalse(sut.GetUpperLeft(-1).IsAlive);
            Assert.IsFalse(sut.GetUpperLeft(0).IsAlive);
            Assert.IsFalse(sut.GetUpperLeft(3).IsAlive);
            Assert.IsFalse(sut.GetUpperLeft(4).IsAlive);
        }

        [TestMethod]
        public void Row_WhenGetUpperLeftInBounds_ExpectAlive()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsTrue(sut.GetUpperLeft(1).IsAlive);
            Assert.IsTrue(sut.GetUpperLeft(2).IsAlive);
        }

        [TestMethod]
        public void Row_WhenGetUpperOutOfBounds_ExpectDeadCell()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsFalse(sut.GetUpper(-1).IsAlive);
            Assert.IsFalse(sut.GetUpper(3).IsAlive);
        }

        [TestMethod]
        public void Row_WhenGetUpperInBounds_ExpectAlive()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsTrue(sut.GetUpper(0).IsAlive);
            Assert.IsTrue(sut.GetUpper(2).IsAlive);
        }

        [TestMethod]
        public void Row_WhenGetUpperRightOutOfBounds_ExpectDeadCell()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsFalse(sut.GetUpperRight(-1).IsAlive);
            Assert.IsFalse(sut.GetUpperRight(2).IsAlive);
            Assert.IsFalse(sut.GetUpperRight(3).IsAlive);
        }

        [TestMethod]
        public void Row_WhenGetUpperRightInBounds_ExpectAlive()
        {
            var sut = GetRowWithThreeLiveCells();
            Assert.IsTrue(sut.GetUpperRight(0).IsAlive);
            Assert.IsTrue(sut.GetUpperRight(1).IsAlive);
        }

        private static Row GetRowWithThreeLiveCells()
        {
            //   T   T   T
            //  [0] [1] [2]
            return new Row(new[] { new Cell(true), new Cell(true), new Cell(true) });
        }

        [TestMethod]
        public void Row_WhenCreateRowFromParent_ExpectFollowsRule110Patterns()
        {
            //   T   F   T   T   F   F
            //  [0] [1] [2] [3] [4] [5]
            var parent = new Row(new[] { new Cell(true), new Cell(false), new Cell(true), new Cell(true), new Cell(false), new Cell(false) });
            var result = new Row(parent);

            //   T   T   T   T   F   F
            //  [0] [1] [2] [3] [4] [5]
            var expect = new Row(new[] { new Cell(true), new Cell(true), new Cell(true), new Cell(true), new Cell(false), new Cell(false) });
            Assert.AreEqual(expect.ToString(), result.ToString());
        }
    }

    //public class Program
    //{
    //    private const int MAX_COL = 29;
    //    private const int MAX_ROW = 15;

    //    static void Main(string[] args)
    //    {
    //        Console.WriteLine("------------ Rule 110 Demo --------------");

    //        var seed = Enumerable.Range(0, MAX_COL)
    //            .Select(x => x == 15 ? new Cell(true) : new Cell(false))
    //            .ToList();

    //        var curr = new Row(seed);
    //        Console.WriteLine(curr.ToString());

    //        for (int i = 0; i < MAX_ROW; ++i)
    //        {
    //            curr = new Row(curr);
    //            Console.WriteLine(curr.ToString());
    //        }
    //    }
    //}
}