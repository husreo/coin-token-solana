using System.IO;
using System.Linq;
using Neo.VM;
using LunarParser;
using Neo.Emulation;
using NUnit.Framework;

namespace NEP5.Contract.Tests
{
    internal static class TestHelper
    {
        private const string Nep5ContractFilePath =
            "../../../../NEP5.Contract/bin/Release/netcoreapp2.0/publish/NEP5.Contract.avm";
        public static readonly byte[] Avm = File.ReadAllBytes(Nep5ContractFilePath);

        public static StackItem Execute(this Emulator emulator, string operation, params object[] args)
        {
            var inputs = DataNode.CreateArray();
            inputs.AddValue(operation);

            if (args.Length > 0)
            {
                var parameters = DataNode.CreateArray();
                args.ToList().ForEach(a => parameters.AddValue(a));
                inputs.AddNode(parameters); 
            }
            else
            {
                inputs.AddValue(null);
            }

            emulator.Reset(inputs, new ABI());
            emulator.Run();

            var result = emulator.GetOutput();
            
            Assert.NotNull(result);
            return result;
        }
    }
}