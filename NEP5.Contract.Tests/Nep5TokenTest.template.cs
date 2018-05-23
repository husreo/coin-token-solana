using System;
using System.Linq;
using System.Numerics;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Lux.Utils;
using NEP5.Common;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        private readonly byte[][] _scriptHashes = new[]
        {
            "D_OWNER",
            "AYg1R35Ymx3Bazs7Xqo5kc7UkuF8uPQzeU",
            "AX3CjPZzknv1WrLGQArtHKTMEg3yq3tbJU",
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

        [Test]
        public void T01_CheckReturnFalseWhenInvalidOperation()
        {
            var result = _emulator.Execute("invalidOperation").GetBoolean();
            Console.WriteLine($"Result: {result}");
            Assert.IsFalse(result, "Invalid operation execution result should be false");
        }

        [Test]
        public void T02_CheckName()
        {
            var name = _emulator.Execute(Operations.Name).GetString();
            Console.WriteLine($"Token name: {name}");
            Assert.AreEqual("MyWish Token", name);
        }

        [Test]
        public void T03_CheckSymbol()
        {
            var symbol = _emulator.Execute(Operations.Symbol).GetString();
            Console.WriteLine($"Token symbol: {symbol}");
            Assert.AreEqual("WISH", symbol);
        }

        [Test]
        public void T04_CheckDecimals()
        {
            var decimals = _emulator.Execute(Operations.Decimals).GetBigInteger();
            Console.WriteLine($"Token decimals: {decimals}");
            Assert.AreEqual("8", decimals.ToString());
        }

        [Test]
        public void T05_CheckOwner()
        {
            var owner = _emulator.Execute(Operations.Owner).GetByteArray();
            Console.WriteLine($"Owner scriptHash: {owner}");
            Assert.AreEqual(_scriptHashes[0], owner);
        }

        [Test]
        public void T06_CheckTotalSupplyIsZeroBeforeDeploy()
        {
            var totalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {totalSupply}");
            Assert.AreEqual(BigInteger.Zero, totalSupply);
        }

        [Test]
        public void T07_CheckNotPausedBeforePause()
        {
            var result = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T08_CheckPauseNotByOwner()
        {
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T09_CheckPauseByOwner()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T10_CheckPausedAfterPause()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            var paused = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsTrue(paused);
        }

        [Test]
        public void T11_CheckUnpauseAfterPause()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsTrue(unpauseResult);
        }

        [Test]
        public void T12_CheckNotPausedAfterUnpause()
        {
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
        public void T13_CheckCannotUnpauseAfterUnpause()
        {
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
        public void T14_CheckCannotUnpauseNotByOwner()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsFalse(unpauseResult);
        }

        [Test]
        public void T15_CheckMint()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            const int tokensToMint = 10;
            var result = _emulator
                .Execute(Operations.Mint, _scriptHashes[0], tokensToMint)
                .GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T16_CheckBalanceAfterMint()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var tokensToMint = new BigInteger(10);
            var result = _emulator.Execute(Operations.Mint, _scriptHashes[1], tokensToMint).GetBigInteger();
            Console.WriteLine($"Mint result: {result}");

            var balance = _emulator.Execute(Operations.BalanceOf, _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Balance: {balance}");
            Assert.AreEqual(tokensToMint, balance);

            var totalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {totalSupply}");
            Assert.AreEqual(tokensToMint, totalSupply);
        }

        [Test]
        public void T17_CheckMintNotFinishedBeforeFinish()
        {
            var result = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T18_CheckFinishMintByOwner()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T19_CheckFinishMintingNotByOwner()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T20_CheckMintingFinishedAfterFinish()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintingFinished = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {mintingFinished}");
            Assert.IsTrue(mintingFinished);
        }

        [Test]
        public void T21_CheckMintingForbiddenAfterFinish()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[1], new BigInteger(10)).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");
            Assert.IsFalse(mintResult);
        }

        [Test]
        public void T22_CheckTransfer()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;

            var result = _emulator.Execute(Operations.Mint, _scriptHashes[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");

            var transferResult = _emulator
                .Execute(Operations.Transfer, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            Assert.IsTrue(transferResult);
        }

        [Test]
        public void T23_CheckBalanceAfterTransfer()
        {
            var tokensToMint = new BigInteger(10);
            var tokensToTransfer = new BigInteger(7);

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {result}");

            var transferResult = _emulator
                .Execute(Operations.Transfer, _scriptHashes[0], _scriptHashes[1], tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");

            var balanceFrom = _emulator.Execute(Operations.BalanceOf, _scriptHashes[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");

            var balanceTo = _emulator.Execute(Operations.BalanceOf, _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");

            Assert.AreEqual(tokensToMint - tokensToTransfer, balanceFrom);
            Assert.AreEqual(tokensToTransfer, balanceTo);
        }

        [Test]
        public void T24_CheckApprove()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T25_CheckApproveNotByOriginator()
        {
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T26_CheckAllowanceAfterApprove()
        {
            var tokensToApprove = new BigInteger(5);

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            var allowance = _emulator.Execute(Operations.Allowance, _scriptHashes[0], _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(tokensToApprove, allowance);
        }

        [Test]
        public void T27_CheckMintAndApproveAndTransferFrom()
        {
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsTrue(transferResult);
        }

        [Test]
        public void T28_CheckAllowedAfterTransferFrom()
        {
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");

            var allowance = _emulator.Execute(Operations.Allowance, _scriptHashes[0], _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(tokensToApprove - tokensToTransfer, allowance);
        }

        [Test]
        public void T29_CheckBalancesAfterTransferFrom()
        {
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(4);
            var tokensToTransfer = new BigInteger(3);

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");

            var balanceFrom = _emulator.Execute(Operations.BalanceOf, _scriptHashes[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");

            var balanceTo = _emulator.Execute(Operations.BalanceOf, _scriptHashes[2]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");

            Assert.AreEqual(tokensToMint - tokensToTransfer, balanceFrom);
            Assert.AreEqual(tokensToTransfer, balanceTo);
        }

        [Test]
        public void T30_CheckApproveAndTransferFromNotByOriginator()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2], 5)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsFalse(transferResult);
        }
    }
}