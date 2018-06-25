using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Lux.Utils;
using Common;
using NUnit.Framework;

namespace NEP5.Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        private byte[][] _scriptHashes;

        private static Blockchain _chain;
        private static Emulator _emulator;
        private static Account _owner;
        private static Account _account1;
        private static Account _account2;

        [SetUp]
        public void Setup()
        {
            _chain = new Blockchain();
            _emulator = new Emulator(_chain);
            _owner = _chain.DeployContract("owner", File.ReadAllBytes(TestHelper.Nep5ContractFilePath));
            
            _chain.CreateAddress("account1");
            _account1 = _chain.FindAddressByName("account1");
            _chain.CreateAddress("account2");
            _account2 = _chain.FindAddressByName("account2");
            
            _emulator.SetExecutingAccount(_owner);
            Runtime.invokerKeys = _owner.keys;

            _scriptHashes = new[] {_owner, _account1, _account2}
                .Select(a => a.keys.address.AddressToScriptHash())
                .ToArray();
        }

        private static void ExecuteInitWithoutTransferringOwnership()
        {
            var initResult = _emulator.Execute(Operations.Init).GetBoolean();
            Console.WriteLine($"Init result: {initResult}");
            Assert.IsTrue(initResult);
        }

        private static void ExecuteInitWithTransferringOwnership()
        {
            ExecuteInitWithoutTransferringOwnership();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var transferOwnershipResult = _emulator
                .Execute(Operations.TransferOwnership, _owner.keys.address.AddressToScriptHash())
                .GetBoolean();
            _emulator.checkWitnessMode = CheckWitnessMode.Default;
            Console.WriteLine($"TransferOwnership result: {transferOwnershipResult}");
            Assert.IsTrue(transferOwnershipResult);
        }

        [Test]
        public void T01_CheckReturnFalseWhenInvalidOperation()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute("invalidOperation").GetBoolean();
            Console.WriteLine($"Result: {result}");
            Assert.IsFalse(result, "Invalid operation execution result should be false");
        }

        [Test]
        public void T02_CheckName()
        {
            ExecuteInitWithTransferringOwnership();
            var name = _emulator.Execute(Operations.Name).GetString();
            Console.WriteLine($"Token name: {name}");
            Assert.AreEqual("D_NAME", name);
        }

        [Test]
        public void T03_CheckSymbol()
        {
            ExecuteInitWithTransferringOwnership();
            var symbol = _emulator.Execute(Operations.Symbol).GetString();
            Console.WriteLine($"Token symbol: {symbol}");
            Assert.AreEqual("D_SYMBOL", symbol);
        }

        [Test]
        public void T04_CheckDecimals()
        {
            ExecuteInitWithTransferringOwnership();
            var decimals = _emulator.Execute(Operations.Decimals).GetBigInteger();
            Console.WriteLine($"Token decimals: {decimals}");
            Assert.AreEqual("D_DECIMALS", decimals.ToString());
        }

        [Test]
        public void T05_CheckOwnerBeforeInit()
        {
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual("D_OWNER".AddressToScriptHash().ByteToHex(), owner);
        }
        
        [Test]
        public void T06_CheckOwnerAfterInit()
        {
            ExecuteInitWithoutTransferringOwnership();
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual("D_OWNER".AddressToScriptHash().ByteToHex(), owner);
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
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T09_CheckPauseNotByOwner()
        {
            ExecuteInitWithTransferringOwnership();
            Runtime.invokerKeys = _account1.keys;
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T10_CheckPauseByOwner()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T11_CheckPausedAfterPause()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");
            var paused = _emulator.Execute(Operations.Paused).GetBoolean();
            Console.WriteLine($"Paused: {paused}");
            Assert.IsTrue(paused);
        }

        [Test]
        public void T12_CheckUnpauseAfterPause()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsTrue(unpauseResult);
        }

        [Test]
        public void T13_CheckNotPausedAfterUnpause()
        {
            ExecuteInitWithTransferringOwnership();
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
            ExecuteInitWithTransferringOwnership();
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
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {result}");

            Runtime.invokerKeys = _account1.keys;
            var unpauseResult = _emulator.Execute(Operations.Unpause).GetBoolean();
            Console.WriteLine($"Unpause result: {unpauseResult}");
            Assert.IsFalse(unpauseResult);
        }

        [Test]
        public void T16_CheckMint()
        {
            ExecuteInitWithTransferringOwnership();
            const int tokensToMint = 10;
            var result = _emulator
                .Execute(Operations.Mint, _scriptHashes[0], tokensToMint)
                .GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            
            #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
            Assert.IsTrue(result);
            #else
            Assert.IsFalse(result);
            #endif
        }

        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T17_CheckBalanceAfterMint()
        {
            ExecuteInitWithTransferringOwnership();
            var tokensToMint = new BigInteger(10);
            
            var totalSupplyBeforeMint = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply before mint: {totalSupplyBeforeMint}");
            
            var result = _emulator.Execute(Operations.Mint, _scriptHashes[1], tokensToMint).GetBigInteger();
            Console.WriteLine($"Mint result: {result}");

            var balance = _emulator.Execute(Operations.BalanceOf, _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Balance: {balance}");
            Assert.AreEqual(tokensToMint, balance);

            var totalSupplyAfterMint = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply after mint: {totalSupplyAfterMint}");
            Assert.AreEqual(tokensToMint, totalSupplyAfterMint - totalSupplyBeforeMint);
        }
        #endif

        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T18_CheckMintNotFinishedBeforeFinish()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T19_CheckFinishMintByOwner()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsTrue(result);
        }
        #endif

        [Test]
        public void T20_CheckFinishMintingNotByOwner()
        {
            ExecuteInitWithTransferringOwnership();
            Runtime.invokerKeys = _account1.keys;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T21_CheckMintingFinishedAfterFinish()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintingFinished = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {mintingFinished}");
            Assert.IsTrue(mintingFinished);
        }

        [Test]
        public void T22_CheckMintingForbiddenAfterFinish()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[1], 10).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");
            Assert.IsFalse(mintResult);
        }
        
        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T23_CheckTransfer()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Mint, _scriptHashes[0], 10).GetBoolean();
            Console.WriteLine($"Mint result: {result}");

            var transferResult = _emulator
                .Execute(Operations.Transfer, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            Assert.IsTrue(transferResult);
        }
        #endif

        [Test]
        public void T24_CheckBalanceAfterTransfer()
        {
            var tokensToMint = new BigInteger(10);
            var tokensToTransfer = new BigInteger(7);

            ExecuteInitWithTransferringOwnership();
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
        public void T25_CheckApprove()
        {
            ExecuteInitWithTransferringOwnership();
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T26_CheckApproveNotByOriginator()
        {
            ExecuteInitWithTransferringOwnership();
            Runtime.invokerKeys = _account1.keys;
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T27_CheckAllowanceAfterApprove()
        {
            ExecuteInitWithTransferringOwnership();
            var tokensToApprove = new BigInteger(5);

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            var allowance = _emulator.Execute(Operations.Allowance, _scriptHashes[0], _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(tokensToApprove, allowance);
        }

        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T28_CheckMintAndApproveAndTransferFrom()
        {
            ExecuteInitWithTransferringOwnership();
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);

            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            Runtime.invokerKeys = _account1.keys;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsTrue(transferResult);
        }
        #endif

        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T29_CheckAllowedAfterTransferFrom()
        {
            ExecuteInitWithTransferringOwnership();
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);

            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            Runtime.invokerKeys = _account1.keys;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");

            var allowance = _emulator.Execute(Operations.Allowance, _scriptHashes[0], _scriptHashes[1]).GetBigInteger();
            Console.WriteLine($"Allowance: {allowance}");
            Assert.AreEqual(tokensToApprove - tokensToTransfer, allowance);
        }
        #endif

        [Test]
        public void T30_CheckPauseAndTransfer()
        {
            var tokensToMint = new BigInteger(10);
            var tokensToTransfer = new BigInteger(7);

            ExecuteInitWithTransferringOwnership();
            var result = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {result}");

            var pauseResult = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {pauseResult}");
            
            var transferResult = _emulator
                .Execute(Operations.Transfer, _scriptHashes[0], _scriptHashes[1], tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"Transfer result: {transferResult}");
            Assert.IsFalse(transferResult);
        }

        [Test]
        public void T31_CheckPauseAndTransferFrom()
        {
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);

            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");
            
            var pauseResult = _emulator.Execute(Operations.Pause).GetBoolean();
            Console.WriteLine($"Pause result: {pauseResult}");
            
            Runtime.invokerKeys = _account1.keys;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2],
                    tokensToTransfer)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsFalse(transferResult);
        }

        [Test]
        public void T32_CheckBalancesAfterTransferFrom()
        {
            ExecuteInitWithTransferringOwnership();
            var tokensToMint = new BigInteger(5);
            var tokensToApprove = new BigInteger(4);
            var tokensToTransfer = new BigInteger(3);

            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[0], tokensToMint).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");

            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], tokensToApprove)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            Runtime.invokerKeys = _account1.keys;
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
        public void T33_CheckApproveAndTransferFromNotByOriginator()
        {
            ExecuteInitWithTransferringOwnership();
            var approveResult = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {approveResult}");

            Runtime.invokerKeys = _account1.keys;
            var transferResult = _emulator
                .Execute(Operations.TransferFrom, _scriptHashes[1], _scriptHashes[0], _scriptHashes[2], 5)
                .GetBoolean();
            Console.WriteLine($"TransferFrom result: {transferResult}");
            Assert.IsFalse(transferResult);
        }

        #if D_PREMINT_COUNT > 0
        private readonly int _premintCount = D_PREMINT_COUNT;
        private readonly byte[] _premintScriptHashes = {D_PREMINT_SCRIPT_HASHES};
        private readonly byte[] _premintAmounts = {D_PREMINT_AMOUNTS};

        public class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] left, byte[] right)
            {
                if (left == null || right == null)
                {
                    return left == right;
                }

                if (left.Length != right.Length)
                {
                    return false;
                }

                return !left.Where((t, i) => t != right[i]).Any();
            }

            public int GetHashCode(byte[] key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                return key.Aggregate(0, (current, cur) => current + cur);
            }
        }

        public static ByteArrayComparer Comparer = new ByteArrayComparer();
    
        [Test]
        public void T34_CheckPremintedBalances()
        {
            ExecuteInitWithTransferringOwnership();
            
            var scriptHashesToAmounts = new Dictionary<byte[], BigInteger>(Comparer);
            for (var i = 0; i < _premintCount; i++)
            {
                var scriptHash = new List<byte>(_premintScriptHashes).GetRange(i * 20, 20).ToArray();
                var amount = new BigInteger(new List<byte>(_premintAmounts).GetRange(i * 33, 33).ToArray());
                scriptHashesToAmounts[scriptHash] = scriptHashesToAmounts.ContainsKey(scriptHash)
                    ? scriptHashesToAmounts[scriptHash] + amount
                    : amount;
            }
            
            var balances = scriptHashesToAmounts.Keys
                .Select(sh => _emulator.Execute(Operations.BalanceOf, sh).GetBigInteger())
                .ToArray();
                    
            var addressesToBalances = new Dictionary<byte[], BigInteger>(Comparer);
            var j = 0;
            foreach (var sh in scriptHashesToAmounts.Keys)
            {
                addressesToBalances[sh] = balances[j++];
            }
            
            foreach (var key in scriptHashesToAmounts.Keys)
            {
                Console.WriteLine($"Premint amount: {scriptHashesToAmounts[key]}");
                Console.WriteLine($"Premint balance: {addressesToBalances[key]}");
                Assert.AreEqual(scriptHashesToAmounts[key], addressesToBalances[key]);
            }
            
            var calculatedTotalSupply = BigInteger.Zero;
            for (var i = 0; i < _premintCount; i++)
            {
                calculatedTotalSupply += new BigInteger(new List<byte>(_premintAmounts).GetRange(i * 33, 33).ToArray());
            }
            var realTotalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {realTotalSupply}");
            Assert.AreEqual(calculatedTotalSupply, realTotalSupply, "TotalSupply should be equals");
        }
        #endif

        [Test]
        public void T35_CheckTransferOwnership()
        {
            ExecuteInitWithTransferringOwnership();
            var transferOwnershipResult = _emulator
                .Execute(Operations.TransferOwnership, _scriptHashes[1])
                .GetBoolean();
            Console.WriteLine($"TransferOwnership result: {transferOwnershipResult}");
            Assert.IsTrue(transferOwnershipResult);
            
            var owner = _emulator.Execute(Operations.Owner).GetByteArray().ByteToHex();
            Console.WriteLine($"Owner: {owner}");
            Assert.AreEqual(_scriptHashes[1].ByteToHex(), owner);
        }
    }
}