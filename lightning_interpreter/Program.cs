using System;
using System.Collections.Generic;
using System.IO;

using lightning;

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
                string name = System.IO.Path.ChangeExtension(path, null);
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
//////////////////////////////////////////////////////// TOKENS
            Scanner scanner = new Scanner(input, name);
            Parser parser = new Parser(scanner.Tokens);

//////////////////////////////////////////////////////// AST
            Node program = parser.ParsedTree;
            if (parser.Warnings.Count > 0)
            {
                Console.WriteLine("Parsing had Warnings:");
                foreach (string error in parser.Warnings)
                {
                    Console.WriteLine(error);
                }
            }
            if(parser.HasParsed == false){
                Console.WriteLine("Parsing had Errors, check _parser.log!");
                return  0;
            }
            PrettyPrinter astPrinter = new PrettyPrinter();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"_ast.out", false)){
                Console.SetOut(file);
                astPrinter.Print(program);
                var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }

//////////////////////////////////////////////////////// CHUNK

            Chunker code_generator = new Chunker(program, name, Prelude.GetPrelude());
            Chunk chunk = code_generator.Chunk;
            if(code_generator.Errors.Count > 0)
            {
                Console.WriteLine("\nCompiling had errors!");
                foreach(string e in code_generator.Errors)
                {
                    Console.WriteLine(e);
                }
                return 0;
            }
            if (code_generator.HasChunked == true)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"_chunk.out", false)){
                    Console.SetOut(file);
                    chunk.Print();
                    var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                    standardOutput.AutoFlush = true;
                    Console.SetOut(standardOutput);
                }

                VM vm = new VM(chunk);
                VMResult result = vm.ProtectedRun();
                if (result.status != VMResultType.OK)
                    Console.WriteLine("Program returned ERROR");
                else if (result.value.Type != UnitType.Null)
                    Console.WriteLine("Program returned: " + result.value);

                // Print memory_use
                Unit my_func = chunk.GetUnitFromTable("machine", "memory_use");
                Unit call_result = vm.ProtectedCallFunction(my_func, null);
                Console.WriteLine(call_result);
            }
            else
            {
                if (code_generator.Errors.Count > 0)
                {
                    Console.WriteLine("Code generation had errors, check _chunker.log");
                    return 0;
                }
            }
            return 0;
        }
    }
}
