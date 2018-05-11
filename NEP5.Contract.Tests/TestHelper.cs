using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo;
using System.Collections;
using Neo.Core;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    internal static class TestHelper
    {
        private const string Nep5ContractFilePath =
            "../../../../NEP5.Contract/bin/Release/netcoreapp2.0/publish/NEP5.Contract.avm";

        private const string NeoAssetId = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";

        private static IScriptContainer _scriptContainer;
        private static CustomInteropService _service;

        public static void Init()
        {
            _service = new CustomInteropService {StorageContext = {Data = new Hashtable()}};
        }

        public static void InitTransactionContext(string scriptHash, int value, ushort inputAmount = 1)
        {
            Transaction initialTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            Transaction currentTransaction = new CustomTransaction(TransactionType.ContractTransaction);
            initialTransaction.Outputs = new TransactionOutput[inputAmount];
            currentTransaction.Inputs = new CoinReference[inputAmount];

            for (ushort i = 0; i < inputAmount; ++i)
            {
                /* CREATE FAKE PREVIOUS TRANSACTION */
                var transactionOutput = new TransactionOutput
                {
                    ScriptHash = UInt160.Parse(scriptHash),
                    Value = new Fixed8(value),
                    AssetId = UInt256.Parse(NeoAssetId)
                };

                initialTransaction.Outputs[i] = transactionOutput;
                /* CREATE FAKE CURRENT TRANSACTION */
                var coinRef = new CoinReference
                {
                    PrevHash = initialTransaction.Hash,
                    PrevIndex = i
                };

                currentTransaction.Outputs = new TransactionOutput[1];
                currentTransaction.Outputs[0] = new TransactionOutput
                {
                    ScriptHash = UInt160.Parse(scriptHash),
                    Value = new Fixed8(value),
                    AssetId = UInt256.Parse(NeoAssetId)
                };

                currentTransaction.Inputs[i] = coinRef;
            }

            /* INIT CONTEXT */
            _service.Transactions[initialTransaction.Hash] = initialTransaction;
            _scriptContainer = currentTransaction;
        }

        private static ExecutionEngine LoadContractScript()
        {
            var engine = new ExecutionEngine(_scriptContainer, Crypto.Default, null, _service);
            var contractScriptBytes = File.ReadAllBytes(Nep5ContractFilePath);
            engine.LoadScript(contractScriptBytes);
            return engine;
        }

        private static byte[] GetScriptArguments(string operation, params object[] args)
        {
            Debug.Assert(operation != null && args != null);
            using (var sb = new ScriptBuilder())
            {
                foreach (var arg in args.Reverse())
                {
                    EmitPush(sb, arg);
                }

                sb.EmitPush(args.Length);
                sb.EmitPush(OpCode.PACK);
                sb.EmitPush(operation);
                return sb.ToArray();
            }
        }

        public static StackItem Execute(string operation, params object[] args)
        {
            var engine = LoadContractScript();
            var scriptArguments = GetScriptArguments(operation, args);
            engine.LoadScript(scriptArguments);
            engine.Execute();
//            Assert.AreEqual(VMState.HALT, engine.State); // todo: uncomment
            return engine.EvaluationStack.Peek();
        }

        private static void EmitPush(ScriptBuilder builder, object arg)
        {
            switch (arg)
            {
                case bool _:
                    builder.EmitPush((bool) arg);
                    break;
                case byte[] _:
                    builder.EmitPush((byte[]) arg);
                    break;
                case string _:
                    builder.EmitPush((string) arg);
                    break;
                case BigInteger _:
                    builder.EmitPush((BigInteger) arg);
                    break;
                case ContractParameter _:
                    builder.EmitPush((ContractParameter) arg);
                    break;
                case ISerializable _:
                    builder.EmitPush((ISerializable) arg);
                    break;
                default:
                    builder.EmitPush(arg);
                    break;
            }
        }
    }
}