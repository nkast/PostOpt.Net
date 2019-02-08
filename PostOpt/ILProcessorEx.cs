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

        internal Instruction Create(OpCode opcode, MethodReference method)
        {
            return Processor.Create(opcode, method);
        }

        public Instruction Create(OpCode opcode, ParameterDefinition parameter)
        {            
            return Processor.Create(opcode, parameter);
        }
        
        public Instruction Create(OpCode opcode, VariableDefinition variable)
        {
            return Processor.Create(opcode, variable);
        }

        internal void Replace(Instruction instruction, Instruction newInstruction)
        {
            var instructionOffset = instruction.Offset;
            Processor.Replace(instruction, newInstruction);

            // update offset
            newInstruction.Offset = instructionOffset;
            instruction.Offset = 0;

            UpdateBranchesTarget(instruction, newInstruction);
        }

        private void UpdateBranchesTarget(Instruction oldTarget, Instruction newTarget)
        {
            for (var instruction = this.Method.Body.Instructions[0]; instruction != null; instruction = instruction.Next)
            {
                if (instruction.OpCode == OpCodes.Beq
                 || instruction.OpCode == OpCodes.Beq_S
                 || instruction.OpCode == OpCodes.Bge
                 || instruction.OpCode == OpCodes.Bge_S
                 || instruction.OpCode == OpCodes.Bge_Un
                 || instruction.OpCode == OpCodes.Bge_Un_S
                 || instruction.OpCode == OpCodes.Bgt
                 || instruction.OpCode == OpCodes.Bgt_S
                 || instruction.OpCode == OpCodes.Bgt_Un
                 || instruction.OpCode == OpCodes.Bgt_Un_S
                 || instruction.OpCode == OpCodes.Ble
                 || instruction.OpCode == OpCodes.Ble_S
                 || instruction.OpCode == OpCodes.Ble_Un
                 || instruction.OpCode == OpCodes.Ble_Un_S
                 || instruction.OpCode == OpCodes.Blt
                 || instruction.OpCode == OpCodes.Blt_S
                 || instruction.OpCode == OpCodes.Blt_Un
                 || instruction.OpCode == OpCodes.Blt_Un_S
                 || instruction.OpCode == OpCodes.Bne_Un
                 || instruction.OpCode == OpCodes.Bne_Un_S                 
                 || instruction.OpCode == OpCodes.Br
                 || instruction.OpCode == OpCodes.Br_S
                 || instruction.OpCode == OpCodes.Brfalse
                 || instruction.OpCode == OpCodes.Brfalse_S
                 || instruction.OpCode == OpCodes.Brtrue
                 || instruction.OpCode == OpCodes.Brtrue_S)
                {
                    if (instruction.Operand == oldTarget)
                    {
                        instruction.Operand = newTarget;
                    }
                }

                if (instruction.OpCode == OpCodes.Switch)
                {
                    //TODO: update switch branch target 
                    throw new NotImplementedException("Patching Switch branch targets in not Implemented");
                }
            }
            return;
        }
    }
}