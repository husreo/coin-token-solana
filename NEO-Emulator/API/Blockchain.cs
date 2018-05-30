using LunarParser;
using LunarParser.JSON;
using Neo.Lux.Cryptography;
using Neo.Lux.Utils;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Neo.Emulation.API
{
    public class Blockchain
    {
        public uint currentHeight { get { return (uint)_blocks.Count; } }
        public IEnumerable<Account> Accounts { get { return _accounts; } }
        public int AddressCount { get { return _accounts.Count; } }

        public IEnumerable<Block> Blocks { get { return _blocks.Values; } }

        private Dictionary<uint, Block> _blocks = new Dictionary<uint, Block>();
        private List<Account> _accounts = new List<Account>();

        public string fileName { get; set; }

        public Block currentBlock
        {
            get
            {
                if (_blocks.Count == 0)
                {
                    return null;
                }
                return _blocks[currentHeight];
            }
        }

        public static readonly string InitialPrivateWIF = "KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr";

        public Blockchain()
        {
            this.fileName = null;
            Reset();
        }

        public void Reset()
        {
            var keypair = KeyPair.FromWIF(InitialPrivateWIF);

            int amount = 10000;

            var balances = new Dictionary<string, decimal>();
            balances["NEO"] = amount;
            balances["GAS"] = amount;

            _accounts.Clear();
            _accounts.Add(new Account() { name = "Genesis", balances = balances, byteCode = null, keys = keypair, storage = null });

            _blocks.Clear();
            var block = GenerateBlock();

            var hash = new UInt160(CryptoUtils.AddressToScriptHash(keypair.address));

            var tx = new Transaction(block);

            foreach (var entry in balances)
            {
                BigInteger total = (BigInteger)amount * Asset.Decimals;
                tx.outputs.Add(new TransactionOutput(Asset.GetAssetId(entry.Key), total, hash));
            }

            ConfirmBlock(block);
        }

        public Block GenerateBlock()
        {
            var block = new Block(currentHeight + 1, DateTime.Now.ToTimestamp());
            return block;
        }

        // this verifies that this block is valid as the next block in the chain
        // if thats true, then it updates balances from all accounts in the included transactions
        public bool ConfirmBlock(Block block)
        {
            if (block.height != currentHeight + 1)
            {
                return false;
            }

            if (block.TransactionCount == 0)
            {
                return false;
            }

            foreach (var tx in block.Transactions)
            {
                foreach (var output in tx.outputs)
                {
                    var address = FindAccountByHash(output.hash);
                    if (address == null)
                    {
                        return false;
                    }
                }
            }

            _blocks[block.height] = block;
            return true;
        }

        public Account FindAccountByHash(UInt160 hash)
        {
            foreach (var entry in _accounts)
            {
                var bytes = CryptoUtils.AddressToScriptHash(entry.keys.address);
                var temp = new UInt160(bytes);
                if (temp.Equals(hash))
                {
                    return entry;
                }
            }

            return null;
        }

        public Account FindAccountByAddress(string address)
        {
            foreach (var entry in _accounts)
            {
                if (entry.keys.address == address)
                {
                    return entry;
                }
            }

            return null;
        }

        public Block GetBlockByHeight(uint height)
        {
            if (_blocks.ContainsKey(height))
            {
                return _blocks[height];
            }

            return null;
        }

        public bool Load(string fileName)
        {
            this.fileName = fileName;

            if (!File.Exists(fileName))
            {
                return false;
            }

            var json = File.ReadAllText(fileName);
            var root = JSONReader.ReadFromString(json);

            root = root["blockchain"];

            _blocks.Clear();
            _accounts.Clear();

            foreach (var child in root.Children)
            {
                if (child.Name.Equals("block"))
                {
                    uint index = (uint)(_blocks.Count + 1);
                    var block = new Block(index, 0);
                    if (block.Load(child))
                    {
                        _blocks[index] = block;
                    }
                }
                if (child.Name.Equals("address"))
                {
                    var address = new Account();
                    if (address.Load(child))
                    {
                        _accounts.Add(address);
                    }
                }
            }

            return true;
        }

        public void CreateAddress(string name)
        {
            var bytes = new byte[32];
            var rnd = new Random();
            rnd.NextBytes(bytes);

            var keypair = new KeyPair(bytes);
            var address = new Account() { keys = keypair, balances = new Dictionary<string, decimal>(), byteCode = null, name = name, storage = null };
            
            _accounts.Add(address);
        }

        public void Save()
        {
            if (this.fileName == null)
            {
                throw new Exception("Blockchain filename cannot be null");
            }

            this.Save(this.fileName);
        }

        public bool Save(string fileName)
        {
            this.fileName = fileName;

            var result = DataNode.CreateObject("blockchain");
            for (uint i = 1; i <= _blocks.Count; i++)
            {
                var block = _blocks[i];
                result.AddNode(block.Save());
            }

            foreach (var address in _accounts)
            {
                result.AddNode(address.Save());
            }

            try
            {
                var json = JSONWriter.WriteToString(result);
                File.WriteAllText(fileName, json);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private Account GenerateAddress(string name)
        {
            byte[] array = new byte[32];
            var random = new Random();
            random.NextBytes(array);

            var keys = new KeyPair(array);

            var address = new Account();
            address.name = name;
            address.keys = keys;

            this._accounts.Add(address);

            return address;
        }

        public Account DeployContract(string name, byte[] byteCode)
        {
            var address = GenerateAddress(name);
            address.byteCode = byteCode;
            return address;
        }

        [Syscall("Neo.Blockchain.GetHeight")]
        public static bool GetHeight(ExecutionEngine engine)
        {
            var blockchain = engine.GetBlockchain();
            engine.EvaluationStack.Push(blockchain.currentHeight);

            return true;
        }

        [Syscall("Neo.Blockchain.GetHeader", 0.1)]
        public static bool GetHeader(ExecutionEngine engine)
        {
            var obj = engine.EvaluationStack.Pop();

            Block block = null;

            var hash = obj.GetByteArray();

            if (hash.Length > 1)
            {
                throw new NotImplementedException();
            }

            var blockchain = engine.GetBlockchain();

            if (hash.Length == 1)
            {
                var temp = obj.GetBigInteger();

                var height = (uint)temp;

                if (blockchain._blocks.ContainsKey(height))
                {
                    block = blockchain._blocks[height];
                }
                else
                if (height <= blockchain.currentHeight)
                {
                    uint index = height + 1;
                    block = new Block(index, 1506787300);
                    blockchain._blocks[index] = block;
                }
            }

            if (block == null)
            {
            }

            engine.EvaluationStack.Push(new VM.Types.InteropInterface(block));
            return true;
            // returns Header
        }

        public Account FindAddressByName(string name)
        {
            foreach (var addr in _accounts)
            {
                if (addr.name.Equals(name))
                {
                    return addr;
                }
            }

            return null;
        }

        [Syscall("Neo.Blockchain.GetBlock", 0.2)]
        public static bool GetBlock(ExecutionEngine engine)
        {
            //uint height
            //OR
            //byte[] hash
            //returns Block 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Blockchain.GetTransaction", 0.1)]
        public static bool GetTransaction(ExecutionEngine engine)
        {
            //byte[] hash
            //returns Transaction 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Blockchain.GetAccount", 0.1)]
        public static bool GetAccount(ExecutionEngine engine)
        {
            //byte[] script_hash
            // returns Account 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Blockchain.GetValidators", 0.2)]
        public static bool GetValidators(ExecutionEngine engine)
        {
            //returns byte[][]
            throw new NotImplementedException();
        }

        [Syscall("Neo.Blockchain.GetAsset", 0.1)]
        public static bool GetAsset(ExecutionEngine engine)
        {
            //byte[] asset_id
            // returns Asset
            throw new NotImplementedException();
        }

        [Syscall("Neo.Blockchain.GetContract", 0.1)]
        public static bool GetContract(ExecutionEngine engine)
        {
            //byte[] script_hash
            //returns Contract
            throw new NotImplementedException();
        }
    }
}
