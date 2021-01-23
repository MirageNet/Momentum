using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.Momentum
{
    public class SequencerTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestNext()
        {
            // 3 bit sequencer
            var sequencer = new Sequencer(3);
            Assert.That(sequencer.Next(), Is.EqualTo(1));

        }

        [Test]
        public void TestBits()
        {
            // 3 bit sequencer
            var sequencer = new Sequencer(3);
            Assert.That(sequencer.Bits, Is.EqualTo(3));
        }

        [Test]
        public void TestWrap()
        {
            // 2 bit sequencer, so if we ask for 4, it should wrap
            var sequencer = new Sequencer(2);
            Assert.That(sequencer.Next(), Is.EqualTo(1));
            Assert.That(sequencer.Next(), Is.EqualTo(2));
            Assert.That(sequencer.Next(), Is.EqualTo(3));
            Assert.That(sequencer.Next(), Is.EqualTo(0));
        }

        [Test]
        public void TestDistanceAtBegining()
        {
            // 2 bit sequencer, so if we ask for 4, it should wrap
            var sequencer = new Sequencer(8);
            Assert.That(sequencer.Distance(0, 8), Is.EqualTo(-8));
        }

        [Test]
        public void TestNegativeDistance()
        {
            // 2 bit sequencer, so if we ask for 4, it should wrap
            var sequencer = new Sequencer(8);
            Assert.That(sequencer.Distance(8, 0), Is.EqualTo(8));
        }

        [Test]
        public void TestWrappingDistance()
        {
            // 2 bit sequencer, so if we ask for 4, it should wrap
            var sequencer = new Sequencer(8);
            Assert.That(sequencer.Distance(254, 4), Is.EqualTo(-6));
        }
    }
}