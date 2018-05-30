using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using NEP5.Common;

namespace Crowdsale.Contract
{
    public class Crowdsale : SmartContract
    {
        //Token Settings
        public static readonly byte[] Owner = "ATrzHaicmhRj15C3Vv6e6gLfLqhSD2PtTr".ToScriptHash();
        private const ulong Factor = 100000000; //decided by Decimals()
        private const ulong DecimalsMultiplier = 100000000;

        //ICO Settings
        private static readonly byte[] NeoAssetId = {155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197};
        private const ulong TotalAmount = 100000000 * Factor; // total token amount
        private const ulong BasicRate = 1000 * Factor;
        private const int StartTime = 1506787200;
        private const int EndTime = 1538323200;

        [DisplayName("transfer")]
        public static event Types.Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("refund")]
        public static event Types.Action<byte[], BigInteger> Refund;

        [Appcall("d2cc940d7b95d8520656351707b84c0125b4cbbb")]
        public static extern Object Nep5Call(string operation, params object[] args);
        
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[]) args[0];
                    return BalanceOf(account);
                }
                if (operation == "mintTokens") return MintTokens();
            }

            //you can choice refund or not refund
            byte[] sender = GetSender();
            ulong contributeValue = GetContributeValue();
            if (contributeValue > 0 && sender.Length != 0)
            {
                Refund(sender, contributeValue);
            }

            return false;
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        // The function MintTokens is only usable by the chosen wallet
        // contract to mint a number of tokens proportional to the
        // amount of neo sent to the wallet contract. The function
        // can only be called during the tokenswap period
        public static bool MintTokens()
        {
            byte[] sender = GetSender();
            // contribute asset is not neo
            if (sender.Length == 0)
            {
                return false;
            }

            ulong contributeValue = GetContributeValue();
            // the current exchange rate between ico tokens and neo during the token swap period
            ulong swapRate = CurrentSwapRate();
            // crowdfunding failure
            if (swapRate == 0)
            {
                Refund(sender, contributeValue);
                return false;
            }

            // you can get current swap token amount
            ulong token = CurrentSwapToken(sender, contributeValue, swapRate);
            if (token == 0)
            {
                return false;
            }

            // crowdfunding success
            BigInteger balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
            Storage.Put(Storage.CurrentContext, sender, token + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", token + totalSupply);
            Transferred(null, sender, token);
            return true;
        }

        // get the total token supply
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // The function CurrentSwapRate() returns the current exchange rate
        // between ico tokens and neo during the token swap period
        private static ulong CurrentSwapRate()
        {
            const int icoDuration = EndTime - StartTime;
            uint now = Runtime.Time;
            int time = (int) now - StartTime;
            if (time < 0)
            {
                return 0;
            }

            if (time < icoDuration)
            {
                return BasicRate;
            }

            return 0;
        }

        //whether over contribute capacity, you can get the token amount
        private static ulong CurrentSwapToken(byte[] sender, ulong value, ulong swapRate)
        {
            ulong token = value / DecimalsMultiplier * swapRate;
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger balanceToken = TotalAmount - totalSupply;
            if (balanceToken <= 0)
            {
                Refund(sender, value);
                return 0;
            }

            if (balanceToken < token)
            {
                Refund(sender, (token - balanceToken) / swapRate * DecimalsMultiplier);
                token = (ulong) balanceToken;
            }

            return token;
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction) ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == NeoAssetId) return output.ScriptHash;
            }

            return new byte[] { };
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // get all you contribute neo amount
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction) ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == NeoAssetId)
                {
                    value += (ulong) output.Value;
                }
            }

            return value;
        }
    }
}