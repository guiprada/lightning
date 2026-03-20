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
            if (args.Length == 1)
            {
                RunFile(args[0], forceCompile: false);
            }
            else if (args.Length == 2 && args[0] == "--compile")
            {
                RunFile(args[1], forceCompile: true);
            }
            else
            {
                Console.WriteLine("Usage: lightning_interpreter [script.ltn]");
                Console.WriteLine("       lightning_interpreter --compile [script.ltn]  (force recompile)");
                Console.WriteLine("       lightning_interpreter [script.ltnc]           (load bytecode directly)");
                Environment.Exit(64);
            }
        }

        // Returns true if the .ltnc is up-to-date relative to the .ltn source.
        static bool BytecodeIsFresh(string ltnPath, string ltncPath)
        {
            return File.Exists(ltncPath) &&
                   File.GetLastWriteTimeUtc(ltncPath) >= File.GetLastWriteTimeUtc(ltnPath);
        }

        static int RunFile(string path, bool forceCompile)
        {
            try
            {
                if (path.EndsWith(".ltnc"))
                    return RunBytecode(path);

                string ltncPath = path + "c"; // script.ltn -> script.ltnc

                if (!forceCompile && BytecodeIsFresh(path, ltncPath))
                    return RunBytecode(ltncPath);

                // Compile (and always save .ltnc)
                string input;
                using (var sr = new StreamReader(path))
                    input = sr.ReadToEnd();
                string name = lightningPath.ModuleName(path);
                return Run(input, name, saveAs: ltncPath);
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                Environment.Exit(65);
                return -1;
            }
        }

        static int RunBytecode(string path)
        {
            Chunk chunk = Chunk.Load(path, Prelude.GetPrelude());
            return RunChunk(chunk);
        }

        static int Run(string input, string name, string saveAs = null)
        {
            Scanner scanner = new Scanner(input, name);
            List<Token> tokens = scanner.Tokens;
            if (scanner.HasScanned == false) {
                Console.WriteLine("Scanner Error!");
                return 0;
            }

            Parser parser = new Parser(tokens, name);
            Node program = parser.ParsedTree;
            if (parser.HasParsed == false) {
                Console.WriteLine("Parser Error!");
                return 0;
            }

            Compiler chunker = new Compiler(program, name, Prelude.GetPrelude());
            Chunk chunk = chunker.Chunk;
            if (chunker.HasChunked == false)
            {
                Console.WriteLine("Compiler Error!");
                return 0;
            }

            if (saveAs != null)
                chunk.Save(saveAs);

            return RunChunk(chunk);
        }

        static int RunChunk(Chunk chunk)
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

            return 0;
        }
    }
}
