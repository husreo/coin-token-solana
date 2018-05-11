using System;
using System.Numerics;
using System.Security.Cryptography;
using Neo;
using Neo.Wallets;
using NEP5.Common;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        public string[] HASHES = new string[] {
            "849921a919a31f42543a8dc3643fcb9e025f20ff",
            "22a4d553282d7eaf53538eb8ccb27e842d0d90b6",
            "bc89c04256bd0a5b9d53a0d239d615a8734bc459",
            "1e66cccfed7a0a9f4bc9bf6c92b286acef65fc77",
            "a42abb913fa551de74fd4626ad4a789a2987e52e",
        };
     
        [SetUp]
        public void InitInteropService()
        {
            TestHelper.Init();
        }

        [Test]
        public void T01_CheckReturnFalseWhenInvalidOperation()
        {
            var result = TestHelper.Execute("invalidOperation").GetBoolean();
            Console.WriteLine($"Result: {result}");
            Assert.IsFalse(result, "Invalid operation execution result should be false");
        }
        
        [Test]
        public void T02_CheckName()
        {
            var name = TestHelper.Execute(Operations.Name).GetString();
            Console.WriteLine($"Token name: {name}");
            Assert.AreEqual("D_NAME", name);
        }
        
        [Test]
        public void T03_CheckSymbol()
        {
            var symbol = TestHelper.Execute(Operations.Symbol).GetString();
            Console.WriteLine($"Token symbol: {symbol}");
            Assert.AreEqual("D_SYMBOL", symbol);
        }
        
        [Test]
        public void T04_CheckDecimals()
        {
            var decimals = TestHelper.Execute(Operations.Decimals).GetBigInteger();
            Console.WriteLine($"Token decimals: {decimals}");
            Assert.AreEqual("D_DECIMALS", decimals.ToString());
        }
        
        [Test]
        public void T05_CheckOwner()
        {
            var owner = TestHelper.Execute(Operations.Owner).GetByteArray().ToHexString();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(HASHES[0], owner);
        }

        [Test]
        public void T06_CheckTotalSupplyIsZeroBeforeDeploy()
        {
            var totalSupply = TestHelper.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {totalSupply}");
            Assert.AreEqual(BigInteger.Zero, totalSupply);
        }

        [Test]
        public void T07_CheckNotPausedBeforePause()
        {
            var result = TestHelper.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T08_CheckPauseNotByOwner()
        {
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T09_CheckPauseByOwner()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T10_CheckPausedAfterPause()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            var paused = TestHelper.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsTrue(paused);
        }

        [Test]
        public void T11_CheckUnpauseAfterPause()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = TestHelper.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsTrue(unpauseResult);
        }

        [Test]
        public void T12_CheckNotPausedAfterUnpause()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = TestHelper.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");

            var paused = TestHelper.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsFalse(paused);
        }

        [Test]
        public void T13_CheckCannotUnpauseAfterUnpause()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = TestHelper.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");

            var secondUnpauseResult = TestHelper.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {secondUnpauseResult}");
            Assert.IsFalse(secondUnpauseResult);
        }

        [Test]
        public void T14_CheckCannotUnpauseNotByOwner()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            TestHelper.InitTransactionContext(HASHES[1], 10);
            var unpauseResult = TestHelper.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsFalse(unpauseResult);
        }

        [Test]
        public void T15_CheckMint()
        {
            var tokensToMint = new BigInteger(10);
            var result = TestHelper.Execute(Operations.Mint, HASHES[1], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T16_CheckBalanceAfterMint()
        {
            var tokensToMint = new BigInteger(10);
            var result = TestHelper.Execute(Operations.Mint, HASHES[1], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {result}");

            var balance = TestHelper.Execute(Operations.BalanceOf, HASHES[1]).GetBigInteger();
            Console.WriteLine($"Balance: {balance}");
            Assert.AreEqual(tokensToMint, balance);

            var totalSupply = TestHelper.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {totalSupply}");
            Assert.AreEqual(tokensToMint, totalSupply);
        }

        [Test]
        public void T17_CheckMintNotFinishedBeforeFinish()
        {
            var result = TestHelper.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T18_CheckFinishMintByOwner()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T19_CheckFinishMintingNotByOwner()
        {
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var result = TestHelper.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T20_CheckMintingFinishedAfterFinish()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintingFinished = TestHelper.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {mintingFinished}");
            Assert.IsTrue(mintingFinished);
        }

        [Test]
        public void T21_CheckMintingForbiddenAfterFinish()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintResult = TestHelper.Execute(Operations.Mint, HASHES[1], new BigInteger(10)).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");
            Assert.IsFalse(mintResult);
        }

        [Test]
        public void T22_CheckTransfer()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Mint, HASHES[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            var transferResult = TestHelper
                .Execute(Operations.Transfer, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            Assert.IsTrue(transferResult);
        }

        [Test]
        public void T23_CheckBalanceAfterTransfer()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Mint, HASHES[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            
            var transferResult = TestHelper
                .Execute(Operations.Transfer, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            
            var balanceFrom = TestHelper.Execute(Operations.BalanceOf, HASHES[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");
            
            var balanceTo = TestHelper.Execute(Operations.BalanceOf, HASHES[1]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");
            
            Assert.AreEqual(5, balanceFrom);
            Assert.AreEqual(5, balanceTo);
        }

        [Test]
        public void T24_CheckApprove()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var result = TestHelper.Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5)).GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T25_CheckApproveNotByOriginator()
        {
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var result = TestHelper.Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5)).GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T26_CheckAllowanceAfterApprove()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var approveResult = TestHelper
                .Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            var allowance = TestHelper.Execute(Operations.Allowance, HASHES[0], HASHES[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(5, allowance);
        }
        
        [Test]
        public void T27_CheckApproveAndTransferFrom()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var approveResult = TestHelper
                .Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var transferResult = TestHelper
                .Execute(Operations.TransferFrom, HASHES[1], HASHES[0], HASHES[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsTrue(transferResult);
        }
        
        [Test]
        public void T28_CheckAllowedAfterTransferFrom()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var approveResult = TestHelper
                .Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var transferResult = TestHelper
                .Execute(Operations.TransferFrom, HASHES[1], HASHES[0], HASHES[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");

            var allowance = TestHelper.Execute(Operations.Allowance, HASHES[0], HASHES[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(new BigInteger(2), allowance);
        }
        
        [Test]
        public void T29_CheckBalancesAfterTransferFrom()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var approveResult = TestHelper
                .Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            TestHelper.InitTransactionContext(HASHES[1], 10);
            var transferResult = TestHelper
                .Execute(Operations.TransferFrom, HASHES[1], HASHES[0], HASHES[2], new BigInteger(3))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            
            var balanceFrom = TestHelper.Execute(Operations.BalanceOf, HASHES[0]).GetBigInteger();
            Console.WriteLine($"Sender balance: {balanceFrom}");
            
            var balanceTo = TestHelper.Execute(Operations.BalanceOf, HASHES[2]).GetBigInteger();
            Console.WriteLine($"Recepient balance: {balanceTo}");
            
            Assert.AreEqual(new BigInteger(2), balanceFrom);
            Assert.AreEqual(new BigInteger(3), balanceTo);
        }

        [Test]
        public void T30_CheckApproveAndTransferFromNotByOriginator()
        {
            TestHelper.InitTransactionContext(HASHES[0], 10);
            var approveResult = TestHelper
                .Execute(Operations.Approve, HASHES[0], HASHES[1], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            TestHelper.InitTransactionContext(HASHES[2], 10);
            var transferResult = TestHelper
                .Execute(Operations.TransferFrom, HASHES[1], HASHES[0], HASHES[2], new BigInteger(5))
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsFalse(transferResult);
        }
    }
}