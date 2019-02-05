using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PostOpt
{
    class AssemblyProcessor
    {
        private string _mainAssemblyFilename;
        AssemblyDefinition _mainAssemblyDef;
        private ModuleDefinition _mainModuleDef;

        public AssemblyProcessor(string filename)
        {
            if (!Path.IsPathRooted(filename))
                  filename = Path.GetFullPath(filename);
            this._mainAssemblyFilename = filename;
        }

        public void Open()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(_mainAssemblyFilename));

            //Creates an AssemblyDefinition from the "MainAssembly.dll" assembly
            _mainAssemblyDef = AssemblyDefinition.ReadAssembly(_mainAssemblyFilename);
        }

        public void Save(string newfilename)
        {
            _mainModuleDef.Write(newfilename);
        }

        public void ProcessMainModule()
        {
            _mainModuleDef = _mainAssemblyDef.MainModule;

            //Gets all types which are declared in the Main Module of "MainAssembly.dll"
            //foreach (TypeDefinition type in _mainModuleDef.Types)
            //{
            //    //Writes the full name of a type
            //    Console.WriteLine(type.FullName);
            //}
            
            var methods = from type in _mainModuleDef.Types
                          from method in type.Methods
                          select method;

            foreach (var methodDefinition in methods)
            {
                try
                {
                    Console.WriteLine(@"Processing method body: "+ methodDefinition.FullName);
                    while (ProcessMethod(methodDefinition)) ;
                    Console.WriteLine(@"");
                }
                catch (Exception ex)
                {                    
                    Console.WriteLine(ex);
                }
            }
        }
        
        private bool ProcessMethod(MethodDefinition currentMethod)
        {
            foreach (var instruction in currentMethod.Body.Instructions)
            {
                if (Match_Call(instruction))
                {
                    var callInstruction = instruction;
                    var callMethodRef = callInstruction.Operand as MethodReference;

                    if (callMethodRef.HasThis) // non-static?
                        continue;                     
                    if (!callMethodRef.HasParameters)
                        continue;

                    if (callMethodRef.DeclaringType.FullName == "Xna.Framework.Vector2")
                    {
                        if (callMethodRef.Name == "op_Addition")
                        {
                            var result = ProcessOpCall(currentMethod, callInstruction, callMethodRef, "Add");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Subtraction")
                        {
                            var result = ProcessOpCall(currentMethod, callInstruction, callMethodRef, "Subtract");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Multiply")
                        {
                            var result = ProcessOpCall(currentMethod, callInstruction, callMethodRef, "Multiply");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Division")
                        {
                            var result = ProcessOpCall(currentMethod, callInstruction, callMethodRef, "Divide");
                            if (result)
                                return true;
                        }

                        if (callMethodRef.Name == "Add")
                        {
                            var result = ProcessOpCall(currentMethod, callInstruction, callMethodRef, "Add");
                            if (result)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool ProcessOpCall(MethodDefinition currentMethod, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {
            if (callMethodRef.ReturnType.FullName == "System.Void")
            {
                var lastParam = callMethodRef.Parameters[callMethodRef.Parameters.Count - 1];
                if (lastParam.ParameterType.IsByReference)
                {
                    // replace method `void op(..., valuetype& result)`
                    var result = ProcessOpCall_retout(currentMethod, callInstruction, callMethodRef, methodOpName);
                    if (result == true)
                        return true;
                }
            }
            else if (callMethodRef.ReturnType.IsValueType)
            {
                // replace method `valuetype op(...)`
                var result = ProcessOpCall_retval(currentMethod, callInstruction, callMethodRef, methodOpName);
                if (result == true)
                    return true;

                result = ProcessOpCall_retval2(currentMethod, callInstruction, callMethodRef, methodOpName);
                if (result == true)
                    return true;

            }
            
            return false;
        }

        private bool ProcessOpCall_retval2(MethodDefinition currentMethod, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {   
            var ILprocessorEx = new ILProcessorEx(currentMethod);

            var nextInstruction = callInstruction.Next;

            if(Match_Stloc(nextInstruction))
            {
                Instruction StlocInstruction = nextInstruction;
                int n;
                Instruction newLdlocaInstruction = Stloc2Ldloca(ILprocessorEx, StlocInstruction, out n);
                                
                Instruction op_outInstruction = GetMethodRefOp2(ILprocessorEx, callMethodRef, methodOpName);
                if (op_outInstruction == null)
                    return false;
                
                Console.WriteLine(@"Patching " + callMethodRef.FullName +@" (0x"+callInstruction.Offset.ToString("X")+")");
                Console.WriteLine(@" ...into " + ((MethodReference)op_outInstruction.Operand).FullName);

                ILprocessorEx.Processor.Remove(StlocInstruction);
                ILprocessorEx.Processor.InsertBefore(callInstruction, newLdlocaInstruction);                
                // replace 'valuetype Op(...)' with 'void Op(..., out valuetype)'
                ILprocessorEx.Replace(callInstruction, op_outInstruction);
                
                return true;
            }

            return false;
        }

        private bool ProcessOpCall_retval(MethodDefinition currentMethod, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {
            var ILprocessorEx = new ILProcessorEx(currentMethod);

            int currentParamIdx = callMethodRef.Parameters.Count - 1;
            // for now we support only operators (with two arguments). currentParamIdx has to be #1.
            if (currentParamIdx != 1)
                throw new InvalidOperationException();
            var currentParam = callMethodRef.Parameters[currentParamIdx];
            var isCurrentParamByRef = currentParam.ParameterType.IsByReference;
         
            for(var instruction = callInstruction.Previous; instruction != null; instruction = instruction.Previous)
            {
                //TODO: check if there is a branch target between instruction and callInstruction and quit
                //    break;
                
                if (isCurrentParamByRef == false)
                {
                    if (Match_Ldloc(instruction))
                    {
                        Instruction LdlocInstruction = instruction;
                        int n;
                        Instruction newLdlocaInstruction = Ldloc2Ldloca(ILprocessorEx, LdlocInstruction, out n);

                        Instruction op_refInstruction = GetMethodRefOp(ILprocessorEx, callMethodRef, methodOpName, currentParamIdx);
                        if (op_refInstruction == null)
                            return false;
                    
                        Console.WriteLine(@"Patching " + callMethodRef.FullName +@" (0x"+callInstruction.Offset.ToString("X")+")");
                        Console.WriteLine(@" ...into " + ((MethodReference)op_refInstruction.Operand).FullName);

                        // replace 'Ldloc' with 'Ldloca'
                        ILprocessorEx.Processor.Replace(LdlocInstruction, newLdlocaInstruction);
                        // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2,vector2)'
                        ILprocessorEx.Replace(callInstruction, op_refInstruction);
                                        
                        return true;
                    }
                    else if (Match_Ldarg(instruction))
                    {
                        Instruction LdargInstruction = instruction;
                        int n;
                        Instruction newLdargaInstruction = Ldarg2Ldarga(ILprocessorEx, LdargInstruction, out n);

                        // check validity of parameter n
                        var targParam = currentMethod.Parameters[n];
                        if (targParam.ParameterType.IsByReference == true)
                            throw new InvalidOperationException();

                        Instruction op_refInstruction = GetMethodRefOp(ILprocessorEx, callMethodRef, methodOpName, currentParamIdx);
                        if (op_refInstruction == null)
                            return false;
                    
                        Console.WriteLine(@"Patching " + callMethodRef.FullName +@" (0x"+callInstruction.Offset.ToString("X")+")");
                        Console.WriteLine(@" ...into " + ((MethodReference)op_refInstruction.Operand).FullName);

                        // replace 'Ldarg' with 'Ldarga'
                        ILprocessorEx.Processor.Replace(LdargInstruction, newLdargaInstruction);
                        // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2, ref vector2)'
                        ILprocessorEx.Replace(callInstruction, op_refInstruction);
                    
                        return true;
                    }
                    else if (Match_Ldobj(instruction))
                    {
                        Instruction LdobjInstruction = instruction;

                        var ldtype = LdobjInstruction.Operand;
                        var type = callMethodRef.Parameters[1];
                        
                        if (Match_Ldarg(LdobjInstruction.Previous))
                        {
                            Instruction LdargInstruction = LdobjInstruction.Previous;

                            int n;
                            Instruction newLdargaInstruction = Ldarg2Ldarga(ILprocessorEx, LdargInstruction, out n);

                            // check validity of parameter n
                            var targParam = currentMethod.Parameters[n];
                            if (targParam.ParameterType.IsByReference == false)
                                throw new InvalidOperationException();
                            Instruction op_refInstruction = GetMethodRefOp(ILprocessorEx, callMethodRef, methodOpName, currentParamIdx);
                            if (op_refInstruction == null)
                                return false;
                        
                            var callOffset = callInstruction.Offset;
                            Console.WriteLine(@"Patching " + callMethodRef.FullName +@" (0x"+callInstruction.Offset.ToString("X")+")");
                            Console.WriteLine(@" ...into " + ((MethodReference)op_refInstruction.Operand).FullName);
                            
                            ILprocessorEx.Processor.Remove(LdobjInstruction);
                            // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2, ref vector2)'
                            ILprocessorEx.Processor.Replace(callInstruction, op_refInstruction);
                        
                            return true;
                        }
                    }
                    return false;                    
                }
                else // (isCurrentParamByRef == true)
                {
                    if (Match_Ldloca(instruction))
                    {
                        Instruction LdlocaInstruction = instruction;
                        
                        // argument sucesfully matched. Move to next argument.
                        if (currentParamIdx == 0)
                        {
                            // no more arguments to match.
                            return false;
                        }
                        else
                        {
                            //TODO: after transforming the first argument the assembly doesn't work.
                            return false;

                            // Move to next argument.
                            currentParamIdx--; 
                            currentParam = callMethodRef.Parameters[currentParamIdx];
                            isCurrentParamByRef = currentParam.ParameterType.IsByReference;
                            continue;
                        }


                    }
                    return false;
                }


                if (Match_Nop(instruction))
                    continue;
                if (Match_Break(instruction))
                    continue;
                // Unkwown instruction. quit
                break;
            }
            
            return false;
        }
        
        private bool ProcessOpCall_retout(MethodDefinition currentMethod, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {
            var ILprocessorEx = new ILProcessorEx(currentMethod);

            return false;
        }

        private static Instruction GetMethodRefOp(ILProcessorEx ILprocessorEx, MethodReference callMethodRef, string methodOpName, int currentParamIdx)
        {
            var ILprocessor = ILprocessorEx.Processor;

            MethodReference MethodDefOp = null;
            var typeDef = callMethodRef.DeclaringType.Resolve();
            foreach (var method in typeDef.Methods)
            {
                if (method.HasThis)
                    continue;
                if (method.Name != methodOpName)
                    continue;                
                if (method.ReturnType.FullName != callMethodRef.ReturnType.FullName) // 
                    continue;
                if (method.Parameters.Count != callMethodRef.Parameters.Count)
                    continue;

                var match = true;
                for (int i = 0; i<method.Parameters.Count; i++)
                {
                    if (i ==currentParamIdx)
                    {
                        if (method.Parameters[i].ParameterType.IsByReference != true ||
                            callMethodRef.Parameters[i].ParameterType.IsByReference != false)
                        {
                            match = false;
                            continue;
                        }
                    }
                    else
                    {
                        if (method.Parameters[i].ParameterType.IsByReference != callMethodRef.Parameters[i].ParameterType.IsByReference)
                        {
                            match = false;
                            continue;
                        }
                    }
                }

                if (match != true)
                    continue;

                //if (method.Parameters[0].ParameterType.IsByReference == false &&
                //    method.Parameters[1].ParameterType.IsByReference == true)
                MethodDefOp = method;
                break;
            }

            if (MethodDefOp == null)
            {
                //method not found
                return null;
            }

            var methodRefOp = callMethodRef.DeclaringType.Module.ImportReference(MethodDefOp);
            var op_refInstruction = ILprocessor.Create(OpCodes.Call, methodRefOp);
            return op_refInstruction;
        }
                
        private static Instruction GetMethodRefOp2(ILProcessorEx ILprocessorEx, MethodReference callMethodRef, string methodOpName)
        {
            var ILprocessor = ILprocessorEx.Processor;

            MethodReference MethodDefOp = null;
            var typeDef = callMethodRef.DeclaringType.Resolve();
            foreach (var method in typeDef.Methods)
            {
                if (method.HasThis)
                    continue;
                if (method.Name != methodOpName)
                    continue;                
                if (method.ReturnType.FullName != "System.Void") // 
                    continue;
                //if (callMethodRef.ReturnType.FullName) // 
                //    continue;
                if ((method.Parameters.Count-1) != callMethodRef.Parameters.Count)
                    continue;

                var match = true;
                for (int i = 0; i < callMethodRef.Parameters.Count; i++)
                {
                    if (method.Parameters[i].ParameterType.IsByReference != callMethodRef.Parameters[i].ParameterType.IsByReference)
                    {
                        match = false;
                        continue;
                    }
                }

                if (match != true)
                    continue;

                //if (method.Parameters[0].ParameterType.IsByReference == false &&
                //    method.Parameters[1].ParameterType.IsByReference == true)
                MethodDefOp = method;
                break;
            }

            if (MethodDefOp == null)
            {
                //method not found
                return null;
            }

            var methodRefOp = callMethodRef.DeclaringType.Module.ImportReference(MethodDefOp);
            var op_outInstruction = ILprocessor.Create(OpCodes.Call, methodRefOp);
            return op_outInstruction;
        }
        
        private static Instruction Ldloc2Ldloca(ILProcessorEx ILprocessorEx, Instruction LdlocInstruction, out int n)
        {
            var ILprocessor = ILprocessorEx.Processor;

            if (LdlocInstruction.OpCode.Code == Code.Ldloc)
            {
                var varDef = LdlocInstruction.Operand as VariableDefinition;
                n = varDef.Index;
                return ILprocessor.Create(OpCodes.Ldloca, ILprocessor.Body.Variables[n]);
            }
            else if (LdlocInstruction.OpCode.Code == Code.Ldloc_S)
            {
                var varDef = LdlocInstruction.Operand as VariableDefinition;
                n = varDef.Index;
            }
            else if (LdlocInstruction.OpCode.Code == Code.Ldloc_0)
                n = 0;
            else if (LdlocInstruction.OpCode.Code == Code.Ldloc_1)
                n = 1;
            else if (LdlocInstruction.OpCode.Code == Code.Ldloc_2)
                n = 2;
            else if (LdlocInstruction.OpCode.Code == Code.Ldloc_3)
                n = 3;
            else
                throw new InvalidOperationException();

                return ILprocessor.Create(OpCodes.Ldloca_S, ILprocessor.Body.Variables[n]);
                
        }
        
        private static Instruction Ldarg2Ldarga(ILProcessorEx ILprocessorEx, Instruction LdargInstruction, out int n)
        {
            var ILprocessor = ILprocessorEx.Processor;

            if (LdargInstruction.OpCode.Code == Code.Ldarg)
            {
                var varDef = LdargInstruction.Operand as VariableDefinition;
                n = varDef.Index;
                return ILprocessor.Create(OpCodes.Ldarga, ILprocessor.Body.Method.Parameters[n]);
            }
            else if (LdargInstruction.OpCode.Code == Code.Ldarg_S)
            {
                var varDef = LdargInstruction.Operand as VariableDefinition;
                n = varDef.Index;
            }
            else if (LdargInstruction.OpCode.Code == Code.Ldarg_0)
                n = 0;
            else if (LdargInstruction.OpCode.Code == Code.Ldarg_1)
                n = 1;
            else if (LdargInstruction.OpCode.Code == Code.Ldarg_2)
                n = 2;
            else if (LdargInstruction.OpCode.Code == Code.Ldarg_3)
                n = 3;
            else
                throw new InvalidOperationException();

            return ILprocessor.Create(OpCodes.Ldarga_S, ILprocessor.Body.Method.Parameters[n]);
        }
                
        private static Instruction Stloc2Ldloca(ILProcessorEx ILprocessorEx, Instruction StlocInstruction, out int n)
        {
            var ILprocessor = ILprocessorEx.Processor;

            if (StlocInstruction.OpCode.Code == Code.Stloc)
            {
                var varDef = StlocInstruction.Operand as VariableDefinition;
                n = varDef.Index;
                return ILprocessor.Create(OpCodes.Ldloca, ILprocessor.Body.Variables[n]);
            }
            else if (StlocInstruction.OpCode.Code == Code.Stloc_S)
            {
                var varDef = StlocInstruction.Operand as VariableDefinition;
                n = varDef.Index;
            }
            else if (StlocInstruction.OpCode.Code == Code.Stloc_0)
                n = 0;
            else if (StlocInstruction.OpCode.Code == Code.Stloc_1)
                n = 1;
            else if (StlocInstruction.OpCode.Code == Code.Stloc_2)
                n = 2;
            else if (StlocInstruction.OpCode.Code == Code.Stloc_3)
                n = 3;
            else
                throw new InvalidOperationException();

                return ILprocessor.Create(OpCodes.Ldloca_S, ILprocessor.Body.Variables[n]);
                
        }

        #region Match instruction
        private static bool Match_Nop(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Nop;
        }

        private static bool Match_Break(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Break;
        }

        private static bool Match_Call(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Call;
        }

        private static bool Match_Ldloc(Instruction prevInstruction)
        {
            return  prevInstruction.OpCode.Code == Code.Ldloc_0 ||
                    prevInstruction.OpCode.Code == Code.Ldloc_1 ||
                    prevInstruction.OpCode.Code == Code.Ldloc_2 ||
                    prevInstruction.OpCode.Code == Code.Ldloc_3 ||
                    prevInstruction.OpCode.Code == Code.Ldloc_S ||
                    prevInstruction.OpCode.Code == Code.Ldloc;
        }

        private static bool Match_Ldarg(Instruction prevInstruction)
        {
            return  prevInstruction.OpCode.Code == Code.Ldarg_0 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_1 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_2 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_3 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_S ||
                    prevInstruction.OpCode.Code == Code.Ldarg;
        }
                
        private static bool Match_Ldloca(Instruction prevInstruction)
        {
            return  prevInstruction.OpCode.Code == Code.Ldloca_S ||
                    prevInstruction.OpCode.Code == Code.Ldloca;
        }

        private static bool Match_Ldobj(Instruction prevInstruction)
        {
            return prevInstruction.OpCode.Code == Code.Ldobj;
        }
                
        private static bool Match_Stloc(Instruction prevInstruction)
        {
            return  prevInstruction.OpCode.Code == Code.Stloc_0 ||
                    prevInstruction.OpCode.Code == Code.Stloc_1 ||
                    prevInstruction.OpCode.Code == Code.Stloc_2 ||
                    prevInstruction.OpCode.Code == Code.Stloc_3 ||
                    prevInstruction.OpCode.Code == Code.Stloc_S ||
                    prevInstruction.OpCode.Code == Code.Stloc;
        }


        #endregion Match instruction

    }
}
