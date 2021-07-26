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
                return Run(input);
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Environment.Exit(65);
                return -1;
            }
        }

        static int Run(string input)
        {
//////////////////////////////////////////////////////// TOKENS
            Scanner scanner = new Scanner(input);
            Parser parser = new Parser(scanner.Tokens);
            if (scanner.Errors.Count > 0)
            {
                Console.WriteLine("Scanning had errors:");
                foreach(string error in scanner.Errors)
                {
                    Console.WriteLine(error);
                }
                return 0;
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"_tokens.out", false)){
                Console.SetOut(file);
                foreach (Token token in scanner.Tokens)
                {
                    Console.WriteLine(token.ToString());
                }
                var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }

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

            Chunker code_generator = new Chunker(program, "main", Prelude.GetPrelude());
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
            }
            else
            {
                if (code_generator.Errors.Count > 0)
                {
                    Console.WriteLine("Code generation had errors, check _chunker.log");
                    return 0;
                }
            }

            //ValFunction my_func = chunk.GetFunction("append");
            //List<HeapUnit> stack = new List<HeapUnit>();
            //stack.Add(new ValString("hello"));
            //stack.Add(new ValString("hello!"));
            //stack.Add(new ValString("Failed--------------------------------"));
            //HeapUnit call_result = vm.CallFunction(my_func, stack);
            //Console.WriteLine(call_result);

            return 0;
        }
    }
}
