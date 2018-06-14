using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;
using System.Numerics;
using Common;

namespace NEP5.Contract
{
    public class Nep5Token : SmartContract
    {
        public static string Name() => "D_NAME";
        public static string Symbol() => "D_SYMBOL";
        public static byte Decimals() => D_DECIMALS;
        private static readonly byte[] InitialOwnerScriptHash = "D_OWNER".ToScriptHash();
        
        #if D_PREMINT_COUNT > 0
        #ifdef D_PREMINT_ADDRESS_0
        private static readonly byte[] PremintScriptHash0 = "D_PREMINT_ADDRESS_0".ToScriptHash();
        private static readonly byte[] PremintAmount0 = {D_PREMINT_AMOUNT_0};
        #endif
        #ifdef D_PREMINT_ADDRESS_1
        private static readonly byte[] PremintScriptHash1 = "D_PREMINT_ADDRESS_1".ToScriptHash();
        private static readonly byte[] PremintAmount1 = {D_PREMINT_AMOUNT_1};
        #endif
        #ifdef D_PREMINT_ADDRESS_2
        private static readonly byte[] PremintScriptHash2 = "D_PREMINT_ADDRESS_2".ToScriptHash();
        private static readonly byte[] PremintAmount2 = {D_PREMINT_AMOUNT_2};
        #endif
        #endif
        
        public static string CreationDateTime() => "__DATE__ __TIME__";
        
        [DisplayName("transfer")]
        public static event Types.Action<byte[], byte[], BigInteger> Transferred;
        
        [DisplayName("mint")]
        public static event Types.Action<byte[], BigInteger> Minted;
        
        [DisplayName("finishMint")]
        public static event Types.Action MintFinished;
        
        [DisplayName("init")]
        public static event Types.Action Inited;
 
        [DisplayName("transferOwnership")]
        public static event Types.Action<byte[]> OwnershipTransferred;

        public static object Main(string operation, params object[] args)
        {
            if (operation == Operations.Init) return Init();
            if (operation == Operations.Owner) return Owner();
            if (operation == Operations.Name) return Name();
            if (operation == Operations.Symbol) return Symbol();
            if (operation == Operations.Decimals) return Decimals();
            if (operation == Operations.BalanceOf)
            {
                if (args.Length != 1) return NotifyErrorAndReturnFalse("Arguments count must be 1");
                var account = (byte[]) args[0];
                return BalanceOf(account);
            }

            if (operation == Operations.Transfer)
            {
                if (args.Length != 3) return NotifyErrorAndReturnFalse("Arguments count must be 3");
                var from = (byte[]) args[0];
                var to = (byte[]) args[1];
                var value = (BigInteger) args[2];
                return Transfer(from, to, value);
            }

            if (operation == Operations.TotalSupply) return TotalSupply();
            if (operation == Operations.Allowance)
            {
                if (args.Length != 2) return NotifyErrorAndReturnFalse("Arguments count must be 2");
                var from = (byte[]) args[0];
                var to = (byte[]) args[1];
                return Allowance(from, to);
            }

            if (operation == Operations.Approve)
            {
                if (args.Length != 3) return NotifyErrorAndReturnFalse("Arguments count must be 3");
                var originator = (byte[]) args[0];
                var to = (byte[]) args[1];
                var value = (BigInteger) args[2];
                return Approve(originator, to, value);
            }

            if (operation == Operations.TransferFrom)
            {
                if (args.Length != 4) return NotifyErrorAndReturnFalse("Arguments count must be 4");
                var originator = (byte[]) args[0];
                var from = (byte[]) args[1];
                var to = (byte[]) args[2];
                var value = (BigInteger) args[3];
                return TransferFrom(originator, from, to, value);
            }

            if (operation == Operations.Mint)
            {
                if (args.Length != 2) return NotifyErrorAndReturnFalse("Arguments count must be 2");
                var to = (byte[]) args[0];
                var value = (BigInteger) args[1];
                return Mint(to, value);
            }

            if (operation == Operations.FinishMinting) return FinishMinting();
            if (operation == Operations.MintingFinished) return MintingFinished();
            if (operation == Operations.Pause) return Pause();
            if (operation == Operations.Paused) return Paused();
            if (operation == Operations.Unpause) return Unpause();
            if (operation == Operations.TransferOwnership)
            {
                if (args.Length != 1) return NotifyErrorAndReturnFalse("Arguments count must be 1");
                var target = (byte[]) args[0];
                return TransferOwnership(target);
            }

            return NotifyErrorAndReturnFalse("Unknown operation");
        }

