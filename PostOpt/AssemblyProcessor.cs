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
            foreach (TypeDefinition type in _mainModuleDef.Types)
            {
                //Writes the full name of a type
                Console.WriteLine(type.FullName);
            }
            
            var methods = from type in _mainModuleDef.Types
                          from method in type.Methods
                          select method;

            foreach (var methodDefinition in methods)
            {
                try
                {
                    while (ProcessMethod(methodDefinition)) ;
                }
                catch (Exception ex)
                {                    
                    Console.WriteLine(ex);
                }
            }
        }
        
        private bool ProcessMethod(MethodDefinition currentMethod)
        {
            var processor = currentMethod.Body.GetILProcessor();

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
                            var result = ProcessOpCall(processor, callInstruction, callMethodRef, "Add");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Subtraction")
                        {
                            var result = ProcessOpCall(processor, callInstruction, callMethodRef, "Subtract");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Multiply")
                        {
                            var result = ProcessOpCall(processor, callInstruction, callMethodRef, "Multiply");
                            if (result)
                                return true;
                        }
                        if (callMethodRef.Name == "op_Division")
                        {
                            var result = ProcessOpCall(processor, callInstruction, callMethodRef, "Divide");
                            if (result)
                                return true;
                        }
                        
                    }
                }
            }

            return false;
        }

        private bool ProcessOpCall(ILProcessor processor, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {
            if (callMethodRef.ReturnType.FullName == "System.Void")
            {
                var lastParam = callMethodRef.Parameters[callMethodRef.Parameters.Count - 1];
                if (lastParam.ParameterType.IsByReference)
                {
                    // replace method `void op(..., valuetype& result)`
                    return ProcessOpCall_retout(processor, callInstruction, callMethodRef, methodOpName);
                }
            }
            else if (callMethodRef.ReturnType.IsValueType)
            {
                // replace method `valuetype op(...)`
                return ProcessOpCall_retval(processor, callInstruction, callMethodRef, methodOpName);
            }
            
            return false;
        }

        private bool ProcessOpCall_retval(ILProcessor processor, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {                
            MethodDefinition currentMethod = processor.Body.Method;
            
            var prevInstruction = callInstruction.Previous;
            

            if (Match_Ldloc(prevInstruction))
            {
                Instruction LdlocInstruction = prevInstruction;
                int n;
                Instruction newLdlocaInstruction = Ldloc2Ldloca(processor, LdlocInstruction, out n);

                Instruction add_vrvInstruction = GetMethodRefOp_vrv(processor, callMethodRef, methodOpName);
                if(add_vrvInstruction == null)
                    return false;

                // replace 'Ldloc' with 'Ldloca'
                processor.Replace(LdlocInstruction, newLdlocaInstruction);
                // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2,vector2)'
                processor.Replace(callInstruction, add_vrvInstruction);

                return true;
            }
            else if (Match_Ldarg(prevInstruction))
            {
                Instruction LdargInstruction = prevInstruction;
                int n;
                Instruction newLdargaInstruction = Ldarg2Ldarga(processor, LdargInstruction, out n);

                // check validity of parameter n
                var targParam = currentMethod.Parameters[n];
                if (targParam.ParameterType.IsByReference == true)
                    throw new InvalidOperationException();

                Instruction add_vrvInstruction = GetMethodRefOp_vrv(processor, callMethodRef, methodOpName);
                if (add_vrvInstruction == null)
                    return false;


                // replace 'Ldarg' with 'Ldarga'
                processor.Replace(LdargInstruction, newLdargaInstruction);
                // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2, ref vector2)'
                processor.Replace(callInstruction, add_vrvInstruction);

                return true;
            }
            else if (Match_Ldobj(prevInstruction))
            {
                Instruction LdobjInstruction = prevInstruction;

                var ldtype = LdobjInstruction.Operand;
                var type = callMethodRef.Parameters[1];

                var prev2Instruction = LdobjInstruction.Previous;
                if (Match_Ldarg(prev2Instruction))
                {
                    Instruction LdargInstruction = prev2Instruction;

                    int n;
                    Instruction newLdargaInstruction = Ldarg2Ldarga(processor, LdargInstruction, out n);

                    // check validity of parameter n
                    var targParam = currentMethod.Parameters[n];
                    if (targParam.ParameterType.IsByReference == false)
                        throw new InvalidOperationException();
                    Instruction add_vrvInstruction = GetMethodRefOp_vrv(processor, callMethodRef, methodOpName);
                    if (add_vrvInstruction == null)
                        return false;


                    // replace 'Ldarg' with 'Ldarga'
                    //processor.Replace(prevInstruction, newLdarga);
                    processor.Remove(LdobjInstruction);
                    // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2, ref vector2)'
                    processor.Replace(callInstruction, add_vrvInstruction);

                    return true;
                }
            }
            else
            {
                Instruction add_vvvInstruction = GetMethodRefOp_vvv(processor, callMethodRef, methodOpName);
                if (add_vvvInstruction == null)
                    return false;


                // test replace method. 
                // Add_vvv is identical to operator+
                //processor.Replace(call_Op_Addition, add_vvvInstruction);

                return false;
            }

            return false;
        }
        
        private bool ProcessOpCall_retout(ILProcessor processor, Instruction callInstruction, MethodReference callMethodRef, string methodOpName)
        {
            return false;
        }

        private static Instruction GetMethodRefOp_vrv(ILProcessor processor, MethodReference callMethodRef, string methodOpName)
        {
            MethodReference MethodDefOp_vrv = null;
            var typeDef = callMethodRef.DeclaringType.Resolve();
            foreach (var method in typeDef.Methods)
            {
                if (method.Name == methodOpName &&
                    method.Parameters.Count == 2 &&
                    method.Parameters[0].ParameterType.IsByReference == false &&
                    method.Parameters[1].ParameterType.IsByReference == true)
                {
                    MethodDefOp_vrv = method;
                    break;
                }
            }

            if (MethodDefOp_vrv == null)
            {
                //method not found
                return null;
            }

            var methodRefOp_vrv = callMethodRef.DeclaringType.Module.ImportReference(MethodDefOp_vrv);
            var op_vrvInstruction = processor.Create(OpCodes.Call, methodRefOp_vrv);
            return op_vrvInstruction;
        }
                
        private static Instruction GetMethodRefOp_vvv(ILProcessor processor, MethodReference callMethodRef, string methodOpName)
        {
            MethodReference MethodDefOp_vvv = null;
            var typeDef = callMethodRef.DeclaringType.Resolve();
            foreach (var method in typeDef.Methods)
            {
                if (method.Name == methodOpName &&
                    method.Parameters.Count == 2 &&
                    method.Parameters[0].ParameterType.IsByReference == false &&
                    method.Parameters[1].ParameterType.IsByReference == false)
                {
                    MethodDefOp_vvv = method;
                    break;
                }
            }

            if (MethodDefOp_vvv == null)
            {
                //method not found
                return null;
            }

            var methodRefOp_vvv = callMethodRef.DeclaringType.Module.ImportReference(MethodDefOp_vvv);
            var op_vvvInstruction = processor.Create(OpCodes.Call, methodRefOp_vvv);
            return op_vvvInstruction;
        }

        private static Instruction Ldloc2Ldloca(ILProcessor processor, Instruction LdlocInstruction, out int n)
        {
            if (LdlocInstruction.OpCode.Code == Code.Ldloc)
            {
                var varDef = LdlocInstruction.Operand as VariableDefinition;
                n = varDef.Index;
                return processor.Create(OpCodes.Ldloca, processor.Body.Variables[n]);
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

                return processor.Create(OpCodes.Ldloca_S, processor.Body.Variables[n]);
                
        }
        
        private static Instruction Ldarg2Ldarga(ILProcessor processor, Instruction LdargInstruction, out int n)
        {
            if (LdargInstruction.OpCode.Code == Code.Ldarg)
            {
                var varDef = LdargInstruction.Operand as VariableDefinition;
                n = varDef.Index;
                return processor.Create(OpCodes.Ldarga, processor.Body.Method.Parameters[n]);
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

            return processor.Create(OpCodes.Ldarga_S, processor.Body.Method.Parameters[n]);
        }

        #region Match instruction
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
            return (prevInstruction.OpCode.Code == Code.Ldarg_0 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_1 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_2 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_3 ||
                    prevInstruction.OpCode.Code == Code.Ldarg_S ||
                    prevInstruction.OpCode.Code == Code.Ldarg);
        }

        private static bool Match_Ldobj(Instruction prevInstruction)
        {
            return prevInstruction.OpCode.Code == Code.Ldobj;
        }
        #endregion Match instruction

    }
}
