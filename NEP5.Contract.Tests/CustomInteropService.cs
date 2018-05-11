using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo;
using Neo.Core;
using Neo.VM;

namespace NEP5.Contract.Tests
{
    internal sealed class CustomInteropService : InteropService
    {
        public readonly CustomStorageContext StorageContext;
        public readonly Hashtable Transactions;
        private const uint Date = 1000;

        public CustomInteropService()
        {
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.Get", Storage_Get);
            Register("Neo.Storage.Put", Storage_Put);
//            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Output.GetValue", Output_GetValue);
            Register("Neo.Output.GetAssetId", Output_GetAssetId);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("Neo.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("System.ExecutionEngine.GetExecutingScriptHash", ExecutionEngine_GetExecutingScriptHash);

            StorageContext = new CustomStorageContext();
            Transactions = new Hashtable();
        }


        private static bool ExecutionEngine_GetExecutingScriptHash(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(engine.CurrentContext.ScriptHash);
            return true;
        }

        private static bool Output_GetAssetId(ExecutionEngine engine)
        {
            var output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        private static bool Runtime_CheckWitness(ExecutionEngine engine)
        {
            engine.EvaluationStack.Pop();
            engine.EvaluationStack.Push(true);
            return true;
        }

        private bool Storage_GetContext(ExecutionEngine engine)
        {
            StorageContext.ScriptHash = new UInt160(engine.CurrentContext.ScriptHash);
            engine.EvaluationStack.Push(StackItem.FromInterface(StorageContext));
            return true;
        }

        private static bool Storage_Get(ExecutionEngine engine)
        {
            var context = engine.EvaluationStack.Pop().GetInterface<CustomStorageContext>();
            var key = engine.EvaluationStack.Pop().GetByteArray().ToHexString();
            var item = new StorageItem {Value = (byte[]) context.Data[key]};
            engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
            return true;
        }


        private static bool Storage_Put(ExecutionEngine engine)
        {
            var context = engine.EvaluationStack.Pop().GetInterface<CustomStorageContext>();
            var top = engine.EvaluationStack.Pop();
            var key = top.GetByteArray().ToHexString();
            if (key.Length > 1024) return false;
            var value = engine.EvaluationStack.Pop().GetByteArray();

            context.Data[key] = value;
            return true;
        }

        private static bool Transaction_GetInputs(ExecutionEngine engine)
        {
            var tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(StackItem.FromInterface).ToArray());
            return true;
        }

        private bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            var hash = engine.EvaluationStack.Pop().GetByteArray();
            var tx = (Transaction) Transactions[hash];
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        private static bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(Date);
            return true;
        }


        private static bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            var header = engine.EvaluationStack.Pop().GetBigInteger();
            engine.EvaluationStack.Push(header);
            return true;
        }

        private static bool Header_GetTimestamp(ExecutionEngine engine)
        {
            var header = engine.EvaluationStack.Pop().GetBigInteger();
            engine.EvaluationStack.Push(header);
            return true;
        }

        private static bool Input_GetHash(ExecutionEngine engine)
        {
            var input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        private static bool Output_GetValue(ExecutionEngine engine)
        {
            var output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        private static bool Output_GetScriptHash(ExecutionEngine engine)
        {
            var output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }

        private static bool Input_GetIndex(ExecutionEngine engine)
        {
            var input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int) input.PrevIndex);
            return true;
        }

        private static bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            var tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Outputs.Select(StackItem.FromInterface).ToArray());
            return true;
        }

        private bool Transaction_GetReferences(ExecutionEngine engine)
        {
            var currentTx = engine.EvaluationStack.Pop().GetInterface<Transaction>();

            if (currentTx == null) return false;

            var dictionary = new Dictionary<CoinReference, TransactionOutput>();
            foreach (var group in currentTx.Inputs.GroupBy(p => p.PrevHash))
            {
                var prevTx = (Transaction) Transactions[group.Key];

                if (prevTx == null) return false;

                var inReferences = group.Select(p => new
                {
                    Input = p,
                    Output = prevTx.Outputs[p.PrevIndex]
                });

                foreach (var reference in inReferences)
                {
                    dictionary.Add(reference.Input, reference.Output);
                }
            }

            engine.EvaluationStack.Push(dictionary.Select(v => StackItem.FromInterface(v.Value)).ToArray());

            return true;
        }

        private static bool Runtime_Notify(ExecutionEngine engine)
        {
            engine.EvaluationStack.Pop();
            return true;
        }
    }
}