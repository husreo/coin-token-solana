using System.Numerics;

namespace NEP5.Contract
{
    public struct Freezing
    {
        public byte[] AccountScriptHash;
        public BigInteger Amount;
        public uint FreezeUntil;
    }
}