﻿using System;
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
            Info();

            var filename = args.FirstOrDefault();

            if (filename == null)
            {
                Usage();
                return 0;
            }
            try
            {
                if (!Path.IsPathRooted(filename))
                    filename = Path.GetFullPath(filename);

                if (!File.Exists(filename))
                {                    
                    Console.WriteLine(filename);
                    Console.WriteLine(@"File not found.");  
                    Console.WriteLine(@"");                  
                    return -1;
                }

                AssemblyProcessor assemblyProcessor = new AssemblyProcessor(filename);
                assemblyProcessor.Open();

                assemblyProcessor.ProcessMainModule();

                

                // Save
                var outputAssemblyFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".optmzd" + Path.GetExtension(filename));                
                Console.WriteLine(@"Saving " + outputAssemblyFilename);
                assemblyProcessor.Save(outputAssemblyFilename);
                Console.WriteLine(@" ");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }
        
        private static void Info()
        {
            Console.WriteLine(@"PostOpt.Net v1.0");
            Console.WriteLine(@"");
        }      

        private static void Usage()
        {
            Console.WriteLine(@"Usage: PostOpt filename");
            Console.WriteLine(@"");

        }        
    }
}
