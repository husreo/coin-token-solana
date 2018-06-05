using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        private byte[][] _scriptHashes;

        private static Blockchain _chain;
        private static Emulator _emulator;
        private static Account _owner;
        private static Account _account1;
        private static Account _account2;
        
        private static readonly BigInteger Rate = new BigInteger(D_RATE);
        private static readonly BigInteger HardCapNeo = new BigInteger(D_HARD_CAP_NEO);
        private static readonly uint StartTime = D_START_TIME;
        private static readonly uint EndTime = D_END_TIME;
        private static readonly BigInteger DecimalsMultiplier = BigInteger.Pow(10, D_DECIMALS);

        [SetUp]
        public void Setup()
        {
            _chain = new Blockchain();
            _emulator = new Emulator(_chain);
            _owner = _chain.DeployContract("owner", File.ReadAllBytes(TestHelper.CrowdsaleContractFilePath));
            
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
        public void T16_CheckTransferOwnership()
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

        [Test]
        public void T17_CheckSimpleBuy()
        {
            ExecuteInitWithoutTransferringOwnership();

            _emulator.timestamp = StartTime;
            var buyerScriptHash = _owner.keys.address.AddressToScriptHash();
            var neo = BigInteger.One;
            var balanceBefore = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            
            _emulator.SetTransaction(NeoAssetId, neo);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            Assert.IsTrue(buyResult);
            
            var balanceAfter = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            Console.WriteLine($"Neo sent: {neo}");
            Console.WriteLine($"Tokens buy: {balanceAfter - balanceBefore}");
            Assert.AreEqual(neo * Rate * DecimalsMultiplier, balanceAfter - balanceBefore);
        }

        [Test]
        public void T18_CheckBuyHardCap()
        {
            ExecuteInitWithoutTransferringOwnership();

            _emulator.timestamp = StartTime;
            var buyerScriptHash = _owner.keys.address.AddressToScriptHash();
            var neo = HardCapNeo;
            var balanceBefore = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            
            _emulator.SetTransaction(NeoAssetId, neo);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            Assert.IsTrue(buyResult);
            
            var balanceAfter = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            Console.WriteLine($"Neo sent: {neo}");
            Console.WriteLine($"Tokens buy: {balanceAfter - balanceBefore}");
            Assert.AreEqual(HardCapNeo * Rate * DecimalsMultiplier, balanceAfter - balanceBefore);
        }

        [Test]
        public void T19_CheckBuyMoreHardCapNotAllowed()
        {
            ExecuteInitWithoutTransferringOwnership();

            _emulator.timestamp = StartTime;
            var buyerScriptHash = _owner.keys.address.AddressToScriptHash();
            var neo = HardCapNeo + 1;
            var balanceBefore = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            
            _emulator.SetTransaction(NeoAssetId, neo);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            Assert.IsTrue(buyResult);
            
            var balanceAfter = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            Console.WriteLine($"Neo sent: {neo}");
            Console.WriteLine($"Tokens buy: {balanceAfter - balanceBefore}");
            Assert.AreEqual(HardCapNeo * Rate * DecimalsMultiplier, balanceAfter - balanceBefore);
        }

        [Test]
        public void T20_CheckBuyNotAllowedAfterHardCapReached()
        {
            ExecuteInitWithoutTransferringOwnership();

            _emulator.timestamp = StartTime;
            var buyerScriptHash = _owner.keys.address.AddressToScriptHash();
            var neo = HardCapNeo;
            var balanceBefore = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            
            _emulator.SetTransaction(NeoAssetId, neo);
            var buy1Result = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buy1Result}");
            Assert.IsTrue(buy1Result);
            
            var balanceAfter = _emulator.Execute(Operations.BalanceOf, buyerScriptHash).GetBigInteger();
            Console.WriteLine($"Neo sent: {neo}");
            Console.WriteLine($"Tokens buy: {balanceAfter - balanceBefore}");

            _emulator.SetTransaction(NeoAssetId, 1);
            var buy2Result = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buy2Result}");
            Assert.IsFalse(buy2Result);
        }

        [Test]
        public void T21_CheckBuyNotAllowedBeforeCrowdsaleStart()
        {
            ExecuteInitWithoutTransferringOwnership();
            _emulator.timestamp = StartTime - 1;
            _emulator.SetTransaction(NeoAssetId, 1);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            Assert.IsFalse(buyResult);
        }

        [Test]
        public void T22_CheckBuyNotAllowedAfterCrowdsaleEnd()
        {
            ExecuteInitWithoutTransferringOwnership();
            _emulator.timestamp = EndTime + 1;
            _emulator.SetTransaction(NeoAssetId, 1);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            Assert.IsFalse(buyResult);
        }

        #if !defined(D_CONTINUE_MINTING) || D_CONTINUE_MINTING
        [Test]
        public void T23_CheckTransfer()
        {
            ExecuteInitWithTransferringOwnership();
            _emulator.timestamp = StartTime;
            _emulator.SetTransaction(NeoAssetId, 10);
            var result = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {result}");
            
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
            var neoToSend = new BigInteger(10);
            var tokensToMint = neoToSend * Rate * DecimalsMultiplier;
            var tokensToTransfer = new BigInteger(7);
            
            _emulator.timestamp = StartTime;
            _emulator.SetTransaction(NeoAssetId, neoToSend);
            var result = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {result}");
            
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
            var neoToSend = new BigInteger(5);
            var tokensToMint = neoToSend * Rate * DecimalsMultiplier;
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);
            
            _emulator.timestamp = StartTime;
            _emulator.SetTransaction(NeoAssetId, neoToSend);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            
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
            var neoToSend = new BigInteger(5);
            var tokensToMint = neoToSend * Rate * DecimalsMultiplier;
            var tokensToApprove = new BigInteger(5);
            var tokensToTransfer = new BigInteger(3);
            
            _emulator.timestamp = StartTime;
            _emulator.SetTransaction(NeoAssetId, neoToSend);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            
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
        public void T30_CheckBalancesAfterTransferFrom()
        {
            ExecuteInitWithTransferringOwnership();
            var neoToSend = new BigInteger(5);
            var tokensToMint = neoToSend * Rate * DecimalsMultiplier;
            var tokensToApprove = new BigInteger(4);
            var tokensToTransfer = new BigInteger(3);
            
            _emulator.timestamp = StartTime;
            _emulator.SetTransaction(NeoAssetId, neoToSend);
            var buyResult = _emulator.Execute(Operations.MintTokens).GetBoolean();
            Console.WriteLine($"Buy result: {buyResult}");
            
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
        public void T31_CheckApproveAndTransferFromNotByOriginator()
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
        private readonly string[] _addresses = {
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

        private readonly BigInteger[] _amounts = {
            #ifdef D_PREMINT_AMOUNT_0
            new BigInteger(new byte[] {D_PREMINT_AMOUNT_0}),
            #endif
            #ifdef D_PREMINT_AMOUNT_1
            new BigInteger(new byte[] {D_PREMINT_AMOUNT_1}),
            #endif
            #ifdef D_PREMINT_AMOUNT_2
            new BigInteger(new byte[] {D_PREMINT_AMOUNT_2}),
            #endif
        };

        [Test]
        public void T32_CheckPremintedBalances()
        {
            ExecuteInitWithTransferringOwnership();
            
            var addressesToAmounts = new Dictionary<string, BigInteger>();
            for (var i = 0; i < _addresses.Length; i++)
            {
                addressesToAmounts[_addresses[i]] = addressesToAmounts.ContainsKey(_addresses[i])
                    ? addressesToAmounts[_addresses[i]] + _amounts[i]
                    : _amounts[i];
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
            _amounts.ToList().ForEach(a => calculatedTotalSupply += a);
            var realTotalSupply = _emulator.Execute(Operations.TotalSupply).GetBigInteger();
            Console.WriteLine($"Total supply: {realTotalSupply}");
            Assert.AreEqual(calculatedTotalSupply, realTotalSupply, "TotalSupply should be equals");
        }
        #endif
    }
}
