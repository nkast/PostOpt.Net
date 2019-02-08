using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PostOpt
{
    internal class ILProcessorEx
    {
        private readonly ILProcessor _processor;

        public readonly MethodDefinition Method;        

        public ILProcessorEx(MethodDefinition method)
        {
            Method = method;
            _processor = method.Body.GetILProcessor();
        }

        public Instruction Create(OpCode opcode, MethodReference method)
        {
            return _processor.Create(opcode, method);
        }

        public Instruction Create(OpCode opcode, ParameterDefinition parameter)
        {            
            return _processor.Create(opcode, parameter);
        }
        
        public Instruction Create(OpCode opcode, VariableDefinition variable)
        {
            return _processor.Create(opcode, variable);
        }

        public void InsertBefore(Instruction instruction, Instruction newInstruction)
        {
            // keep some info
            var instructionOffset = instruction.Offset;

            _processor.InsertBefore(instruction, newInstruction);
            
            // update offset
            newInstruction.Offset = instructionOffset;
            // update offsets
            int newInstructionSize = newInstruction.GetSize();
            int offsetDiff = newInstructionSize;
            for (var followingInstruction = instruction; followingInstruction != null; followingInstruction = followingInstruction.Next)
            {
                followingInstruction.Offset += offsetDiff;
            }
                        
            UpdateBranchesTarget(instruction, newInstruction);
        }
        
        public void Remove(Instruction instruction)
        {
            // keep some info
            var instructionOffset = instruction.Offset;
            var nextInstruction = instruction.Next;
            // there sould be at least a ret after the removed instruction in case it was a branch target.
            // otherwise UpdateBranchesTarget(instruction, newInstruction) will fail.
            if (nextInstruction == null) 
                throw new InvalidOperationException();
            
            _processor.Remove(instruction);
            instruction.Offset = 0; // clear Offset from the detached instruction
            
            // update offsets
            int oldInstructionSize = instruction.GetSize();
            int offsetDiff = - oldInstructionSize;
            for (var followingInstruction = nextInstruction; followingInstruction != null; followingInstruction = followingInstruction.Next)
            {
                followingInstruction.Offset += offsetDiff;
            }
            
            UpdateBranchesTarget(instruction, nextInstruction);
        }

        public void Replace(Instruction instruction, Instruction newInstruction)
        {
            // keep some info
            var instructionOffset = instruction.Offset;

            _processor.Replace(instruction, newInstruction);
            instruction.Offset = 0; // clear Offset from the detached instruction

            // update offset
            newInstruction.Offset = instructionOffset;
            // update offsets
            int oldInstructionSize = instruction.GetSize();
            int newInstructionSize = newInstruction.GetSize();
            if (oldInstructionSize != newInstructionSize)
            {
                int offsetDiff = newInstructionSize - oldInstructionSize;
                for (var followingInstruction = newInstruction.Next; followingInstruction != null; followingInstruction = followingInstruction.Next)
                {
                    followingInstruction.Offset += offsetDiff;
                }
            }

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