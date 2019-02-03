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
    class Program
    {
        static int Main(string[] args)
        {
            var filename = args.FirstOrDefault();

            if (filename == null)
            {
                Usage();
                return 0;
            }
            try
            {
                ProcessModule(filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }

        private static void Usage()
        {
            Console.WriteLine(@"Usage: PostOpt filename");

        }

        private static void ProcessModule(string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                filename = Path.GetFullPath(filename);
            }

            Directory.SetCurrentDirectory(Path.GetDirectoryName(filename));

            //Creates an AssemblyDefinition from the "MyLibrary.dll" assembly
            AssemblyDefinition myLibrary = AssemblyDefinition.ReadAssembly(filename);

            //Gets all types which are declared in the Main Module of "MyLibrary.dll"
            foreach (TypeDefinition type in myLibrary.MainModule.Types)
            {
                //Writes the full name of a type
                Console.WriteLine(type.FullName);
            }

            //var moduleDefinition = ModuleDefinition.ReadModule(filename);
            var moduleDefinition = myLibrary.MainModule;
            var methods = from type in moduleDefinition.Types
                          from method in type.Methods
                          select method;

            foreach (var methodDefinition in methods)
            {
                try
                {
                    while(ProcessMethod(myLibrary, moduleDefinition, methodDefinition));                    
                }
                catch (Exception ex)
                {

                }
            }

            var newfilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".optmzd" + Path.GetExtension(filename));


            moduleDefinition.Write(newfilename);
        }



        private static bool ProcessMethod(AssemblyDefinition myLibrary, ModuleDefinition moduleDefinition, MethodDefinition methodDefinition)
        {
            var body = methodDefinition.Body;
            var processor = body.GetILProcessor();
            

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Call)
                {
                    var operand = instruction.Operand as MethodReference;
                    if (operand.Name == "op_Addition")
                    {
                        if (operand.DeclaringType.FullName == "Xna.Framework.Vector2")
                        {
                            var call_Op_Addition = instruction;

                            var prevInstruction = call_Op_Addition.Previous;
                            if (prevInstruction.OpCode.Code == Code.Ldloc_0 ||
                                prevInstruction.OpCode.Code == Code.Ldloc_1 ||
                                prevInstruction.OpCode.Code == Code.Ldloc_2 ||
                                prevInstruction.OpCode.Code == Code.Ldloc_3)
                            {

                                Instruction newLdloca = null;
                                if (prevInstruction.OpCode.Code == Code.Ldloc_0)
                                    newLdloca = processor.Create(OpCodes.Ldloca_S, processor.Body.Variables[0]);
                                if (prevInstruction.OpCode.Code == Code.Ldloc_1)
                                    newLdloca = processor.Create(OpCodes.Ldloca_S, processor.Body.Variables[1]);
                                if (prevInstruction.OpCode.Code == Code.Ldloc_2)
                                    newLdloca = processor.Create(OpCodes.Ldloca_S, processor.Body.Variables[2]);
                                if (prevInstruction.OpCode.Code == Code.Ldloc_3)
                                    newLdloca = processor.Create(OpCodes.Ldloca_S, processor.Body.Variables[3]);


                                //var callsite = new CallSite(operand.ReturnType);
                                //callsite.CallingConvention = MethodCallingConvention.StdCall;
                                //foreach (var param in operand.Parameters)
                                //{
                                //    var newParam = new ParameterDefinition(param.ParameterType);
                                //    callsite.Parameters.Add(newParam);
                                //}
                                //var instr = processor.Create(OpCodes.Call, callsite);

                                MethodReference MethodDefAdd_vrv = null;
                                var test = operand.DeclaringType.Resolve();
                                foreach (var method in test.Methods)
                                {
                                    if (method.Name == "Add" &&
                                        method.Parameters.Count == 2 &&
                                        method.Parameters[0].ParameterType.IsByReference == false &&
                                        method.Parameters[1].ParameterType.IsByReference == true)
                                    {
                                        MethodDefAdd_vrv = method;
                                        break;
                                    }
                                }

                                var methodRefAdd_vrv = operand.DeclaringType.Module.ImportReference(MethodDefAdd_vrv);
                                var add_vrvInstruction = processor.Create(OpCodes.Call, methodRefAdd_vrv);

                                // replace 'Ldloc' with 'Ldloca'
                                processor.Replace(prevInstruction, newLdloca);
                                // replace 'vector2 Add(vector2,vector2)' with 'vector2 Add(vector2,vector2)'
                                processor.Replace(call_Op_Addition, add_vrvInstruction);

                                return true;
                            }
                            else
                            {
                                MethodReference MethodDefAdd_vvv = null;
                                var test = operand.DeclaringType.Resolve();
                                foreach (var method in test.Methods)
                                {
                                    if (method.Name == "Add" &&
                                        method.Parameters.Count == 2 &&
                                        method.Parameters[0].ParameterType.IsByReference == false &&
                                        method.Parameters[1].ParameterType.IsByReference == false)
                                    {
                                        MethodDefAdd_vvv = method;
                                        break;
                                    }
                                }

                                var methodRefAdd_vvv = operand.DeclaringType.Module.ImportReference(MethodDefAdd_vvv);
                                var add_vvvInstruction = processor.Create(OpCodes.Call, methodRefAdd_vvv);

                                // test replace method. 
                                // Add_vvv is identical to operator+
                                //processor.Replace(call_Op_Addition, add_vvvInstruction);

                                return true;
                            }
                        }
                    }
                }                
            }

            return false;

            //foreach (var instruction in body.Instructions.Reverse())
            //{
            //    if (instruction.OpCode == OpCodes.Call)
            //    {
            //        var operand = instruction.Operand as MethodReference;
            //        if (operand.DeclaringType.FullName == "Xna.Framework.Vector2")
            //        {
            //            if (operand.Name == "op_Addition")
            //            {
            //            }
            //        }
            //    }
            //
            //    //if (instruction.OpCode == OpCodes.Ret)
            //    //{
            //    //	seenRet = true;
            //    //}
            //    //else if (seenRet && instruction.OpCode.In(OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt))
            //    //{
            //    //	var tailCall = processor.Create(OpCodes.Tail);
            //    //	processor.InsertBefore(instruction, tailCall);
            //    //	seenRet = false;
            //    //}
            //    //else if (instruction.OpCode != OpCodes.Nop)
            //    //{
            //    //	seenRet = false;
            //    //}
            //}
        }

    }
}
