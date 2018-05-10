using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;

namespace NEP5.Contract.Tests
{
    internal static class TestHelper
    {
        private const string Nep5ContractFilePath =
            "../../../../NEP5.Contract/bin/Release/netcoreapp2.0/publish/NEP5.Contract.avm";

        public static ExecutionEngine LoadContractScript()
        {
            var engine = new ExecutionEngine(null, Crypto.Default);
            var contractScriptBytes = File.ReadAllBytes(Nep5ContractFilePath);
            engine.LoadScript(contractScriptBytes);
            return engine;
        }

        public static byte[] GetScriptArguments(string operation, params object[] args)
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

        public static StackItem Execute(ExecutionEngine engine, byte[] scriptArguments)
        {
            engine.LoadScript(scriptArguments);
            engine.Execute();
            return engine.EvaluationStack.Peek();
        }

        public static StackItem Execute(ExecutionEngine engine, string operation, params object[] args)
        {
            return Execute(engine, GetScriptArguments(operation, args));
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