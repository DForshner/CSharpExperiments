using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

// There is a sub that starts at position zero and travels away at an unknown positive velocity.
// Assuming you can check one position per tick how do you determine the velocity of the sub?  The sub
// gets a head start of one tick on the hunter. 

namespace SubHunting 
{
    public class Sub
    {
        private int _speed;
        private int _position;

        public Sub(int speed)
        {
            if (speed < 0) throw new ArgumentException("Sub's speed must be positive."); 
            _speed = speed;
        }

        public void Tick()
        {
            checked // Throw exception if overflow
            {
                _position += _speed;
            }
        }

        public bool IsAtPosition(int position)
        {
            return (_position == position);
        }
    }

    public class Hunter 
    {
        private Sub _sub;
            
        // First check should be at position zero assuming zero velocity.
        private int _ticks = 0;
        private int _positionToCheck = 0;
        private int _estimatedSpeed = 0;

        public Hunter(Sub sub)
        {
            _sub = sub;
        }

        public int GetSubSpeed()
        {
            _sub.Tick(); // Sub starts one tick ahead of hunter

            while (!_sub.IsAtPosition(_positionToCheck))
            {
                _sub.Tick();
                Tick();
            }

            return _estimatedSpeed;
        }

        private void Tick()
        {
            _ticks++;
            _estimatedSpeed++;

            // We starting hunting one tick after the sub starts moving;
            var subTicks = (_ticks + 1);

            checked // Throw exception if overflow
            {
                _positionToCheck = subTicks * _estimatedSpeed;
            }
        }
    }

    /// <summary>
    /// In the real world there would be separate unit test classes
    /// </summary>
    [TestClass]
    public class SubHuntingTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void WhenNegativeSpeed_ExpectException()
        {
            var sub = new Sub(-1);
        }

        [TestMethod]
        public void WhenZeroSpeed_ExpectFound()
        {
            var hunt = new Hunter(new Sub(0));
            Assert.AreEqual(0, hunt.GetSubSpeed());
        }

        [TestMethod]
        public void WhenMinPositiveInt_ExpectFound()
        {
            var hunt = new Hunter(new Sub(1));
            Assert.AreEqual(1, hunt.GetSubSpeed());
        }

        [TestMethod]
        public void WhenMaxInt_ExpectFound()
        {
            var hunt = new Hunter(new Sub(int.MaxValue - 1));
            Assert.AreEqual(int.MaxValue - 1, hunt.GetSubSpeed());
        }
    }
}