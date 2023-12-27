using System;
using System.Collections.Generic;
using System.IO;

using lightningTools;
using lightningVM;
using lightningCompiler;
using lightningAST;
using lightningChunk;
using lightningPrelude;
using lightningUnit;
namespace interpreter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: lightning_interpreter [script]");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
        }

        static int RunFile(string path)
        {
            string input;
            try
            {
                using (var sr = new StreamReader(path))
                {
                    input = sr.ReadToEnd();
                }
                string name = lightningPath.ModuleName(path);
                return Run(input, name);
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Environment.Exit(65);
                return -1;
            }
        }

        static int Run(string input, string name)
        {
            Scanner scanner = new Scanner(input, name);
            List<Token> tokens = scanner.Tokens;
            if (scanner.HasScanned == false) { return 0; }

            Parser parser = new Parser(tokens, name);
            Node program = parser.ParsedTree;
            if (parser.HasParsed == false) { return 0; }

            Compiler chunker = new Compiler(program, name, Prelude.GetPrelude());
            Chunk chunk = chunker.Chunk;
            if (chunker.HasChunked == true)
            {
                VM vm = new VM(chunk);
                ResultUnit result = vm.ProtectedRun();
                if (result.IsOK)
                    Console.WriteLine("Program returned: " + result.Value);
                else
                    Console.WriteLine("Program returned ERROR!");

                // Print modules
                Unit machine_modules = chunk.GetUnitFromTable("machine", "modules");
                Console.WriteLine(vm.ProtectedCallFunction(machine_modules, null));

                // Print memory_use
                Unit machine_memory_use = chunk.GetUnitFromTable("machine", "memory_use");
                Console.WriteLine(vm.ProtectedCallFunction(machine_memory_use, null));

                // Print Global variable errors
                // Unit errors_unit = vm.GetGlobal(chunk, "errors");
                // if (errors_unit.Type == UnitType.Integer)
                //     Console.WriteLine("Total errors in test: " + errors_unit.integerValue);
            }
            return 0;
        }
    }
}
