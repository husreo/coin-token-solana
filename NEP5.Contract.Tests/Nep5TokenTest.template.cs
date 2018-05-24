using System;
using System.Collections.Generic;
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

        public void ExecuteInit()
        {
            var initResult = _emulator.Execute(Operations.Init, _scriptHashes[0]).GetBoolean();
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
        public void T16_CheckMint()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            const int tokensToMint = 10;
            var result = _emulator
                .Execute(Operations.Mint, _scriptHashes[0], tokensToMint)
                .GetBoolean();
            Console.WriteLine($"Mint result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T17_CheckBalanceAfterMint()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
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

        [Test]
        public void T18_CheckMintNotFinishedBeforeFinish()
        {
            ExecuteInit();
            var result = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T19_CheckFinishMintByOwner()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T20_CheckFinishMintingNotByOwner()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysFalse;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T21_CheckMintingFinishedAfterFinish()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintingFinished = _emulator.Execute(Operations.MintingFinished).GetBoolean();
            Console.WriteLine($"Minting finished: {mintingFinished}");
            Assert.IsTrue(mintingFinished);
        }

        [Test]
        public void T22_CheckMintingForbiddenAfterFinish()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator.Execute(Operations.FinishMinting).GetBoolean();
            Console.WriteLine($"Finish minting result: {result}");
            var mintResult = _emulator.Execute(Operations.Mint, _scriptHashes[1], new BigInteger(10)).GetBoolean();
            Console.WriteLine($"Mint result: {mintResult}");
            Assert.IsFalse(mintResult);
        }

        [Test]
        public void T23_CheckTransfer()
        {
            ExecuteInit();
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
        public void T24_CheckBalanceAfterTransfer()
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
        public void T25_CheckApprove()
        {
            ExecuteInit();
            _emulator.checkWitnessMode = CheckWitnessMode.AlwaysTrue;
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsTrue(result);
        }

        [Test]
        public void T26_CheckApproveNotByOriginator()
        {
            ExecuteInit();
            var result = _emulator
                .Execute(Operations.Approve, _scriptHashes[0], _scriptHashes[1], 5)
                .GetBoolean();
            Console.WriteLine($"Approve result: {result}");
            Assert.IsFalse(result);
        }

        [Test]
        public void T27_CheckAllowanceAfterApprove()
        {
            ExecuteInit();
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
        public void T28_CheckMintAndApproveAndTransferFrom()
        {
            ExecuteInit();
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
        public void T29_CheckAllowedAfterTransferFrom()
        {
            ExecuteInit();
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
        public void T30_CheckBalancesAfterTransferFrom()
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
        public void T31_CheckApproveAndTransferFromNotByOriginator()
        {
            ExecuteInit();
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

        #if D_PREMINT_COUNT > 0
        private readonly string[] addresses = {
            #ifdef D_PREMINT_ADDRESS_0
            "D_PREMINT_ADDRESS_0",
            #endif
            #ifdef D_PREMINT_ADDRESS_1
            "D_PREMINT_ADDRESS_1",
            #endif
            #ifdef D_PREMINT_ADDRESS_2
            "D_PREMINT_ADDRESS_2",
            #endif
        };
                                    
        private readonly BigInteger[] amounts = {
            #ifdef D_PREMINT_AMOUNT_0
            new BigInteger(D_PREMINT_AMOUNT_0),
            #endif
            #ifdef D_PREMINT_AMOUNT_1
            new BigInteger(D_PREMINT_AMOUNT_1),
            #endif
            #ifdef D_PREMINT_AMOUNT_2
            new BigInteger(D_PREMINT_AMOUNT_2),
            #endif
        };
                                    
        private readonly long[] freezes = {
            #ifdef D_PREMINT_FREEZE_0
            D_PREMINT_FREEZE_0,
            #endif
            #ifdef D_PREMINT_FREEZE_1
            D_PREMINT_FREEZE_1,
            #endif
            #ifdef D_PREMINT_FREEZE_2
            D_PREMINT_FREEZE_2,
            #endif
        };

        [Test]
        public void T32_CheckPremintedBalances()
        {
            ExecuteInit();
            
            var addressesToAmounts = new Dictionary<string, BigInteger>();
            for (var i = 0; i < addresses.Length; i++)
            {
                addressesToAmounts[addresses[i]] = addressesToAmounts.ContainsKey(addresses[i])
                    ? addressesToAmounts[addresses[i]] + amounts[i]
                    : amounts[i];
            }
            
            var balances = addressesToAmounts.Keys
                .Select(a => _emulator.Execute(Operations.BalanceOf, a.GetScriptHashFromAddress()).GetBigInteger())
                .ToArray();
                    
            var addressesToBalances = new Dictionary<string, BigInteger>();
            var j = 0;
            foreach (var a in addressesToAmounts.Keys)
            {
                addressesToBalances[a] = balances[j++];
            }
            
            foreach (var key in addressesToAmounts.Keys)
            {
                Console.WriteLine($"Premint amount: {addressesToAmounts[key]}");
                Console.WriteLine($"Premint balance: {addressesToBalances[key]}");
                Assert.AreEqual(addressesToAmounts[key], addressesToBalances[key]);
            }
            
            var calculatedTotalSupply = BigInteger.Zero;
            amounts.ToList().ForEach(a => calculatedTotalSupply += a);
            var realTotalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {realTotalSupply}");
            Assert.AreEqual(calculatedTotalSupply, realTotalSupply, "TotalSupply should be equals");
        }
        
//        [Test]
//        public void T33_CheckFreezes() {
//            ExecuteInit();
//            
//            var addressesToAmounts = new Dictionary<string, BigInteger>();
//            for (var i = 0; i < addresses.Length; i++)
//            {
//                addressesToAmounts[addresses[i]] = addressesToAmounts.ContainsKey(addresses[i])
//                    ? addressesToAmounts[addresses[i]] + amounts[i]
//                    : amounts[i];
//            }
//            
//            var addressesToFreezings = new Dictionary<string, List<long>>();
//            for (var i = 0; i < addresses.Length; i++)
//            {
//                if (!addressesToFreezings.ContainsKey(addresses[i]))
//                {
//                    addressesToFreezings[addresses[i]] = new List<long>();
//                }
//
//                var freezings = addressesToFreezings[addresses[i]];
//                freezings.Add(freezes[i]);
//                addressesToFreezings[addresses[i]] = freezings;
//            }
//            
//            var actualBalances = addressesToAmounts.Keys
//                .Select(a => _emulator.Execute(Operations.ActualBalanceOf, a.GetScriptHashFromAddress()).GetBigInteger())
//                .ToArray();
//            
//            var addressesToActualBalances = new Dictionary<string, BigInteger>();
//            var j = 0;
//            foreach (var a in addressesToAmounts.Keys)
//            {
//                addressesToActualBalances[a] = actualBalances[j++];
//            }
//            
//            var freezingBalances = addressesToAmounts.Keys
//                .Select(a => _emulator.Execute(Operations.FreezingBalanceOf, a.GetScriptHashFromAddress()).GetBigInteger())
//                .ToArray();
//            
//            var addressesToFreezingBalances = new Dictionary<string, BigInteger>();
//            var k = 0;
//            foreach (var a in addressesToAmounts.Keys)
//            {
//                addressesToFreezingBalances[a] = freezingBalances[k++];
//            }
//            
//            Console.WriteLine($"Now: {_emulator.timestamp}");
//            foreach (var key in addressesToAmounts.Keys)
//            {
//                Console.WriteLine($"Premint amount: {addressesToAmounts[key]}");
//                Console.WriteLine($"Premint freezing: {addressesToFreezings[key]}");
//                Console.WriteLine($"Premint actualBalance: {addressesToActualBalances[key]}");
//                Console.WriteLine($"Premint freezingBalance: {addressesToFreezingBalances[key]}");
//
//                if (addressesToFreezings[key].TrueForAll(freezing => freezing <= _emulator.timestamp))
//                {
//                    Assert.AreEqual(addressesToAmounts[key], addressesToActualBalances[key]);
//                    Assert.AreEqual(BigInteger.Zero, addressesToFreezingBalances[key]);
//                }
//                else 
//                {
//                    Assert.AreEqual(addressesToAmounts[key], addressesToFreezingBalances[key]);
//                    Assert.AreEqual(BigInteger.Zero, addressesToActualBalances[key]);
//                }
//            }
//        }
        #endif
            
        [Test]
        public void T33_CheckTransferOwnership()
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
    }
}