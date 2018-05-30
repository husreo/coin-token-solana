using LunarParser;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Emulation.API
{
    public class Block : Header
    {
        public uint height;

        private List<Transaction> _transactions = new List<Transaction>();
        public IEnumerable<Transaction> Transactions { get { return _transactions; } }

        public int TransactionCount { get { return _transactions.Count; } }

        public Block(uint height, uint timestamp) : base(timestamp)
        {
            this.height = height;
        }

        internal void AddTransaction(Transaction tx)
        {
            if (_transactions.Contains(tx))
            {
                return;
            }

            _transactions.Add(tx);
        }

        public Transaction GetTransactionByIndex(int index)
        {
            return _transactions[index];
        }

        internal bool Load(DataNode root)
        {
            this.timestamp = root.GetUInt32("timestamp");

            this._transactions.Clear();

            foreach (var child in root.Children)
            {
                if (child.Name == "transaction")
                {
                    var tx = new Transaction(this);
                    tx.Load(child);
                    _transactions.Add(tx);
                }
            }

            return true;
        }

        public DataNode Save()
        {
            var result = DataNode.CreateObject("block");
            foreach (var tx in _transactions)
            {
                result.AddNode(tx.Save());
            }

            result.AddField("timestamp", timestamp);

            return result;
        }

        [Syscall("Neo.Block.GetTransactionCount")]
        public bool GetTransactionCount(ExecutionEngine engine)
        {
            var obj = engine.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            engine.EvaluationStack.Push(block._transactions.Count);
            return true;
        }

        [Syscall("Neo.Block.GetTransactions")]
        public bool GetTransactions(ExecutionEngine engine)
        {
            var obj = engine.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            // returns Transaction[]

            var txs = new StackItem[block._transactions.Count];

            int index = 0;
            foreach (var tx in block.Transactions)
            {
                txs[index] = new VM.Types.InteropInterface(tx);
                index++;
            }

            var array = new VM.Types.Array(txs);

            throw new NotImplementedException();
        }

        
        [Syscall("Neo.Block.GetTransaction")]
        public bool GetTransaction(ExecutionEngine engine)
        {
            var index = (int)engine.EvaluationStack.Pop().GetBigInteger();
            var obj = engine.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            if (index<0 || index>=block._transactions.Count)
            {
                return false;
            }

            var tx = block.GetTransactionByIndex(index);
            engine.EvaluationStack.Push(new VM.Types.InteropInterface(tx));
            return true;
        }

    }
}