        public static byte[] Owner()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Owner) ?? InitialOwnerScriptHash;
        }
        
        public static bool Init()
        {
            if (Storage.Get(Storage.CurrentContext, Constants.Inited).AsString() == Constants.Inited)
            {
                return NotifyErrorAndReturnFalse("Already initialized");
            }
            bool result = true;
            #if D_PREMINT_COUNT > 0
            #ifdef D_PREMINT_ADDRESS_0
            result = result && _Mint(PremintScriptHash0, PremintAmount0.AsBigInteger());
            #endif
            #ifdef D_PREMINT_ADDRESS_1
            result = result && _Mint(PremintScriptHash1, PremintAmount1.AsBigInteger());
            #endif
            #ifdef D_PREMINT_ADDRESS_2
            result = result && _Mint(PremintScriptHash2, PremintAmount2.AsBigInteger());
            #endif
            #endif
            
            #if defined(D_CONTINUE_MINTING) && !D_CONTINUE_MINTING
            result = result && _FinishMinting();
            #endif
            
            Storage.Put(Storage.CurrentContext, Constants.Inited, Constants.Inited);
            Inited();
            return result;
        }
        
        public static BigInteger BalanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return NotifyErrorAndReturnFalse("Value should be positive");
            if (!Runtime.CheckWitness(from))
            {
                return NotifyErrorAndReturnFalse("Owner of the wallet isn't associated with this invoke");
            }
            if (Paused()) return NotifyErrorAndReturnFalse("Token transfer is paused");
            if (to.Length != 20) return NotifyErrorAndReturnFalse("To value must be script hash (size of 20)");
            BigInteger fromBalance = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (fromBalance < value) return NotifyErrorAndReturnFalse("Sender doesn't have enough tokens");
            if (from == to) return true;
            if (fromBalance == value)
            {
                Storage.Delete(Storage.CurrentContext, from);
            }
            else
            {
                Storage.Put(Storage.CurrentContext, from, fromBalance - value);
            }
            
            BigInteger recipientBalance = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, recipientBalance + value);

            Transferred(from, to, value);
            return true;
        }
        
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, Constants.TotalSupply).AsBigInteger();
        }

        public static BigInteger Allowance(byte[] from, byte[] to)
        {
            return Storage.Get(Storage.CurrentContext, from.Concat(to)).AsBigInteger();
        }

        public static bool Approve(byte[] originator, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(originator))
            {
                return NotifyErrorAndReturnFalse("Originator isn't associated with this invoke");
            }
            if (to.Length != 20) return NotifyErrorAndReturnFalse("To value must be script hash (size of 20)");
            Storage.Put(Storage.CurrentContext, originator.Concat(to), value);
            return true;
        }

        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(originator))
            {
                return NotifyErrorAndReturnFalse("Originator isn't associated with this invoke");
            }
            if (Paused()) return NotifyErrorAndReturnFalse("Token transfer is paused");
            if (from.Length != 20) return NotifyErrorAndReturnFalse("From value must be script hash (size of 20)");
            if (to.Length != 20) return NotifyErrorAndReturnFalse("To value must be script hash (size of 20)");;
            byte[] key = from.Concat(originator);
            BigInteger allowed = Allowance(from, originator);
            BigInteger fromBalance = BalanceOf(from);
            BigInteger toBalance = BalanceOf(to);
            if (allowed < value)
            {
                return NotifyErrorAndReturnFalse("You are trying to send more than you are allowed to");
            }
            if (fromBalance < value) return NotifyErrorAndReturnFalse("Owner doesn't have enough tokens");
            if (allowed == value)
            {
                Storage.Delete(Storage.CurrentContext, key);
            }
            else
            {
                Storage.Put(Storage.CurrentContext, key, allowed - value);
            }

            if (fromBalance == value)
            {
                Storage.Delete(Storage.CurrentContext, from);
            }
            else
            {
                Storage.Put(Storage.CurrentContext, from, fromBalance - value);
            }
            
            Storage.Put(Storage.CurrentContext, to, toBalance + value);

            Transferred(from, to, value);
            return true;
        }

        public static bool Mint(byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(Owner()))
            {
                return NotifyErrorAndReturnFalse("You are not the owner of the contract");
            }
            if (MintingFinished()) return NotifyErrorAndReturnFalse("Minting is finished");
            return _Mint(to, value);
        }

        private static bool _Mint(byte[] to, BigInteger value)
        {
            if (to.Length != 20) return NotifyErrorAndReturnFalse("To value must be script hash (size of 20)");
            if (value <= 0) return NotifyErrorAndReturnFalse("Value should be positive");
            Storage.Put(Storage.CurrentContext, to, BalanceOf(to) + value);
            Storage.Put(Storage.CurrentContext, Constants.TotalSupply, TotalSupply() + value);
            Minted(to, value);
            Transferred(null, to, value);
            return true;
        }

        public static bool FinishMinting()
        {
            if (!Runtime.CheckWitness(Owner()))
            {
                return NotifyErrorAndReturnFalse("You are not the owner of the contract");
            }
            return _FinishMinting();
        }

        private static bool _FinishMinting()
        {
            if (MintingFinished()) return NotifyErrorAndReturnFalse("Minting already finished");
            Storage.Put(Storage.CurrentContext, Constants.MintingFinished, Constants.MintingFinished);
            MintFinished();
            return true;
        }

        public static bool MintingFinished()
        {
            byte[] isMintingFinished = Storage.Get(Storage.CurrentContext, Constants.MintingFinished);
            return isMintingFinished.AsString() == Constants.MintingFinished;
        }
        
        public static bool Pause()
        {
            if (!Runtime.CheckWitness(Owner()))
            {
                return NotifyErrorAndReturnFalse("You are not the owner of the contract");
            }
            if (Paused()) return NotifyErrorAndReturnFalse("Transfers are paused");
            Storage.Put(Storage.CurrentContext, Constants.Paused, Constants.Paused);
            return true;
        }

        public static bool Paused()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Paused).AsString() == Constants.Paused;
        }
        
        public static bool Unpause()
        {
            if (!Runtime.CheckWitness(Owner()))
            {
                return NotifyErrorAndReturnFalse("You are not the owner of the contract");
            }
            if (!Paused()) return NotifyErrorAndReturnFalse("Transfers are not paused");
            Storage.Delete(Storage.CurrentContext, Constants.Paused);
            return true;
        }
        
        public static bool TransferOwnership(byte[] target)
        {
            if (!Runtime.CheckWitness(Owner()))
            {
                return NotifyErrorAndReturnFalse("You are not the owner of the contract");
            }
            if (target.Length != 20) return NotifyErrorAndReturnFalse("Target value must be script hash (size of 20)");
            Storage.Put(Storage.CurrentContext, Constants.Owner, target);
            OwnershipTransferred(target);
            return true;
        }
        
        private static bool NotifyErrorAndReturnFalse(string error)
        {
            Runtime.Notify(error);
            return false;
        }
    }
}