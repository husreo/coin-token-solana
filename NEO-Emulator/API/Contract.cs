using Neo.VM;
using System;

namespace Neo.Emulation.API
{
    public class Contract : VM.IInteropInterface
    {
        public byte[] script;

        [Syscall("Neo.Contract.GetScript")]
        public static bool GetScript(ExecutionEngine engine)
        {
            // Contract
            // returns byte[] 

            var obj = engine.EvaluationStack.Pop();
            var contract = ((VM.Types.InteropInterface)obj).GetInterface<Contract>();

            engine.EvaluationStack.Push(contract.script);

            return true;
        }

        [Syscall("Neo.Contract.GetStorageContext")]
        public static bool GetStorageContext(ExecutionEngine engine)
        {
            // Contract
            // returns StorageContext 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Contract.Create", 500)]
        public static bool Create(ExecutionEngine engine)
        {
            var script = engine.EvaluationStack.Pop().GetByteArray();
            var parameterList = engine.EvaluationStack.Pop().GetByteArray();
            var return_type = engine.EvaluationStack.Pop();
            var need_storage = engine.EvaluationStack.Pop().GetBoolean();
            var name = engine.EvaluationStack.Pop().GetString();
            var version = engine.EvaluationStack.Pop().GetString();
            var author = engine.EvaluationStack.Pop().GetString();
            var email = engine.EvaluationStack.Pop().GetString();
            var desc = engine.EvaluationStack.Pop().GetString();

            //byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description

            var contract = new Contract();
            contract.script = script;

            var blockchain = engine.GetBlockchain();
            var account = blockchain.DeployContract(name, script);
            // TODO : merge Contract and Account

            engine.EvaluationStack.Push(new VM.Types.InteropInterface(contract));

            return true;
        }

        [Syscall("Neo.Contract.Migrate", 500)]
        public static bool Migrate(ExecutionEngine engine)
        {
            //byte[] script, byte[] parameter_list, byte return_type, bool need_storage, string name, string version, string author, string email, string description
            // returns Contract 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Contract.Destroy")]
        public static bool Destroy(ExecutionEngine engine)
        {
            // returns nothing
            throw new NotImplementedException();
        }
    }
}
