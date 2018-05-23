using System;

namespace Neo.Emulation
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class NonemitAttribute : Attribute
    {
    }
}
