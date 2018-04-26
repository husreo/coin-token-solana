using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NEP5Token
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
            if (Owner() == new byte[] {0})
            {
                if (operation == "deploy")
                {
                    if (args.Length != 1) return false;
                    byte[] originator = (byte[]) args[0];
                    return Deploy(originator);
                }

                return false;
            }
            
            if (operation == "owner") return Owner();
            if (operation == "name") return Name();
            if (operation == "symbol") return Symbol();
            if (operation == "decimals") return Decimals();
            if (operation == "balanceOf")
            {
                if (args.Length != 1) return 0;
                byte[] address = (byte[]) args[0];
                return BalanceOf(address);
            }
            if (operation == "transfer")
            {
                if (args.Length != 3) return false;
                byte[] from = (byte[]) args[0];
                byte[] to = (byte[]) args[1];
                BigInteger value = (BigInteger) args[2];
                return Transfer(from, to, value);
            }
            if (operation == "totalSupply") return TotalSupply();

            if (operation == "allowance")
            {
                if (args.Length != 2) return 0;
                byte[] from = (byte[]) args[0];
                byte[] to = (byte[]) args[1];
                return Allowance(from, to);
            }
            if (operation == "approve")
            {
                if (args.Length != 3) return false;
                byte[] from = (byte[]) args[0];
                byte[] to = (byte[]) args[1];
                BigInteger value = (BigInteger) args[2];
                return Approve(from, to, value);
            }
            if (operation == "transferFrom")
            {
                if (args.Length != 4) return false;
                byte[] originator = (byte[]) args[0];
                byte[] from = (byte[]) args[1];
                byte[] to = (byte[]) args[2];
                BigInteger value = (BigInteger) args[3];
                return TransferFrom(originator, from, to, value);
            }

            if (operation == "mint")
            {
                if (args.Length != 2) return false;
                byte[] to = (byte[]) args[0];
                BigInteger value = (BigInteger) args[1];
                return Mint(to, value);
            }
            if (operation == "finishMinting") return FinishMinting();
            if (operation == "mintingFinished") return MintingFinished();
            
            if (operation == "pause") return Pause();
            if (operation == "paused") return Paused();
            if (operation == "unpause") return Unpause();

            if (operation == "transferOwnership")
            {
                if (args.Length != 1) return false;
                byte[] to = (byte[]) args[0];
                return TransferOwnership(to);
            }
            
            return true;
        }

        public static bool Deploy(byte[] originator)
        {
            Storage.Put(Storage.CurrentContext, "owner", originator);
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
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
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
            Storage.Put(Storage.CurrentContext, "totalSupply", TotalSupply() + value);
            Minted(to, value);
            Transferred(null, to, value);
            return true;
        }

        public static bool FinishMinting()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (MintingFinished()) return false;
            Storage.Put(Storage.CurrentContext, "mintingFinished", "mintingFinished");
            MintFinished();
            return true;
        }

        public static bool MintingFinished()
        {
            return Storage.Get(Storage.CurrentContext, "mintingFinished").AsString() == "mintingFinished";
        }
        
        public static bool Pause()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (Paused()) return false;
            Storage.Put(Storage.CurrentContext, "paused", "paused");
            return true;
        }

        public static bool Paused()
        {
            return Storage.Get(Storage.CurrentContext, "paused").AsString() == "paused";
        }
        
        public static bool Unpause()
        {
            if (!Runtime.CheckWitness(Owner())) return false;
            if (!Paused()) return false;
            Storage.Delete(Storage.CurrentContext, "paused");
            return true;
        }
        
        public static bool TransferOwnership(byte[] to)
        {
            byte[] owner = Owner();
            if (!Runtime.CheckWitness(owner)) return false;
            if (owner == to) return false;
            Storage.Put(Storage.CurrentContext, "owner", to);
            return true;
        }

        public static byte[] Owner()
        {
            return Storage.Get(Storage.CurrentContext, "owner");
        }
    }
}