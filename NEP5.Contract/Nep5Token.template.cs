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
        public static readonly byte[] InitialOwnerScriptHash = "D_OWNER".ToScriptHash();
        
        #if D_PREMINT_COUNT > 0
        #ifdef D_PREMINT_ADDRESS_0
        private static readonly byte[] PremintScriptHash0 = "D_PREMINT_ADDRESS_0".ToScriptHash();
        private static readonly BigInteger PremintAmount0 = new BigInteger(D_PREMINT_AMOUNT_0);
        private static readonly uint PremintFreeze0 = D_PREMINT_FREEZE_0;
        #endif
        #ifdef D_PREMINT_ADDRESS_1
        private static readonly byte[] PremintScriptHash1 = "D_PREMINT_ADDRESS_1".ToScriptHash();
        private static readonly BigInteger PremintAmount1 = new BigInteger(D_PREMINT_AMOUNT_1);
        private static readonly uint PremintFreeze1 = D_PREMINT_FREEZE_1;
        #endif
        #ifdef D_PREMINT_ADDRESS_2
        private static readonly byte[] PremintScriptHash2 = "D_PREMINT_ADDRESS_2".ToScriptHash();
        private static readonly BigInteger PremintAmount2 = new BigInteger(D_PREMINT_AMOUNT_2);
        private static readonly uint PremintFreeze2 = D_PREMINT_FREEZE_2;
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
        
        [DisplayName("finishMint")]
        public static event Action MintFinished;
        
        [DisplayName("init")]
        public static event Action Inited;
 
        [DisplayName("transferOwnership")]
        public static event Action<byte[]> OwnershipTransferred;
        
//        [DisplayName("freeze")]
//        public static event Action<byte[], BigInteger, uint> Freezed;
//        
//        [DisplayName("release")]
//        public static event Action<byte[], BigInteger, uint> Released;

        public static Object Main(string operation, params object[] args)
        {
            if (operation == Operations.Init) return Init();
            if (operation == Operations.Owner) return Owner();
            if (operation == Operations.Name) return Name();
            if (operation == Operations.Symbol) return Symbol();
            if (operation == Operations.Decimals) return Decimals();
            if (operation == Operations.BalanceOf) return BalanceOf((byte[]) args[0]);
//            if (operation == Operations.ActualBalanceOf) return ActualBalanceOf((byte[]) args[0]);
//            if (operation == Operations.FreezingBalanceOf) return FreezingBalanceOf((byte[]) args[0]);
//            if (operation == Operations.Release) return Release((byte[]) args[0]);
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

        public static byte[] Owner()
        {
            return Storage.Get(Storage.CurrentContext, Constants.Owner) ?? InitialOwnerScriptHash;
        }
        
        public static bool Init()
        {
            if (Storage.Get(Storage.CurrentContext, Constants.Inited).AsString() == Constants.Inited) return false;
            bool result = true;
            #if D_PREMINT_COUNT > 0
            #ifdef D_PREMINT_ADDRESS_0
            result = result && _Mint(PremintScriptHash0, PremintAmount0);
//            result = result && _MintOrFreeze(PremintScriptHash0, PremintAmount0, PremintFreeze0);
            #endif
            #ifdef D_PREMINT_ADDRESS_1
            result = result && _Mint(PremintScriptHash1, PremintAmount1);
//            result = result && _MintOrFreeze(PremintScriptHash1, PremintAmount1, PremintFreeze1);
            #endif
            #ifdef D_PREMINT_ADDRESS_2
            result = result && _MintOrFreeze(PremintScriptHash2, PremintAmount2);
//            result = result && _MintOrFreeze(PremintScriptHash2, PremintAmount2, PremintFreeze2);
            #endif
            #endif
            Storage.Put(Storage.CurrentContext, Constants.Inited, Constants.Inited);
            Inited();
            return result;
        }

//        private static bool _MintOrFreeze(byte[] account, BigInteger amount, uint freezeUntil)
//        {
//            return freezeUntil <= Runtime.Time ? _Mint(account, amount) : _Freeze(account, amount, freezeUntil);
//        }
//
//        private static bool _Freeze(byte[] account, BigInteger amount, uint freezeUntil)
//        {
//            if (account.Length != 20) return false;
//            if (amount <= 0) return false;
//            Freezing f = new Freezing
//            {
//                AccountScriptHash = account,
//                Amount = amount,
//                FreezeUntil = freezeUntil
//            };
//            BigInteger freezesCount = Storage.Get(Storage.CurrentContext, "freezesCount").AsBigInteger();
//            Storage.Put(Storage.CurrentContext, "freeze".AsByteArray().Concat(freezesCount.AsByteArray()), f.Serialize());
//            Storage.Put(Storage.CurrentContext, "freezesCount", freezesCount + 1);
//            Storage.Put(Storage.CurrentContext, Constants.TotalSupply, TotalSupply() + amount);
//            Freezed(account, amount, freezeUntil);
//            Transferred(null, account, amount);
//            return true;
//        }
        
        public static BigInteger BalanceOf(byte[] account)
        {
            return ActualBalanceOf(account) + FreezingBalanceOf(account);
        }

        public static BigInteger ActualBalanceOf(byte[] account)
        {
            return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
        }

        public static BigInteger FreezingBalanceOf(byte[] account)
        {
//            BigInteger freezesCount = Storage.Get(Storage.CurrentContext, "freezesCount").AsBigInteger();
            BigInteger balance = 0;
//            for (BigInteger i = 0; i < freezesCount; i += 1)
//            {
//                Freezing f = (Freezing) Storage
//                    .Get(Storage.CurrentContext, "freeze".AsByteArray().Concat(i.AsByteArray()))
//                    .Deserialize();
               
//                if (f.AccountScriptHash == account) {
//                    balance += f.Amount;
//                }
//            }
            return balance;
        }

//        public static bool Release(byte[] account)
//        {
//            if (!Runtime.CheckWitness(account)) return false;
//            BigInteger freezesCount = Storage.Get(Storage.CurrentContext, "freezesCount").AsBigInteger();
//            
//            for (BigInteger i = 0; i < freezesCount; i += 1)
//            {
//                byte[] key = "freeze".AsByteArray().Concat(i.AsByteArray());
//                Freezing f = (Freezing) Storage.Get(Storage.CurrentContext, key).Deserialize();
//               
//                if (f.AccountScriptHash == account && f.FreezeUntil <= Runtime.Time) {
//                    Storage.Put(Storage.CurrentContext, account, ActualBalanceOf(account) + f.Amount);
//                    Storage.Delete(Storage.CurrentContext, key);
//                    Released(f.AccountScriptHash, f.Amount, f.FreezeUntil);
//                }
//            }
//
//            return true;
//        }

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
            if (!Runtime.CheckWitness(Owner())) return false;
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
        
        public static bool TransferOwnership(byte[] target)
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (target.Length != 20) return false;
            Storage.Put(Storage.CurrentContext, Constants.Owner, target);
            OwnershipTransferred(target);
            return true;
        }
    }
}