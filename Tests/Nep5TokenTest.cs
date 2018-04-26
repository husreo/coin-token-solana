using System.IO;
using System.Linq;
using Neo.Cryptography;
using Neo.VM;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Nep5TokenTest
    {
        public const string TokenFilename = "../../../../NEP5Token/bin/Release/netcoreapp2.0/publish/NEP5Token.avm";

        [Test]
        public void SimpleTest()
        {
            ExecutionEngine engine = new ExecutionEngine(null, Crypto.Default);
            engine.LoadScript(File.ReadAllBytes(TokenFilename));

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                object[] parameters = {"deploy"};
                parameters.Reverse().ToList().ForEach(p => sb.EmitPush(p));
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute();
            Assert.True(engine.EvaluationStack.Peek().GetBoolean());
        }
    }
}