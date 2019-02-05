using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PostOpt
{
    internal class ILProcessorEx
    {
        public readonly ILProcessor Processor;
        public readonly MethodDefinition Method;
        

        public ILProcessorEx(MethodDefinition method)
        {
            Method = method;
            Processor = method.Body.GetILProcessor();
        }
    }
}