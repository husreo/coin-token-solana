using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using NEP5.Common;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NEP5.Contract
{
    public class Nep5Token : SmartContract
    {
        public static string Name() => "D_NAME";
        public static string Symbol() => "D_SYMBOL";
        public static byte Decimals() => D_DECIMALS;

        public delegate void Action();
        public delegate void Action<in T1>(T1 arg1);
        public delegate void Action<in T1, in T2>(T1 arg1, T2 arg2);
        public delegate void Action<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
        
        [DisplayName("transfer")] 
        public static event Action<byte[], byte[], BigInteger> Transferred;
        
        [DisplayName("mint")] 
        public static event Action<byte[], BigInteger> Minted;
        
        [DisplayName("mintFinish")]
        public static event Action MintFinished;

        public static Object Main(string operation, params object[] args)
        {
            if (operation == Operations.Deploy) return Deploy((byte[]) args[0]);
            if (operation == Operations.Owner) return Owner();
            if (operation == Operations.Name) return Name();
            if (operation == Operations.Symbol) return Symbol();
            if (operation == Operations.Decimals) return Decimals();
            if (operation == Operations.BalanceOf) return BalanceOf((byte[]) args[0]);
            if (operation == Operations.Transfer) return Transfer((byte[]) args[0], (byte[]) args[1], (BigInteger) args[2]);
            if (operation == Operations.TotalSupply) return TotalSupply();
            if (operation == Operations.Allowance) return Allowance((byte[]) args[0], (byte[]) args[1]);
            if (operation == Operations.Approve) return Approve((byte[]) args[0], (byte[]) args[1], (BigInteger) args[2]);
            if (operation == Operations.TransferFrom) return TransferFrom((byte[]) args[0], (byte[]) args[1], (byte[]) args[2], (BigInteger) args[3]);
            if (operation == Operations.Mint) return Mint((byte[]) args[0], (BigInteger) args[1]);
            if (operation == Operations.FinishMinting) return FinishMinting();
            if (operation == Operations.MintingFinished) return MintingFinished();
            if (operation == Operations.Pause) return Pause();
            if (operation == Operations.Paused) return Paused();
            if (operation == Operations.Unpause) return Unpause();
            if (operation == Operations.TransferOwnership) return TransferOwnership((byte[]) args[0]);
            
            return false;
        }

        public static bool Deploy(byte[] originator)
        {
            Storage.Put(Storage.CurrentContext, Constants.Owner, originator);
            return true;
        }

        public static BigInteger BalanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger fromBalance = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (fromBalance < value) return false;
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
            if (!Runtime.CheckWitness(originator)) return false;
            Storage.Put(Storage.CurrentContext, originator.Concat(to), value);
            return true;
        }

        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(originator)) return false;
            byte[] key = from.Concat(originator);
            BigInteger allowed = Allowance(from, originator);
            BigInteger fromBalance = BalanceOf(from);
            BigInteger toBalance = BalanceOf(to);
            if (allowed < value || fromBalance < value) return false;
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
            if (!Runtime.CheckWitness(Owner())) return false;
            if (MintingFinished()) return false;
            Storage.Put(Storage.CurrentContext, to, BalanceOf(to) + value);
            Storage.Put(Storage.CurrentContext, Constants.TotalSupply, TotalSupply() + value);
            Minted(to, value);
            Transferred(null, to, value);
            return true;
        }

        public static bool FinishMinting()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (MintingFinished()) return false;
            Storage.Put(Storage.CurrentContext, Constants.MintingFinished, Constants.MintingFinished);
            MintFinished();
            return true;
        }

        public static bool MintingFinished()
        {
            return Storage.Get(Storage.CurrentContext, Constants.MintingFinished).AsString() == Constants.MintingFinished;
        }
        
        public static bool Pause()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (Paused()) return false;
            Storage.Put(Storage.CurrentContext, Constants.Paused, Constants.Paused);
            return true;
        }

        public static bool Paused()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Paused).AsString() == Constants.Paused;
        }
        
        public static bool Unpause()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (!Paused()) return false;
            Storage.Delete(Storage.CurrentContext, Constants.Paused);
            return true;
        }
        
        public static bool TransferOwnership(byte[] to)
        {
            byte[] owner = Owner();
            if (!Runtime.CheckWitness(owner)) return false;
            if (owner == to) return false;
            Storage.Put(Storage.CurrentContext, Constants.Owner, to);
            return true;
        }

        public static byte[] Owner()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Owner);
        }
    }
}