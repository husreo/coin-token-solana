using System.Collections;
using Neo;
using Neo.VM;

namespace NEP5.Contract.Tests
{
    internal class CustomStorageContext : IInteropInterface
    {
        public UInt160 ScriptHash;

        public Hashtable Data;

        public CustomStorageContext()
        {
            Data = new Hashtable();
        }

        public byte[] ToArray()
        {
            return ScriptHash.ToArray();
        }
    }
}
