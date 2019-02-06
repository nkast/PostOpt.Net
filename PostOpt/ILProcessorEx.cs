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

            UpdateBranchesTarget(target, newInstruction);
        }

        private void UpdateBranchesTarget(Instruction oldTarget, Instruction newTarget)
        {
            for (var instruction = this.Processor.Body.Instructions[0]; instruction != null; instruction = instruction.Next)
            {
                if (instruction.OpCode == OpCodes.Blt ||
                    instruction.OpCode == OpCodes.Blt_S
                    )
                {
                    if (instruction.Operand == oldTarget)
                    {
                        instruction.Operand = newTarget;
                    }
                }
            }
            return;
        }
    }
}