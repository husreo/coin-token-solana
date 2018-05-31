using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Common;

namespace Crowdsale.Contract
{
    public class Crowdsale : SmartContract
    {
        //Token Settings
        private static readonly byte[] InitialOwnerScriptHash = "APyEx5f4Zm4oCHwFWiSTaph1fPBxZacYVR".ToScriptHash();
        private const ulong Factor = 100000000;
        private const ulong DecimalsMultiplier = 100000000;

        //ICO Settings
        private static readonly byte[] NeoAssetId = {155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197};
        private const ulong TotalAmount = 100000000 * Factor; // total token amount
        private const ulong BasicRate = 1000 * Factor;
        private const int StartTime = 1506787200;
        private const int EndTime = 1538323200;

        [DisplayName("buyTokens")]
        public static event Types.Action<byte[], ulong, ulong> TokenPurchase; 
        
        [DisplayName("refund")]
        public static event Types.Action<byte[], BigInteger> Refund;
 
        [DisplayName("transferOwnership")]
        public static event Types.Action<byte[]> OwnershipTransferred;

        [Appcall("612047ef0f529e02d6907047d91c1fb4e14e51ca")]
        private static extern object Nep5Call(string operation, params object[] args);
        
        public static object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }

            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "mintTokens") return MintTokens();
                if (operation == "owner") return Owner();
                if (operation == Operations.TransferOwnership)
                {
                    if (args.Length != 1) return false;
                    var target = (byte[]) args[0];
                    return TransferOwnership(target);
                }
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

        public static byte[] Owner()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Owner) ?? InitialOwnerScriptHash;
        }
        
        public static bool TransferOwnership(byte[] target)
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (target.Length != 20) return false;
            Storage.Put(Storage.CurrentContext, Constants.Owner, target);
            OwnershipTransferred(target);
            return true;
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
            ulong rate = GetRate();
            // crowdfunding failure
            if (rate == 0)
            {
                Refund(sender, contributeValue);
                return false;
            }

            // you can get current swap token amount
            ulong tokens = GetTokensCount(sender, contributeValue, rate);
            if (tokens == 0)
            {
                return false;
            }

            Runtime.Log("MINTING");
            var result = (bool) Nep5Call(Operations.Mint, sender, tokens);
            Runtime.Log("RETURNING");
            TokenPurchase(sender, contributeValue, tokens);
            return result;
        }

        // The function CurrentSwapRate() returns the current exchange rate
        // between ico tokens and neo during the token swap period
        private static ulong GetRate()
        {
            var now = Runtime.Time;
            if (StartTime <= now && now <= EndTime)
            {
                return BasicRate;
            }

            return 0;
        }

        //whether over contribute capacity, you can get the token amount
        private static ulong GetTokensCount(byte[] sender, ulong value, ulong rate)
        {
            ulong token = value / DecimalsMultiplier * rate;
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger balanceToken = TotalAmount - totalSupply;
            if (balanceToken <= 0)
            {
                Refund(sender, value);
                return 0;
            }

            if (balanceToken < token)
            {
                Refund(sender, (token - balanceToken) / rate * DecimalsMultiplier);
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