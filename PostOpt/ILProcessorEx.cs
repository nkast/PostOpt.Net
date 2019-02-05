using System;
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

        internal void Replace(Instruction target, Instruction newInstruction)
        {
            var targetOpOffset = target.Offset;
            Processor.Replace(target, newInstruction);

            // update offset
            newInstruction.Offset = targetOpOffset;
            target.Offset = 0;

        }
    }
}