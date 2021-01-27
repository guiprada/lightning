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
            Console.WriteLine("Welcome to lightning :)");
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: lightning_interpreter [script]");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
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

        static void RunPrompt()
        {
            bool is_running = true;
            Node prog_node = new ProgramNode(null, 0);
            Chunker code_generator = new Chunker(prog_node, "main", Prelude.GetPrelude());
            Chunk chunk = code_generator.Code;
            
            Value eval = chunk.GetFunction("eval");
            if (eval == null)
            {
                throw(new Exception("Could not find eval function!"));

            }
            if (code_generator.HasChunked == true)
            {
                VM vm = new VM(chunk);
                while (is_running)
                {
                    Console.Write(">");
                    string input = Console.ReadLine();
                    if (input != "")
                    {
                        List<Value> stack = new List<Value>();
                        stack.Add(new ValString(input));
                        Value result = vm.CallFunction(eval, stack);
                        Console.WriteLine(result);
                    }
                }
                //VMResult result = vm.Run();                
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

            //Console.WriteLine("---------------------------------- Tokens:");

            //foreach (Token token in scanner.Tokens)
            //{
            //    Console.WriteLine(token.ToString());
            //}
            //Console.WriteLine("\n---------------------------------- AST:");

            //PrettyPrinter astPrinter = new PrettyPrinter();
            //astPrinter.Print(program);

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
                //Console.WriteLine("\n---------------------------------- Generated Chunk:");
                //Console.WriteLine();
                //chunk.Print();

                //foreach(Value v in chunk.GetConstants())
                //{
                //    Console.WriteLine(v);
                //}

                VM vm = new VM(chunk);
                VMResult result = vm.Run();
                if (result.status == VMResultType.OK)
                    Console.WriteLine("Program returned: " + result.value);
                vm.Stats();
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
            //List<Value> stack = new List<Value>();
            //stack.Add(new ValString("hello"));
            //stack.Add(new ValString("hello!"));
            //stack.Add(new ValString("Failed--------------------------------"));
            //Value call_result = vm.CallFunction(my_func, stack);
            //Console.WriteLine(call_result);

            return 0;
        }
    }
}
