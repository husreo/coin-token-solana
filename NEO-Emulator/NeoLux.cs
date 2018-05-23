// PROVIDE NEOLUX API to interact with the emulator

using System.Collections.Generic;
using Neo.Lux.Cryptography;
using Neo.Emulation.API;
using Neo.Lux.Core;
using Neo.Lux.Utils;

namespace Neo.Emulation
{
    public class NeoEmulator : NeoAPI
    {
        private Blockchain blockchain;
        private Emulator emulator;

        public NeoEmulator()
        {
            this.blockchain = new Blockchain();
            this.emulator = new Emulator(this.blockchain);
        }

        public override Dictionary<string, decimal> GetAssetBalancesOf(string address)
        {
            var acc = blockchain.FindAccountByAddress(address);
            var result = new Dictionary<string, decimal>();
            foreach (var entry in acc.balances)
            {
                result[entry.Key] = entry.Value;
            }
            return result;
        }

        public override byte[] GetStorage(string scriptHash, byte[] key)
        {
            var bytes = scriptHash.HexToBytes();
            var hash = new UInt160(bytes);
            var acc = blockchain.FindAccountByHash(hash);

            if (acc == null || acc.storage == null)
            {
                return null;
            }

            return acc.storage.Read(key);
        }

        public override Dictionary<string, List<UnspentEntry>> GetUnspent(string address)
        {
            var acc = blockchain.FindAccountByAddress(address);
            var result = new Dictionary<string, List<UnspentEntry>>();
            foreach (var entry in acc.balances)
            {
                var unspents = new List<UnspentEntry>();
                unspents.Add(new UnspentEntry() { index = 0, txid = "", value = entry.Value });
                result[entry.Key] = unspents;
            }

            return result;
        }

        public override bool SendRawTransaction(string hexTx)
        {
            var bytes = hexTx.HexToBytes();
            var tx = Neo.Lux.Core.Transaction.Unserialize(bytes);

            switch (tx.type)
            {
                case Lux.Core.TransactionType.ContractTransaction:
                    {
                        var loaderScript = tx.script;
                        string methodName = null;

                        emulator.Reset(loaderScript, null, methodName);

                        var state = emulator.Run();
                        return true;
                    }

                default:
                    {
                        return false;
                    }
            }
        }

        protected override string GetRPCEndpoint()
        {
            throw new System.NotImplementedException();
        }

        public override Lux.Core.Transaction GetTransaction(string hash)
        {
            throw new System.NotImplementedException();
        }

        public override InvokeResult TestInvokeScript(byte[] scriptHash, object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
