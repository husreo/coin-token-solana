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
        public static readonly byte[] Owner = "D_OWNER".ToScriptHash();

        #if D_PREMINT_COUNT > 0
        #ifdef D_PREMINT_ADDRESS_0
        private static readonly byte[] PremintAddress0 = "D_PREMINT_ADDRESS_0".ToScriptHash();
        private static readonly BigInteger PremintAmount0 = new BigInteger(D_PREMINT_AMOUNT_0);
        #endif
        #ifdef D_PREMINT_ADDRESS_1
        private static readonly byte[] PremintAddress1 = "D_PREMINT_ADDRESS_1".ToScriptHash();
        private static readonly BigInteger PremintAmount1 = new BigInteger(D_PREMINT_AMOUNT_1);
        #endif
        #ifdef D_PREMINT_ADDRESS_2
        private static readonly byte[] PremintAddress2 = "D_PREMINT_ADDRESS_2".ToScriptHash();
        private static readonly BigInteger PremintAmount2 = new BigInteger(D_PREMINT_AMOUNT_2);
        #endif
        #endif
        
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
            #if D_PREMINT_COUNT > 0
            if (operation == Operations.Init) return Init();
            #endif
            if (operation == Operations.Owner) return Owner;
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

            return false;
        }

        #if D_PREMINT_COUNT > 0
        public static bool Init()
        {
            if (Storage.Get(Storage.CurrentContext, Constants.Inited).AsString() == Constants.Inited) return false;
            #ifdef D_PREMINT_ADDRESS_0
            _Mint(PremintAddress0, PremintAmount0);
            #endif
            #ifdef D_PREMINT_ADDRESS_1
            _Mint(PremintAddress1, PremintAmount1);
            #endif
            #ifdef D_PREMINT_ADDRESS_2
            _Mint(PremintAddress2, PremintAmount2);
            #endif
            Storage.Put(Storage.CurrentContext, Constants.Inited, Constants.Inited);
            return true;
        }
        #endif
        
        public static BigInteger BalanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (to.Length != 20) return false;
            BigInteger fromBalance = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (fromBalance < value) return false;
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
            if (!Runtime.CheckWitness(originator)) return false;
            if (to.Length != 20) return false;
            Storage.Put(Storage.CurrentContext, originator.Concat(to), value);
            return true;
        }

        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger value)
        {
            if (!Runtime.CheckWitness(originator)) return false;
            if (from.Length != 20) return false;
            if (to.Length != 20) return false;
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
            if (!Runtime.CheckWitness(Owner)) return false;
            if (MintingFinished()) return false;
            return _Mint(to, value);
        }

        private static bool _Mint(byte[] to, BigInteger value)
        {
            if (to.Length != 20) return false;
            if (value <= 0) return false;
            Storage.Put(Storage.CurrentContext, to, BalanceOf(to) + value);
            Storage.Put(Storage.CurrentContext, Constants.TotalSupply, TotalSupply() + value);
            Minted(to, value);
            Transferred(null, to, value);
            return true;
        }

        public static bool FinishMinting()
        {
            if (!Runtime.CheckWitness(Owner)) return false;
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
            if (!Runtime.CheckWitness(Owner)) return false;
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
            if (!Runtime.CheckWitness(Owner)) return false;
            if (!Paused()) return false;
            Storage.Delete(Storage.CurrentContext, Constants.Paused);
            return true;
        }
    }
}