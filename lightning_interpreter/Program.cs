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
            Scanner scanner = new Scanner(input);
            if (scanner.Errors.Count > 0)
            {
                Console.WriteLine("Scanning had errors:");
                foreach(string error in scanner.Errors)
                {
                    Console.WriteLine(error);
                }
                return 0;
            }

            Parser parser = new Parser(scanner.Tokens);
            Node program = parser.ParsedTree;
            if (parser.Errors.Count > 0)
            {
                Console.WriteLine("Parsing had errors:");
                foreach (string error in parser.Errors)
                {
                    Console.WriteLine(error);
                }
                return 0;
            }
            if (parser.Warnings.Count > 0)
            {
                Console.WriteLine("Parsing had Warnings:");
                foreach (string error in parser.Warnings)
                {
                    Console.WriteLine(error);
                }
            }
            bool skip_line = false;
#if TOKENS
            Console.WriteLine("---------------------------------- Tokens:");

            foreach (Token token in scanner.Tokens)
            {
               Console.WriteLine(token.ToString());
            }
            Console.WriteLine("-------------------------------end Tokens:");
            skip_line = true;
#endif
#if AST
            Console.WriteLine("\n---------------------------------- AST:");

            PrettyPrinter astPrinter = new PrettyPrinter();
            astPrinter.Print(program);
            Console.WriteLine("\n-------------------------------end AST:");
            skip_line = true;
#endif

            Chunker code_generator = new Chunker(program, "main", Prelude.GetPrelude());
            Chunk chunk = code_generator.Code;
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
#if CHUNK
                Console.WriteLine("\n---------------------------------- Generated Chunk:");
                Console.WriteLine();
                chunk.Print();
#endif
#if CONSTANTS
                foreach(Unit v in chunk.GetConstants())
                {
                   Console.WriteLine(v);
                }
                Console.WriteLine("\n-------------------------------end Generated Chunk:");
                skip_line = true;
#endif
                if(skip_line)
                    Console.WriteLine();

                VM vm = new VM(chunk);
                VMResult result = vm.Run();
                if (result.status != VMResultType.OK)
                    Console.WriteLine("Program returned ERROR");
                else if (result.value != Unit.Null)
                    Console.WriteLine("Program returned: " + result.value);
            }
            else
            {
                if (code_generator.Errors.Count > 0)
                {
                    Console.WriteLine("Code generation had errors:");
                    foreach (string error in code_generator.Errors)
                        Console.WriteLine(error);
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
