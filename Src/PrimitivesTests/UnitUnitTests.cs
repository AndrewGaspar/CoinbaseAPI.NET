using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Bitlet.Primitives.Tests
{
    [TestClass]
    public class UnitUnitTests
    {
        [TestMethod]
        public void CorrectValue()
        {
            var unit = new FixedPrecisionUnit<Bitcoin.BTC>(12.3M);
            Assert.AreEqual(unit.Value, 12.3M);
        }

        [TestMethod]
        public void AddFixedPrecisionUnit()
        {
            var one = new FixedPrecisionUnit<Bitcoin.BTC>(1M);
            var two = new FixedPrecisionUnit<Bitcoin.BTC>(2M);
            Assert.AreEqual((one + two).Value, 3M);
        }

        [TestMethod]
        public void CheckTruncation()
        {
            var unit = new FixedPrecisionUnit<Bitcoin.mBTC>(1.234567M);
            Assert.AreEqual(unit.Value, 1.23456M);
        }

        [TestMethod]
        public void CheckConversion()
        {
            var unit = new FixedPrecisionUnit<Bitcoin.BTC>(12.345M);
            var mBTC = Bitcoin.Convert<Bitcoin.BTC, Bitcoin.mBTC>(unit);

            Assert.AreEqual(mBTC.Value, 12345M);
        }

        [TestMethod]
        public void CheckDivision()
        {
            var unit = new FixedPrecisionUnit<Bitcoin.BTC>(1M);
            Assert.AreEqual(0.5M, (unit / 2).Value);
        }

        [TestMethod]
        public void CheckDivisionTruncation()
        {
            var unit = new FixedPrecisionUnit<Bitcoin.Satoshis>(20M);
            Assert.AreEqual(6M, (unit / 3).Value);
        }
    }
}
