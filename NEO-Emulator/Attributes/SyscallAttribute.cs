using Neo.VM;
using System;

namespace Neo.Emulation
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class SyscallAttribute : Attribute
    {
        public string Method { get; }
        public decimal gasCost { get; }

        public SyscallAttribute(string method, double gasCost = (double)InteropService.defaultGasCost)
        {
            this.Method = method;
            this.gasCost = (decimal)gasCost;
        }
    }
}
