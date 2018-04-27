using System;
using NEP5.Common;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        [Test]
        public void CheckName()
        {
            using (var engine = TestHelper.LoadContractScript())
            {
                var name = TestHelper.Execute(engine, Operations.Name).GetString();
                Console.WriteLine($"Token name: {name}");
                Assert.AreEqual("D_NAME", name);
            }
        }
        
        [Test]
        public void CheckSymbol()
        {
            using (var engine = TestHelper.LoadContractScript())
            {
                var symbol = TestHelper.Execute(engine, Operations.Symbol).GetString();
                Console.WriteLine($"Token symbol: {symbol}");
                Assert.AreEqual("D_SYMBOL", symbol);
            }
        }
        
        [Test]
        public void CheckDecimals()
        {
            using (var engine = TestHelper.LoadContractScript())
            {
                var decimals = TestHelper.Execute(engine, Operations.Decimals).GetBigInteger();
                Console.WriteLine($"Token decimals: {decimals}");
                Assert.AreEqual("D_DECIMALS", decimals.ToString());
            }
        }
    }
}