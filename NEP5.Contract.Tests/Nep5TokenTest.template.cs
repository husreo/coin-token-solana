using System;
using System.Numerics;
using Neo.Emulation;
using Neo.Emulation.API;
using NEP5.Common;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        private readonly string[] _addresses = {
            "59E75D652B5D3827BF04C165BBE9EF95CCA4BF55",
            "C85145B41CBDA9272E3141A396468F262569FF6B",
            "849921A919A31F42543A8DC3643FCB9E025F20FF",
        };
     
        private static Blockchain _chain;
        private static Emulator _emulator;

        private static Account _owner;
        private static Account _executor1;

        [SetUp]
        public void Setup()
        {
            _chain = new Blockchain();
            _emulator = new Emulator(_chain);
            _owner = _chain.DeployContract("owner", TestHelper.Avm);
            _emulator.SetExecutingAccount(_owner);
            
            _chain.CreateAddress("executor1");
            _executor1 = _chain.FindAddressByName("executor1");
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
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(_addresses[0], owner);
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
            var tokensToMint = new BigInteger(10);
            var result = _emulator.Execute(Operations.Mint, _addresses[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            Assert.IsTrue(result);
        }
        
        [Test]
        public void T16_CheckBalanceAfterMint()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var tokensToMint = new BigInteger(10);
            var result = _emulator.Execute(Operations.Mint, tokensToMint).GetBigInteger();
            Console.WriteLine($"Mint result: {result}");

            var balance = _emulator.Execute(Operations.BalanceOf, _addresses[1]).GetBigInteger();
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
            var mintResult = _emulator.Execute(Operations.Mint, _addresses[1], new BigInteger(10)).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");
            Assert.IsFalse(mintResult);
        }

        [Test]
        public void T22_CheckTransfer()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Mint, _addresses[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            
            var transferResult = _emulator
                .Execute(Operations.Transfer, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            Assert.IsTrue(transferResult);
        }

        [Test]
        public void T23_CheckBalanceAfterTransfer()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.Mint, _addresses[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            
            var transferResult = _emulator
                .Execute(Operations.Transfer, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            
            var balanceFrom = _emulator.Execute(Operations.BalanceOf, _addresses[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");
            
            var balanceTo = _emulator.Execute(Operations.BalanceOf, _addresses[1]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");
            
            Assert.AreEqual(new BigInteger(5), balanceFrom);
            Assert.AreEqual(new BigInteger(5), balanceTo);
        }

        [Test]
        public void T24_CheckApprove()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T25_CheckApproveNotByOriginator()
        {
            var result = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T26_CheckAllowanceAfterApprove()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            var allowance = _emulator.Execute(Operations.Allowance, _addresses[0], _addresses[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(new BigInteger(5), allowance);
        }
        
        [Test]
        public void T27_CheckApproveAndTransferFrom()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _addresses[1], _addresses[0], _addresses[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsTrue(transferResult);
        }
        
        [Test]
        public void T28_CheckAllowedAfterTransferFrom()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _addresses[1], _addresses[0], _addresses[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");

            var allowance = _emulator.Execute(Operations.Allowance, _addresses[0], _addresses[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(new BigInteger(2), allowance);
        }
        
        [Test]
        public void T29_CheckBalancesAfterTransferFrom()
        {
            var approveResult = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _addresses[1], _addresses[0], _addresses[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            
            var balanceFrom = _emulator.Execute(Operations.BalanceOf, _addresses[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");
            
            var balanceTo = _emulator.Execute(Operations.BalanceOf, _addresses[2]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");
            
            Assert.AreEqual(new BigInteger(2), balanceFrom);
            Assert.AreEqual(new BigInteger(3), balanceTo);
        }

        [Test]
        public void T30_CheckApproveAndTransferFromNotByOriginator()
        {
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var approveResult = _emulator
                .Execute(Operations.Approve, _addresses[0], _addresses[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _addresses[1], _addresses[0], _addresses[2], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsFalse(transferResult);
        }
    }
}
