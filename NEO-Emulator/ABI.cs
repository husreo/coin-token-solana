using LunarParser.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Emulation
{
    public class AVMInput
    {
        public string name;
        public Emulator.Type type;
    }

    public class AVMFunction
    {
        public string name;
        public Emulator.Type returnType;
        public List<AVMInput> inputs = new List<AVMInput>();
    }

    public class ABI
    {
        public Dictionary<string, AVMFunction> functions = new Dictionary<string, AVMFunction>(StringComparer.InvariantCultureIgnoreCase);
        public AVMFunction entryPoint { get; private set; }
        public readonly string fileName;

        public ABI()
        {
            var f = new AVMFunction();
            f.name = "Main";
            f.inputs.Add(new AVMInput() { name = "operation", type = Emulator.Type.String});
            f.inputs.Add(new AVMInput() { name = "args", type = Emulator.Type.Array });

            this.functions[f.name] = f;
            this.entryPoint = functions.Values.FirstOrDefault();
        }

        public ABI(string fileName)
        {
            this.fileName = fileName;

            var json = File.ReadAllText(fileName);
            var root = JSONReader.ReadFromString(json);

            var fn = root.GetNode("functions");
            foreach (var child in fn.Children) {
                var f = new AVMFunction();
                f.name = child.GetString("name");
                if (!Enum.TryParse(child.GetString("returnType"), true, out f.returnType))
                {
                    f.returnType = Emulator.Type.Unknown;
                }

                var p = child.GetNode("parameters");
                if (p != null && p.ChildCount > 0)
                {
                    for (int i=0; i<p.ChildCount; i++)
                    {
                        var input = new AVMInput();
                        input.name = p[i].GetString("name");
                        var temp = p[i].GetString("type");
                        if (!Enum.TryParse<Emulator.Type>(temp, true, out input.type))
                        {
                            input.type = Emulator.Type.Unknown;
                        }                         
                        f.inputs.Add(input);
                    }
                }

                functions[f.name] = f;
            }

            entryPoint = functions[root.GetString("entrypoint")];
        }
    }
}
