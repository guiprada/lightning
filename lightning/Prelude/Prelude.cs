#if DOUBLE
using Float = System.Double;
    using Integer = System.Int64;
    using Operand = System.UInt16;
#else
    using Float = System.Single;
    using Integer = System.Int32;
    using Operand = System.UInt16;
#endif

using System;
using System.Collections.Generic;
using System.IO;

namespace lightning
{
    public struct Library
    {
        public List<IntrinsicUnit> intrinsics;
        public Dictionary<string, TableUnit> tables;

        public Library(List<IntrinsicUnit> p_intrinsics, Dictionary<string, TableUnit> p_tables)
        {
            intrinsics = p_intrinsics;
            tables = p_tables;
        }
    }

    public class Prelude
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////// Prelude

        public static void AddLibraries(Library p_lib1, Library p_lib2)
        {
            foreach (IntrinsicUnit i in p_lib2.intrinsics)
            {
                p_lib1.intrinsics.Add(i);
            }
            foreach (KeyValuePair<string, TableUnit> entry in p_lib2.tables)
            {
                p_lib1.tables.Add(entry.Key, entry.Value);
            }
        }

        public static Library GetPrelude()
        {
            Dictionary<string, TableUnit> tables = new Dictionary<string, TableUnit>();
            tables.Add("string", StringUnit.ClassMethodTable);
            tables.Add("list", ListUnit.ClassMethodTable);
            tables.Add("table", TableUnit.ClassMethodTable);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////// machine
            tables.Add("machine", Machine.GetTableUnit());

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// tuple
            tables.Add("tuple", Tuple.GetTableUnit());

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// nuple
            tables.Add("nuple", Nuple.GetTableUnit());

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// roslyn
            {
#if ROSLYN
                tables.Add("roslyn", Roslyn.GetTableUnit());
#else
                tables.Add("roslyn", new Unit(UnitType.Null));
#endif
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// rand

            {
                TableUnit rand = new TableUnit(null);

                var rng = new Random();

                Unit NextInt(VM p_vm)
                {
                    int max = (int)(p_vm.GetInteger(0));
                    return new Unit(rng.Next(max));
                }
                rand.Set("int", new IntrinsicUnit("int", NextInt, 1));

                Unit NextFloat(VM p_vm)
                {
                    return new Unit((Float)rng.NextDouble());
                }
                rand.Set("float", new IntrinsicUnit("float", NextFloat, 0));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                TableUnit math = new TableUnit(null);
                math.Set("pi", (Float)Math.PI);
                math.Set("e", (Float)Math.E);
#if DOUBLE
                math.Set("double", true);
#else
                math.Set("double", false);
#endif

                //////////////////////////////////////////////////////
                Unit Sin(VM p_vm)
                {
                    return new Unit((Float)Math.Sin(p_vm.GetNumber(0)));
                }
                math.Set("sin", new IntrinsicUnit("sin", Sin, 1));

                //////////////////////////////////////////////////////
                Unit Cos(VM p_vm)
                {
                    return new Unit((Float)Math.Cos(p_vm.GetNumber(0)));
                }
                math.Set("cos", new IntrinsicUnit("cos", Cos, 1));

                //////////////////////////////////////////////////////
                Unit Tan(VM p_vm)
                {
                    return new Unit((Float)Math.Tan(p_vm.GetNumber(0)));
                }
                math.Set("tan", new IntrinsicUnit("tan", Tan, 1));

                //////////////////////////////////////////////////////
                Unit Sec(VM p_vm)
                {
                    return new Unit((Float)(1 / Math.Cos(p_vm.GetNumber(0))));
                }
                math.Set("sec", new IntrinsicUnit("sec", Sec, 1));

                //////////////////////////////////////////////////////
                Unit Cosec(VM p_vm)
                {
                    return new Unit((Float)(1 / Math.Sin(p_vm.GetNumber(0))));
                }
                math.Set("cosec", new IntrinsicUnit("cosec", Cosec, 1));

                //////////////////////////////////////////////////////
                Unit Cotan(VM p_vm)
                {
                    return new Unit((Float)(1 / Math.Tan(p_vm.GetNumber(0))));
                }
                math.Set("cotan", new IntrinsicUnit("cotan", Cotan, 1));

                //////////////////////////////////////////////////////
                Unit Asin(VM p_vm)
                {
                    return new Unit((Float)Math.Asin(p_vm.GetNumber(0)));
                }
                math.Set("asin", new IntrinsicUnit("asin", Asin, 1));

                //////////////////////////////////////////////////////
                Unit Acos(VM p_vm)
                {
                    return new Unit((Float)Math.Acos(p_vm.GetNumber(0)));
                }
                math.Set("acos", new IntrinsicUnit("acos", Acos, 1));

                //////////////////////////////////////////////////////
                Unit Atan(VM p_vm)
                {
                    return new Unit((Float)Math.Atan(p_vm.GetNumber(0)));
                }
                math.Set("atan", new IntrinsicUnit("atan", Atan, 1));

                //////////////////////////////////////////////////////
                Unit Sinh(VM p_vm)
                {
                    return new Unit((Float)Math.Sinh(p_vm.GetNumber(0)));
                }
                math.Set("sinh", new IntrinsicUnit("sinh", Sinh, 1));

                //////////////////////////////////////////////////////
                Unit Cosh(VM p_vm)
                {
                    return new Unit((Float)Math.Cosh(p_vm.GetNumber(0)));
                }
                math.Set("cosh", new IntrinsicUnit("cosh", Cosh, 1));

                //////////////////////////////////////////////////////
                Unit Tanh(VM p_vm)
                {
                    return new Unit((Float)Math.Tanh(p_vm.GetNumber(0)));
                }
                math.Set("tanh", new IntrinsicUnit("tanh", Tanh, 1));

                //////////////////////////////////////////////////////
                Unit Pow(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    Float exponent = p_vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, exponent));
                }
                math.Set("pow", new IntrinsicUnit("pow", Pow, 2));

                //////////////////////////////////////////////////////
                Unit Root(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    Float exponent = p_vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, 1 / exponent));
                }
                math.Set("root", new IntrinsicUnit("root", Root, 2));

                //////////////////////////////////////////////////////
                Unit Sqroot(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    return new Unit((Float)Math.Sqrt(value));
                }
                math.Set("sqroot", new IntrinsicUnit("sqroot", Sqroot, 1));
                //////////////////////////////////////////////////////
                Unit Exp(VM p_vm)
                {
                    Float exponent = p_vm.GetNumber(0);
                    return new Unit((Float)Math.Exp(exponent));
                }
                math.Set("exp", new IntrinsicUnit("exp", Exp, 1));

                //////////////////////////////////////////////////////
                Unit Log(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    Float this_base = p_vm.GetNumber(1);
                    return new Unit((Float)Math.Log(value, this_base));
                }
                math.Set("log", new IntrinsicUnit("log", Log, 2));

                //////////////////////////////////////////////////////
                Unit Ln(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, Math.E));
                }
                math.Set("ln", new IntrinsicUnit("ln", Ln, 1));

                //////////////////////////////////////////////////////
                Unit Log10(VM p_vm)
                {
                    Float value = p_vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, (Float)10));
                }
                math.Set("log10", new IntrinsicUnit("log10", Log10, 1));

                //////////////////////////////////////////////////////
                Unit Mod(VM p_vm)
                {
                    Float value1 = p_vm.GetNumber(0);
                    Float value2 = p_vm.GetNumber(1);
                    return new Unit(value1 % value2);
                }
                math.Set("mod", new IntrinsicUnit("mod", Mod, 2));

                //////////////////////////////////////////////////////
                Unit Idiv(VM p_vm)
                {
                    Float value1 = p_vm.GetNumber(0);
                    Float value2 = p_vm.GetNumber(1);
                    return new Unit((Integer)(value1 / value2));
                }
                math.Set("idiv", new IntrinsicUnit("idiv", Idiv, 2));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                TableUnit time = new TableUnit(null);
                TableUnit timeMethods = new TableUnit(null);

                Unit TimeNow(VM p_vm)
                {
                    return new Unit(new WrapperUnit<long>(DateTime.Now.Ticks, timeMethods));
                }
                time.Set("now", new IntrinsicUnit("time_now", TimeNow, 0));

                //////////////////////////////////////////////////////
                Unit TimeReset(VM p_vm)
                {
                    WrapperUnit<long> this_time = p_vm.GetWrapperUnit<long>(0);
                    this_time.content = DateTime.Now.Ticks;
                    return new Unit(UnitType.Null);
                }
                timeMethods.Set("reset", new IntrinsicUnit("time_reset", TimeReset, 1));

                //////////////////////////////////////////////////////
                Unit TimeElapsed(VM p_vm)
                {
                    long timeStart = p_vm.GetWrappedContent<long>(0);
                    long timeEnd = DateTime.Now.Ticks;
                    return new Unit((Integer)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                timeMethods.Set("elapsed", new IntrinsicUnit("time_elapsed", TimeElapsed, 1));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null);

                //////////////////////////////////////////////////////

                Unit IsAlpha(VM p_vm)
                {
                    string input_string = p_vm.GetString(0);
                    if (1 <= input_string.Length)
                    {
                        char head = input_string[0];
                        if (Char.IsLetter(head))
                        {
                            return new Unit(true);
                        }
                    }
                    return new Unit(false);
                }
                char_table.Set("is_alpha", new IntrinsicUnit("char_is_alpha", IsAlpha, 1));

                //////////////////////////////////////////////////////

                Unit IsDigit(VM p_vm)
                {
                    string input_string = p_vm.GetString(0);
                    if (1 <= input_string.Length)
                    {
                        char head = input_string[0];
                        if (Char.IsDigit(head))
                        {
                            return new Unit(true);
                        }
                        return new Unit(false);
                    }
                    return new Unit(UnitType.Null);
                }
                char_table.Set("is_digit", new IntrinsicUnit("char_is_digit", IsDigit, 1));

                tables.Add("char", char_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// file
            {
                TableUnit file = new TableUnit(null);
                Unit LoadFile(VM p_vm)
                {
                    string path = p_vm.GetString(0);
                    string input;
                    using (var sr = new StreamReader(path))
                    {
                        input = sr.ReadToEnd();
                    }
                    if (input != null)
                        return new Unit(input);

                    return new Unit(UnitType.Null);
                }
                file.Set("load", new IntrinsicUnit("file_load_file", LoadFile, 1));

                //////////////////////////////////////////////////////
                Unit WriteFile(VM p_vm)
                {
                    string path = p_vm.GetString(0);
                    string output = p_vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return new Unit(UnitType.Null);
                }
                file.Set("write", new IntrinsicUnit("file_write_file", WriteFile, 2));

                //////////////////////////////////////////////////////
                Unit AppendFile(VM p_vm)
                {
                    string path = p_vm.GetString(0);
                    string output = p_vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }

                    return new Unit(UnitType.Null);
                }
                file.Set("append", new IntrinsicUnit("file_append_file", AppendFile, 2));

                tables.Add("file", file);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////// Global Intrinsics

            List<IntrinsicUnit> functions = new List<IntrinsicUnit>();

            //////////////////////////////////////////////////////
            Unit Eval(VM p_vm)
            {
                string eval_code = p_vm.GetString(0);
                string eval_name = "eval@" + Path.ToLambdaName(p_vm.CurrentInstructionModule())+ p_vm.CurrentInstructionPositionData();

                Scanner scanner = new Scanner(eval_code, eval_name);
                List<Token> tokens = scanner.Tokens;
                if(scanner.HasScanned == false){
                    throw new Exception("Scanning Error");
                }

                Parser parser = new Parser(tokens, eval_name);

                Node program = parser.ParsedTree;
                if(parser.HasParsed == false){
                        throw new Exception("Parsing Error");
                }

                Compiler chunker = new Compiler(program, eval_name, p_vm.Prelude);
                Chunk chunk = chunker.Chunk;
                if (chunker.HasChunked == false){
                    throw new Exception("Code Generation Error");
                }

                if (chunker.HasChunked == true)
                {
                    VM imported_vm = new VM(chunk);
                    VMResult result = imported_vm.ProtectedRun();
                    if (result.status == VMResultType.OK)
                    {
                        if (result.value.Type == UnitType.Table || result.value.Type == UnitType.Function)
                            MakeModule(result.value, eval_name, p_vm, imported_vm);
                        return result.value;
                    }
                }
                throw new Exception("Code Execution not OK :|");
            }
            functions.Add(new IntrinsicUnit("eval", Eval, 1));

            ////////////////////////////////////////////////////
            Unit Require(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string name = Path.ModuleName(path);
                foreach (ModuleUnit v in p_vm.modules)// skip already imported modules
                {
                    if (v.Name == name)
                        return new Unit(v);
                }
                string module_code;
                using (var sr = new StreamReader(path))
                {
                    module_code = sr.ReadToEnd();
                }
                if (module_code != null)
                {

                    Scanner scanner = new Scanner(module_code, name);
                    List<Token> tokens = scanner.Tokens;
                    if(scanner.HasScanned == false){
                        throw new Exception("Scanning Error");
                    }

                    Parser parser = new Parser(tokens, name);
                    Node program = parser.ParsedTree;
                    if(parser.HasParsed == false){
                        throw new Exception("Parsing Error");
                    }

                    Compiler chunker = new Compiler(program, name, p_vm.Prelude);
                    Chunk chunk = chunker.Chunk;
                    if (chunker.HasChunked == false){
                        throw new Exception("Code Generation Error");
                    }

                    if (chunker.HasChunked == true){
                        VM imported_vm = new VM(chunk);
                        VMResult result = imported_vm.ProtectedRun();
                        if (result.status == VMResultType.OK){
                            MakeModule(result.value, name, p_vm, imported_vm);
                            return result.value;
                        }
                    }
                }
                throw new Exception("Code Execution was NOT OK :|");
            }
            functions.Add(new IntrinsicUnit("require", Require, 1));

            Unit Try(VM p_vm)
            {
                Unit this_callable = p_vm.GetUnit(0);
                Unit this_unit = p_vm.GetUnit(1);

                if (Unit.IsCallable(this_callable))
                {
                    List<Unit> this_arguments = null;
                    if(this_unit.Type == UnitType.List)
                        this_arguments = ((ListUnit)this_unit.heapUnitValue).Elements;

                    VM try_vm = p_vm.GetVM();
                    try
                    {
                        try_vm.CallFunction(this_callable, this_arguments);
                        p_vm.RecycleVM(try_vm);
                        return new Unit(true);
                    }catch(Exception e){
                        Logger.LogLine("------------------- (Try log) " + p_vm.CurrentInstructionPositionDataString(), Defaults.Config.TryLogFile);
                        Logger.LogLine(e.ToString() + "\n---", Defaults.Config.TryLogFile);
                        p_vm.RecycleVM(try_vm);
                    }
                }

                return new Unit(false);
            }
            functions.Add(new IntrinsicUnit("try", Try, 2));

            //////////////////////////////////////////////////////
            Unit WriteLine(VM p_vm)
            {
                Console.WriteLine(p_vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line", WriteLine, 1));

            //////////////////////////////////////////////////////
            Unit Write(VM p_vm)
            {
                Console.Write(p_vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write", Write, 1));

            //////////////////////////////////////////////////////
            Unit WriteLineRaw(VM p_vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Escape(p_vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line_raw", WriteLineRaw, 1));

            //////////////////////////////////////////////////////
            Unit WriteRaw(VM p_vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Escape(p_vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_raw", WriteRaw, 1));

            //////////////////////////////////////////////////////
            Unit Readln(VM p_vm)
            {
                string read = Console.ReadLine();
                return new Unit(read);
            }
            functions.Add(new IntrinsicUnit("read_line", Readln, 0));

            //////////////////////////////////////////////////////
            Unit readNumber(VM p_vm)
            {
                string read = Console.ReadLine();
                if (Float.TryParse(read, out Float n))
                    return new Unit(n);
                else
                    return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("read_number", readNumber, 0));

            //////////////////////////////////////////////////////
            Unit Read(VM p_vm)
            {
                int read = Console.Read();
                if (read > 0)
                {
                    char next = Convert.ToChar(read);
                    if (next == '\n')
                        return new Unit(UnitType.Null);
                    else
                        return new Unit(Char.ToString(next));
                }
                else
                    return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("read", Read, 0));

            //////////////////////////////////////////////////////
            Unit Type(VM p_vm)
            {
                UnitType this_type = p_vm.GetUnit(0).Type;
                return new Unit(this_type.ToString());
            }
            functions.Add(new IntrinsicUnit("type", Type, 1));

            //////////////////////////////////////////////////////
            Unit ToInteger(VM p_vm)
            {
                Float this_float = p_vm.GetFloat(0);
                return new Unit((Integer)this_float);
            }
            functions.Add(new IntrinsicUnit("to_integer", ToInteger, 1));

            //////////////////////////////////////////////////////
            Unit ToFloat(VM p_vm)
            {
                Integer this_integer = p_vm.GetInteger(0);
                return new Unit((Float)this_integer);
            }
            functions.Add(new IntrinsicUnit("to_float", ToFloat, 1));

            //////////////////////////////////////////////////////
            Unit ToString(VM p_vm)
            {
                string this_string = p_vm.GetUnit(0).ToString();
                return new Unit(this_string);
            }
            functions.Add(new IntrinsicUnit("to_string", ToString, 1));

            //////////////////////////////////////////////////////
            Unit IsNumber(VM p_vm)
            {
                Unit this_unit = p_vm.GetUnit(0);
                return new Unit(Unit.IsNumeric(this_unit));
            }
            functions.Add(new IntrinsicUnit("is_number", IsNumber, 1));

            //////////////////////////////////////////////////////
            Unit TableNew(VM p_vm)
            {
                TableUnit new_table = new TableUnit(null);

                return new Unit(new_table);
            }
            functions.Add(new IntrinsicUnit("Table", TableNew, 0));

            //////////////////////////////////////////////////////
            Unit ListNew(VM p_vm)
            {
                Integer size = p_vm.GetInteger(0);
                ListUnit new_list = new ListUnit(null);
                for(int i=0; i<size; i++)
                    new_list.Elements.Add(new Unit(UnitType.Null));

                return new Unit(new_list);
            }
            functions.Add(new IntrinsicUnit("List", ListNew, 1));

            //////////////////////////////////////////////////////
            Unit Maybe(VM p_vm)
            {
                Unit first = p_vm.stack.Peek(0);
                Unit second = p_vm.stack.Peek(1);
                if (first.Type != UnitType.Null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", Maybe, 2));

            //////////////////////////////////////////////////////
            Unit Tasks(VM p_vm)
            {

                Integer n_tasks = p_vm.GetInteger(0);
                Unit func = p_vm.GetUnit(1);
                Unit arguments = p_vm.GetUnit(2);

                VM[] vms = new VM[n_tasks];
                for (int i = 0; i < (int)n_tasks; i++)
                {
                    vms[i] = p_vm.GetParallelVM();
                }

                System.Threading.Tasks.Parallel.For(0, n_tasks, (index) =>
                {
                    List<Unit> args = new List<Unit>();
                    args.Add(arguments);
                    vms[index].ProtectedCallFunction(func, args);
                });
                for (int i = 0; i<n_tasks; i++)
                {
                    p_vm.RecycleVM(vms[i]);
                }
                return new Unit(UnitType.Null);
            }

            functions.Add(new IntrinsicUnit("tasks", Tasks, 3));

            //////////////////////////////////////////////////////
            Unit GetOS(VM p_vm)
            {
                return new Unit(Environment.OSVersion.VersionString);
            }
            functions.Add(new IntrinsicUnit("get_os", GetOS, 0));

            //////////////////////////////////////////////////////
            Unit NewLine(VM p_vm)
            {
                return new Unit(Environment.NewLine);
            }
            functions.Add(new IntrinsicUnit("new_line", NewLine, 0));

            //////////////////////////////////////////////////////
            Library prelude = new Library(functions, tables);

            return prelude;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////// Relocation

        struct RelocationInfo
        {
            public VM importingVM;
            public VM importedVM;
            public Dictionary<Operand, Operand> relocatedGlobals;
            public List<Operand> toBeRelocatedGlobals;
            public Dictionary<Operand, Operand> relocatedData;
            public List<Operand> toBeRelocatedData;
            public List<int> relocatedTables;
            public Dictionary<Operand, Operand> relocatedModules;
            public ModuleUnit module;
            public Operand moduleIndex;
            public RelocationInfo(
                VM p_importingVM,
                VM p_importedVM,
                Dictionary<Operand, Operand> p_relocatedGlobals,
                List<Operand> p_toBeRelocatedGlobals,
                Dictionary<Operand, Operand> p_relocatedData,
                List<Operand> p_toBeRelocatedData,
                List<int> p_relocatedTables,
                Dictionary<Operand, Operand> p_relocatedModules,
                ModuleUnit p_module,
                Operand p_moduleIndex)
            {
                importingVM = p_importingVM;
                importedVM = p_importedVM;
                relocatedGlobals = p_relocatedGlobals;
                toBeRelocatedGlobals = p_toBeRelocatedGlobals;
                relocatedData = p_relocatedData;
                toBeRelocatedData = p_toBeRelocatedData;
                relocatedTables = p_relocatedTables;
                relocatedModules = p_relocatedModules;
                module = p_module;
                moduleIndex = p_moduleIndex;
            }
        }

        static ModuleUnit MakeModule(Unit this_value, string name, VM importing_vm, VM imported_vm)
        {
            Dictionary<Operand, Operand> relocated_modules = new Dictionary<Operand, Operand>();
            foreach (ModuleUnit m in imported_vm.modules)
            {
                Operand old_module_index = m.ImportIndex;
                Operand copied_module_index;
                if (!importing_vm.modules.Contains(m))
                {
                    copied_module_index = importing_vm.AddModule(m);
                }
                else
                    copied_module_index = (Operand)importing_vm.modules.IndexOf(m);

                m.ImportIndex = copied_module_index;// ready for next import
                relocated_modules.Add(old_module_index, copied_module_index);
                ImportModule(m, (Operand)copied_module_index);
            }

            ModuleUnit module = new ModuleUnit(name, null, null, null);
            Operand module_index = importing_vm.AddModule(module);
            module.ImportIndex = module_index;
            RelocationInfo relocationInfo = new RelocationInfo(
                importing_vm,
                imported_vm,
                new Dictionary<Operand, Operand>(),
                new List<Operand>(),
                new Dictionary<Operand, Operand>(),
                new List<Operand>(),
                new List<int>(),
                relocated_modules,
                module,
                (Operand)module_index);

            if (this_value.Type == UnitType.Function)
                RelocateFunction((FunctionUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.Type == UnitType.Closure)
                RelocateClosure((ClosureUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.Type == UnitType.Table)
                FindFunction((TableUnit)this_value.heapUnitValue, relocationInfo);

            return module;
        }

        static void FindFunction(TableUnit table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<Unit, Unit> entry in table.Map)
                {
                    if (entry.Value.Type == UnitType.Function)
                        RelocateFunction((FunctionUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.Type == UnitType.Closure)
                        RelocateClosure((ClosureUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.Type == UnitType.Table)
                        FindFunction((TableUnit)entry.Value.heapUnitValue, relocationInfo);

                    relocationInfo.module.Set(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ClosureUnit closure, RelocationInfo relocationInfo)
        {
            foreach (UpValueUnit v in closure.UpValues)
            {
                if (v.UpValue.Type == UnitType.Closure)
                {
                    RelocateClosure((ClosureUnit)v.UpValue.heapUnitValue, relocationInfo);
                }
                else if (v.UpValue.Type == UnitType.Function)
                {
                    RelocateFunction((FunctionUnit)v.UpValue.heapUnitValue, relocationInfo);
                }
            }

            RelocateFunction(closure.Function, relocationInfo);
        }

        static void RelocateFunction(FunctionUnit function, RelocationInfo relocationInfo)
        {
            List<TableUnit> relocation_stack = new List<TableUnit>();
            RelocateChunk(function, relocationInfo);

            for (Operand i = 0; i < relocationInfo.toBeRelocatedGlobals.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetGlobal(relocationInfo.toBeRelocatedGlobals[i]);

                relocationInfo.module.Globals.Add(new_value);
                relocationInfo.relocatedGlobals.Add(
                    relocationInfo.toBeRelocatedGlobals[i],
                    (Operand)(relocationInfo.module.Globals.Count - 1));

                if (new_value.Type == UnitType.Table)
                    relocation_stack.Add((TableUnit)new_value.heapUnitValue);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();

            for (Operand i = 0; i < relocationInfo.toBeRelocatedData.Count; i++)
            {
                Unit new_value =
                    relocationInfo.importedVM.Data[relocationInfo.toBeRelocatedData[i]];

                relocationInfo.module.Data.Add(new_value);
                relocationInfo.relocatedData.Add(
                    relocationInfo.toBeRelocatedData[i],
                    (Operand)(relocationInfo.module.Data.Count - 1));

                if (new_value.Type == UnitType.Table)
                    relocation_stack.Add((TableUnit)new_value.heapUnitValue);
            }
            relocationInfo.toBeRelocatedData.Clear();

            foreach (TableUnit v in relocation_stack)
            {
                FindFunction(v, relocationInfo);
            }
        }

        static void RelocateChunk(FunctionUnit function, RelocationInfo relocationInfo)
        {
            for (Operand i = 0; i < function.Body.Count; i++)
            {
                Instruction next = function.Body[i];

                if (next.opCode == OpCode.LOAD_GLOBAL)
                {
                    if (next.opA >= relocationInfo.importedVM.Prelude.intrinsics.Count)// do not reimport intrinsics
                    {
                        if (relocationInfo.relocatedGlobals.ContainsKey(next.opA))
                        {
                            next.opCode = OpCode.LOAD_IMPORTED_GLOBAL;
                            next.opA = relocationInfo.relocatedGlobals[next.opA];
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else if (relocationInfo.toBeRelocatedGlobals.Contains(next.opA))
                        {
                            next.opCode = OpCode.LOAD_IMPORTED_GLOBAL;
                            next.opA =
                                (Operand)(relocationInfo.toBeRelocatedGlobals.IndexOf(next.opA) +
                                relocationInfo.relocatedGlobals.Count);
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else
                        {
                            Operand global_count =
                                (Operand)(relocationInfo.relocatedGlobals.Count +
                                relocationInfo.toBeRelocatedGlobals.Count);
                            relocationInfo.toBeRelocatedGlobals.Add(next.opA);
                            next.opCode = OpCode.LOAD_IMPORTED_GLOBAL;
                            next.opA = global_count;
                            next.opB = relocationInfo.moduleIndex;
                        }
                    }
                }
                else if (next.opCode == OpCode.ASSIGN_GLOBAL)
                {
                    if (next.opA >= relocationInfo.importedVM.Prelude.intrinsics.Count)// do not reimport intrinsics
                    {
                        next.opC = next.opB;
                        if (relocationInfo.relocatedGlobals.ContainsKey(next.opA))
                        {
                            next.opCode = OpCode.ASSIGN_IMPORTED_GLOBAL;
                            next.opA = relocationInfo.relocatedGlobals[next.opA];
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else if (relocationInfo.toBeRelocatedGlobals.Contains(next.opA))
                        {
                            next.opCode = OpCode.ASSIGN_IMPORTED_GLOBAL;
                            next.opA =
                                (Operand)(relocationInfo.toBeRelocatedGlobals.IndexOf(next.opA) +
                                relocationInfo.relocatedGlobals.Count);
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else
                        {
                            Operand global_count =
                                (Operand)(relocationInfo.relocatedGlobals.Count +
                                relocationInfo.toBeRelocatedGlobals.Count);
                            relocationInfo.toBeRelocatedGlobals.Add(next.opA);
                            next.opCode = OpCode.ASSIGN_IMPORTED_GLOBAL;
                            next.opA = global_count;
                            next.opB = relocationInfo.moduleIndex;
                        }
                    }
                }
                else if (next.opCode == OpCode.LOAD_DATA)
                {
                    if (relocationInfo.relocatedData.ContainsKey(next.opA))
                    {
                        next.opCode = OpCode.LOAD_IMPORTED_DATA;
                        next.opA = relocationInfo.relocatedData[next.opA];
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else if (relocationInfo.toBeRelocatedData.Contains(next.opA))
                    {
                        next.opCode = OpCode.LOAD_IMPORTED_DATA;
                        next.opA =
                            (Operand)(relocationInfo.toBeRelocatedData.IndexOf(next.opA) +
                            relocationInfo.relocatedData.Count);
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else
                    {
                        Operand global_count =
                            (Operand)(relocationInfo.relocatedData.Count +
                            relocationInfo.toBeRelocatedData.Count);
                        relocationInfo.toBeRelocatedData.Add(next.opA);
                        next.opCode = OpCode.LOAD_IMPORTED_DATA;
                        next.opA = global_count;
                        next.opB = relocationInfo.moduleIndex;
                    }
                }
                else if (next.opCode == OpCode.DECLARE_FUNCTION)
                {
                    Unit this_value = relocationInfo.importedVM.Data[next.opC];
                    if (relocationInfo.importingVM.Data.Contains(this_value))
                    {
                        next.opC =
                            (Operand)relocationInfo.importingVM.Data.IndexOf(this_value);
                    }
                    else
                    {
                        relocationInfo.importingVM.Data.Add(this_value);
                        next.opC = (Operand)(relocationInfo.importingVM.Data.Count - 1);
                        RelocateChunk(((ClosureUnit)this_value.heapUnitValue).Function, relocationInfo);
                    }
                }
                else if (next.opCode == OpCode.LOAD_IMPORTED_GLOBAL)
                {
                    if (relocationInfo.relocatedModules.ContainsKey(next.opB))
                    {
                        next.opB = relocationInfo.relocatedModules[next.opB];
                    }
                    else
                    {
                        bool found = false;
                        foreach (ModuleUnit v in relocationInfo.importingVM.modules)
                        {
                            if (function.Module == v.Name)
                            {
                                next.opB = v.ImportIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find LOAD_IMPORTED_GLOBAL index" + function.Module);
                    }
                }
                else if (next.opCode == OpCode.ASSIGN_IMPORTED_GLOBAL)
                {
                    if (relocationInfo.relocatedModules.ContainsKey(next.opB))
                    {
                        next.opB = relocationInfo.relocatedModules[next.opB];
                    }
                    else
                    {
                        bool found = false;
                        foreach (ModuleUnit v in relocationInfo.importingVM.modules)
                        {
                            if (function.Module == v.Name)
                            {
                                next.opB = v.ImportIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find ASSIGN_IMPORTED_GLOBAL index" + function.Module);
                    }
                }
                else if (next.opCode == OpCode.LOAD_IMPORTED_DATA)
                {
                    if (relocationInfo.relocatedModules.ContainsKey(next.opB))
                    {
                        next.opB = relocationInfo.relocatedModules[next.opB];
                    }
                    else
                    {
                        bool found = false;
                        foreach (ModuleUnit v in relocationInfo.importingVM.modules)
                        {
                            if (function.Module == v.Name)
                            {
                                next.opB = v.ImportIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find LOAD_IMPORTED_DATA index" + function.Module);
                    }
                }

                function.Body[i] = next;
            }
        }
        static void ImportModule(ModuleUnit module, Operand new_index)
        {
            foreach (KeyValuePair<Unit, Unit> entry in module.Table)
            {
                if (entry.Value.Type == UnitType.Function)
                {
                    FunctionUnit function = (FunctionUnit)entry.Value.heapUnitValue;
                    for (Operand i = 0; i < function.Body.Count; i++)
                    {
                        Instruction next = function.Body[i];

                        if ((next.opCode == OpCode.LOAD_IMPORTED_GLOBAL) ||
                            (next.opCode == OpCode.LOAD_IMPORTED_DATA) ||
                            (next.opCode == OpCode.ASSIGN_IMPORTED_GLOBAL))
                        {
                            next.opB = new_index;
                            function.Body[i] = next;
                        }
                    }
                }
            }
        }
    }

    internal class CSharpScriptExecution
    {
        public CSharpScriptExecution()
        {
        }

        public bool SaveGeneratedCode { get; set; }
    }
}
