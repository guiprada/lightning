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
                string name = Utils.ModuleName(path);
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
            if(scanner.HasScanned == false){ return 0; }

            Parser parser = new Parser(tokens, name);
            Node program = parser.ParsedTree;
            if(parser.HasParsed == false){ return 0; }

            Chunker chunker = new Chunker(program, name, Prelude.GetPrelude());
            Chunk chunk = chunker.Chunk;
            if (chunker.HasChunked == true)
            {
                VM vm = new VM(chunk);
                VMResult result = vm.ProtectedRun();
                if (result.status != VMResultType.OK)
                    Console.WriteLine("Program returned ERROR!");
                else if (result.value.Type != UnitType.Null)
                    Console.WriteLine("Program returned: " + result.value);

                // Print memory_use
                Unit my_func = chunk.GetUnitFromTable("machine", "memory_use");
                Unit call_result = vm.ProtectedCallFunction(my_func, null);
                Console.WriteLine(call_result);
            }
            return 0;
        }
    }
}
