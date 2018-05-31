using System;
using System.Linq;
using System.Numerics;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Lux.Utils;
using Common;
using NUnit.Framework;

namespace Crowdsale.Tests
{
    [TestFixture]
    public class CrowdsaleTest
    {
        private static readonly byte[] NeoAssetId = {155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197};
        private readonly byte[][] _scriptHashes = new[]
        {
            "D_OWNER",
            "AZCcft1uYtmZXxzHPr5tY7L6M85zG7Dsrv",
            "AWC97WM2rSfARUFdUiUY2DoWMm6o2Jehoq",
        }.Select(a => a.GetScriptHashFromAddress()).ToArray();

        private static Blockchain _chain;
        private static Emulator _emulator;

        [SetUp]
        public void Setup()
        {
            _chain = new Blockchain();
            _emulator = new Emulator(_chain);
            var owner = _chain.DeployContract("owner", TestHelper.Avm);
            _emulator.SetExecutingAccount(owner);
        }

        private void ExecuteInit()
        {
            var initResult = _emulator.Execute(Operations.Init).GetBoolean();
            Console.WriteLine($"Init result: {initResult}");
            Assert.IsTrue(initResult);
        }

        [Test]
        public void T01_CheckReturnFalseWhenInvalidOperation()
        {
            ExecuteInit();
            var result = _emulator.Execute("invalidOperation").GetBoolean();
            Console.WriteLine($"Result: {result}");
            Assert.IsFalse(result, "Invalid operation execution result should be false");
        }

        [Test]
        public void T02_CheckName()
        {
            ExecuteInit();
            var name = _emulator.Execute(Operations.Name).GetString();
            Console.WriteLine($"Token name: {name}");
            Assert.AreEqual("D_NAME", name);
        }

        [Test]
        public void T03_CheckSymbol()
        {
            ExecuteInit();
            var symbol = _emulator.Execute(Operations.Symbol).GetString();
            Console.WriteLine($"Token symbol: {symbol}");
            Assert.AreEqual("D_SYMBOL", symbol);
        }

        [Test]
        public void T04_CheckDecimals()
        {
            ExecuteInit();
            var decimals = _emulator.Execute(Operations.Decimals).GetBigInteger();
            Console.WriteLine($"Token decimals: {decimals}");
            Assert.AreEqual("D_DECIMALS", decimals.ToString());
        }

        [Test]
        public void T05_CheckOwnerBeforeInit()
        {
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(_scriptHashes[0].ByteToHex(), owner);
        }
        
        [Test]
        public void T06_CheckOwnerAfterInit()
        {
            ExecuteInit();
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(_scriptHashes[0].ByteToHex(), owner);
        }

        [Test]
        public void T07_CheckTotalSupplyIsZeroBeforeInit()
        {
            var totalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {totalSupply}");
            Assert.AreEqual(BigInteger.Zero, totalSupply);
        }

        [Test]
        public void T08_CheckNotPausedBeforePause()
        {
            ExecuteInit();
            var result = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T09_CheckPauseNotByOwner()
        {
            ExecuteInit();
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T10_CheckPauseByOwner()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T11_CheckPausedAfterPause()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            var paused = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsTrue(paused);
        }

        [Test]
        public void T12_CheckUnpauseAfterPause()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsTrue(unpauseResult);
        }

        [Test]
        public void T13_CheckNotPausedAfterUnpause()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");

            var paused = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsFalse(paused);
        }

        [Test]
        public void T14_CheckCannotUnpauseAfterUnpause()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");

            var secondUnpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {secondUnpauseResult}");
            Assert.IsFalse(secondUnpauseResult);
        }
        
        [Test]
        public void T15_CheckCannotUnpauseNotByOwner()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsFalse(unpauseResult);
        }
        
        [Test]
        public void T16_CheckTransferOwnership()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var transferOwnershipResult = _emulator
                .Execute(Operations.TransferOwnership, _scriptHashes[1])
                .GetBoolean();
            Console.WriteLine($"TransferOwnership result: {transferOwnershipResult}");
            Assert.IsTrue(transferOwnershipResult);
            
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(_scriptHashes[1].ByteToHex(), owner);
        }

        [Test]
        public void T17_SimpleBuy()
        {
            ExecuteInit();
            _emulator.SetTransaction(NeoAssetId, 1);
            var result = _emulator.Execute(Operations.MintTokens);
            Console.WriteLine($"MintTokens result: {result}");
            Assert.IsTrue(result);
        }
    }
}
