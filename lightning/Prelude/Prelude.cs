using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Operand = System.UInt16;

#if ROSLYN
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
#endif

#if DOUBLE
    using Number = System.Double;
#else
    using Number = System.Single;
#endif

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

        public static void AddLibraries(Library lib1, Library lib2)
        {
            foreach (IntrinsicUnit i in lib2.intrinsics)
            {
                lib1.intrinsics.Add(i);
            }
            foreach (KeyValuePair<string, TableUnit> entry in lib2.tables)
            {
                lib1.tables.Add(entry.Key, entry.Value);
            }
        }

        public static Library GetPrelude()
        {
            Dictionary<string, TableUnit> tables = new Dictionary<string, TableUnit>();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////// machine
            {
                TableUnit machine = new TableUnit(null, null);

                Unit memoryUse(VM vm){
                    TableUnit mem_use = new TableUnit(null, null);
                    mem_use.TableSet("stack_count", vm.StackCount());
                    mem_use.TableSet("globals_count", vm.GlobalsCount());
                    mem_use.TableSet("variables_count", vm.VariablesCount());
                    mem_use.TableSet("variables_capacity", vm.VariablesCapacity());
                    mem_use.TableSet("upvalues_count", vm.UpValuesCount());
                    mem_use.TableSet("upvalue_capacity", vm.UpValueCapacity());
                    mem_use.TableSet("number_pool_count", vm.numberPool.Count);
                    mem_use.TableSet("number_pool_max_used", vm.numberPool.MaxUsed);
                    mem_use.TableSet("number_pool_in_use", vm.numberPool.InUse);

                    return mem_use;
                }
                machine.TableSet("memory_use", new IntrinsicUnit("memory_use", memoryUse, 0));

                tables.Add("machine", machine);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////// intrinsic
            {
                TableUnit intrinsic = new TableUnit(null, null);
#if ROSLYN
                Unit createIntrinsic(VM vm)
                {
                    string name = vm.GetString(0);
                    Number arity = vm.GetNumber(1);
                    string body = vm.GetString(2);

                    var options = ScriptOptions.Default.AddReferences(
                        typeof(Unit).Assembly,
                        typeof(VM).Assembly).WithImports("lightning", "System");
                    Func<VM, Unit> new_intrinsic = CSharpScript.EvaluateAsync<Func<VM, Unit>>(body, options)
                        .GetAwaiter().GetResult();

                    return new IntrinsicUnit(name, new_intrinsic, (int)arity);
                }
                intrinsic.TableSet("create", new IntrinsicUnit("create", createIntrinsic, 3));
#else
                intrinsic.TableSet("create", Unit.Null);
#endif
                tables.Add("intrinsic", intrinsic);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// rand

            {
                TableUnit rand = new TableUnit(null, null);

                var rng = new Random();

                Unit nextInt(VM vm)
                {
                    int max = (int)(vm.GetNumber(0));
                    return vm.numberPool.Get(rng.Next(max + 1));
                }
                rand.TableSet("int", new IntrinsicUnit("int", nextInt, 1));

                Unit nextFloat(VM vm)
                {
                    return vm.numberPool.Get((Number)rng.NextDouble());
                }
                rand.TableSet("float", new IntrinsicUnit("float", nextFloat, 0));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// list

            {
                TableUnit list = new TableUnit(null, null);

                Unit listPush(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Unit value = vm.GetUnit(1);
                    list.elements.Add(value);

                    return Unit.Null;
                }
                list.TableSet("push", new IntrinsicUnit("push", listPush, 2));

                //////////////////////////////////////////////////////
                Unit listPop(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Number value = ((NumberUnit)list.elements[^1]).content;
                    list.elements.RemoveRange(list.elements.Count - 1, 1);

                    return vm.numberPool.Get(value);
                }
                list.TableSet("pop", new IntrinsicUnit("pop", listPop, 1));

                //////////////////////////////////////////////////////
                Unit listToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    bool first = true;
                    string value = "";
                    foreach (Unit v in this_table.elements)
                    {
                        if (first)
                        {
                            value += System.Text.RegularExpressions.Regex.Unescape(v.ToString());
                            first = false;
                        }
                        else
                        {
                            value += ", " + System.Text.RegularExpressions.Regex.Unescape(v.ToString());
                        }
                    }
                    return new StringUnit(value);
                }
                list.TableSet("to_string", new IntrinsicUnit("to_string", listToString, 1));

                ////////////////////////////////////////////////////
                Unit listCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.ECount;
                    return vm.numberPool.Get(count);
                }
                list.TableSet("count", new IntrinsicUnit("count", listCount, 1));

                //////////////////////////////////////////////////////
                Unit listClear(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.elements.Clear();
                    return Unit.Null;
                }
                list.TableSet("clear", new IntrinsicUnit("clear", listClear, 1));

                //////////////////////////////////////////////////////
                Unit listRemoveRange(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);

                    return Unit.Null;
                }
                list.TableSet("remove", new IntrinsicUnit("remove", listRemoveRange, 3));

                //////////////////////////////////////////////////////
                Unit listCopy(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in list.elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new_list;
                }
                list.TableSet("copy", new IntrinsicUnit("copy", listCopy, 1));

                //////////////////////////////////////////////////////
                Unit listSplit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    List<Unit> new_list_elements = list.elements.GetRange(range_init, list.elements.Count - range_init);
                    list.elements.RemoveRange(range_init, list.elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new_list;
                }
                list.TableSet("split", new IntrinsicUnit("split", listSplit, 2));

                //////////////////////////////////////////////////////
                Unit listSlice(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);

                    List<Unit> new_list_elements = list.elements.GetRange(range_init, range_end - range_init + 1);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new_list;
                }
                list.TableSet("slice", new IntrinsicUnit("slice", listSlice, 3));
                //////////////////////////////////////////////////////
                Unit listReverse(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.elements.Reverse();

                    return Unit.Null;
                }
                list.TableSet("reverse", new IntrinsicUnit("reverse", listReverse, 1));

                //////////////////////////////////////////////////////

                Unit makeIndexesIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;
                    StringUnit value_string = new StringUnit("value");
                    StringUnit key_string = new StringUnit("key");

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.table[key_string] = vm.numberPool.Get(i);
                            iterator.table[value_string] = this_table.elements[i];
                            return Unit.True;
                        }
                        return Unit.False;
                    };
                    iterator.TableSet("next", new IntrinsicUnit("iterator_next", next, 0));
                    return iterator;
                }
                list.TableSet("index_iterator", new IntrinsicUnit("list_index_iterator", makeIndexesIterator, 1));

                //////////////////////////////////////////////////////

                Unit makeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;
                    Unit value = Unit.Null;
                    StringUnit value_string = new StringUnit("value");

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.table[value_string] = this_table.elements[i];
                            return Unit.True;
                        }
                        return Unit.False;
                    };
                    iterator.TableSet("next", new IntrinsicUnit("iterator_next", next, 0));
                    return iterator;
                }
                list.TableSet("iterator", new IntrinsicUnit("list_iterator", makeIterator, 1));

                //////////////////////////////////////////////////////

                tables.Add("list", list);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// table

            {
                TableUnit table = new TableUnit(null, null);

                //////////////////////////////////////////////////////
                Unit tableCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.TCount;
                    return vm.numberPool.Get(count);
                }
                table.TableSet("count", new IntrinsicUnit("table_count", tableCount, 1));

                //////////////////////////////////////////////////////
                Unit tableIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (StringUnit v in this_table.table.Keys)
                    {
                        indexes.elements.Add(v);
                    }

                    return indexes;
                }
                table.TableSet("indexes", new IntrinsicUnit("indexes", tableIndexes, 1));

                //////////////////////////////////////////////////////
                Unit tableCopy(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<StringUnit, Unit> table_copy = new Dictionary<StringUnit, Unit>();
                    foreach (KeyValuePair<StringUnit, Unit> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    TableUnit copy = new TableUnit(null, table_copy);

                    return copy;
                }
                table.TableSet("copy", new IntrinsicUnit("copy", tableCopy, 1));

                //////////////////////////////////////////////////////
                Unit tableClear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    return Unit.Null;
                }
                table.TableSet("clear", new IntrinsicUnit("clear", tableClear, 1));

                Unit makeIteratorTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.table.GetEnumerator();

                    StringUnit value_string = new StringUnit("value");
                    StringUnit key_string = new StringUnit("key");
                    TableUnit iterator = new TableUnit(null, null);
                    iterator.table[key_string] = Unit.Null;
                    iterator.table[value_string] = Unit.Null;

                    Unit next(VM vm)
                    {
                        if (enumerator.MoveNext())
                        {
                            iterator.table[key_string] = (StringUnit)enumerator.Key;
                            iterator.table[value_string] = (Unit)enumerator.Value;
                            return Unit.True;
                        }
                        return Unit.False;
                    };

                    iterator.TableSet("next", new IntrinsicUnit("table_iterator_next", next, 0));
                    return iterator;
                }
                table.TableSet("iterator", new IntrinsicUnit("iterator_table", makeIteratorTable, 1));

                //////////////////////////////////////////////////////
                Unit tableToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    string value = "";
                    bool first = true;
                    foreach (KeyValuePair<StringUnit, Unit> entry in this_table.table)
                    {
                        if (first)
                        {
                            value +=
                                System.Text.RegularExpressions.Regex.Unescape(entry.Key.ToString())
                                + " : " + System.Text.RegularExpressions.Regex.Unescape(entry.Value.ToString());
                            first = false;
                        }
                        else
                        {
                            value +=
                                ", "
                                + System.Text.RegularExpressions.Regex.Unescape(entry.Key.ToString())
                                + " : " + System.Text.RegularExpressions.Regex.Unescape(entry.Value.ToString());
                        }
                    }
                    return new StringUnit(value);
                }
                table.TableSet("to_string", new IntrinsicUnit("table_to_string", tableToString, 1));

                tables.Add("table", table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                TableUnit math = new TableUnit(null, null);
                math.TableSet("pi", (Number)Math.PI);
                math.TableSet("e", (Number)Math.E);
#if DOUBLE
                math.TableSet("double", Unit.True);
#else
                math.TableSet("double", Unit.False);
#endif

                //////////////////////////////////////////////////////
                Unit sin(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Sin(vm.GetNumber(0)));
                }
                math.TableSet("sin", new IntrinsicUnit("sin", sin, 1));

                //////////////////////////////////////////////////////
                Unit cos(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Cos(vm.GetNumber(0)));
                }
                math.TableSet("cos", new IntrinsicUnit("cos", cos, 1));

                //////////////////////////////////////////////////////
                Unit tan(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Tan(vm.GetNumber(0)));
                }
                math.TableSet("tan", new IntrinsicUnit("tan", tan, 1));

                //////////////////////////////////////////////////////
                Unit sec(VM vm)
                {
                    return vm.numberPool.Get((Number)(1 / Math.Cos(vm.GetNumber(0))));
                }
                math.TableSet("sec", new IntrinsicUnit("sec", sec, 1));

                //////////////////////////////////////////////////////
                Unit cosec(VM vm)
                {
                    return vm.numberPool.Get((Number)(1 / Math.Sin(vm.GetNumber(0))));
                }
                math.TableSet("cosec", new IntrinsicUnit("cosec", cosec, 1));

                //////////////////////////////////////////////////////
                Unit cotan(VM vm)
                {
                    return vm.numberPool.Get((Number)(1 / Math.Tan(vm.GetNumber(0))));
                }
                math.TableSet("cotan", new IntrinsicUnit("cotan", cotan, 1));

                //////////////////////////////////////////////////////
                Unit asin(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Asin(vm.GetNumber(0)));
                }
                math.TableSet("asin", new IntrinsicUnit("asin", asin, 1));

                //////////////////////////////////////////////////////
                Unit acos(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Acos(vm.GetNumber(0)));
                }
                math.TableSet("acos", new IntrinsicUnit("acos", acos, 1));

                //////////////////////////////////////////////////////
                Unit atan(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Atan(vm.GetNumber(0)));
                }
                math.TableSet("atan", new IntrinsicUnit("atan", atan, 1));

                //////////////////////////////////////////////////////
                Unit sinh(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Sinh(vm.GetNumber(0)));
                }
                math.TableSet("sinh", new IntrinsicUnit("sinh", sinh, 1));

                //////////////////////////////////////////////////////
                Unit cosh(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Cosh(vm.GetNumber(0)));
                }
                math.TableSet("cosh", new IntrinsicUnit("cosh", cosh, 1));

                //////////////////////////////////////////////////////
                Unit tanh(VM vm)
                {
                    return vm.numberPool.Get((Number)Math.Tanh(vm.GetNumber(0)));
                }
                math.TableSet("tanh", new IntrinsicUnit("tanh", tanh, 1));

                //////////////////////////////////////////////////////
                Unit pow(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    Number exponent = vm.GetNumber(1);
                    return vm.numberPool.Get((Number)Math.Pow(value, exponent));
                }
                math.TableSet("pow", new IntrinsicUnit("pow", pow, 2));

                //////////////////////////////////////////////////////
                Unit root(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    Number exponent = vm.GetNumber(1);
                    return vm.numberPool.Get((Number)Math.Pow(value, 1 / exponent));
                }
                math.TableSet("root", new IntrinsicUnit("root", root, 2));

                //////////////////////////////////////////////////////
                Unit sqroot(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    return vm.numberPool.Get((Number)Math.Sqrt(value));
                }
                math.TableSet("sqroot", new IntrinsicUnit("sqroot", sqroot, 1));
                //////////////////////////////////////////////////////
                Unit exp(VM vm)
                {
                    Number exponent = vm.GetNumber(0);
                    return vm.numberPool.Get((Number)Math.Exp(exponent));
                }
                math.TableSet("exp", new IntrinsicUnit("exp", exp, 1));

                //////////////////////////////////////////////////////
                Unit log(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    Number this_base = vm.GetNumber(1);
                    return vm.numberPool.Get((Number)Math.Log(value, this_base));
                }
                math.TableSet("log", new IntrinsicUnit("log", log, 2));

                //////////////////////////////////////////////////////
                Unit ln(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    return vm.numberPool.Get((Number)Math.Log(value, Math.E));
                }
                math.TableSet("ln", new IntrinsicUnit("ln", ln, 1));

                //////////////////////////////////////////////////////
                Unit log10(VM vm)
                {
                    Number value = vm.GetNumber(0);
                    return vm.numberPool.Get((Number)Math.Log(value, (Number)10));
                }
                math.TableSet("log10", new IntrinsicUnit("log10", log10, 1));

                //////////////////////////////////////////////////////
                Unit mod(VM vm)
                {
                    Number value1 = vm.GetNumber(0);
                    Number value2 = vm.GetNumber(1);
                    return vm.numberPool.Get((Number)value1 % value2);
                }
                math.TableSet("mod", new IntrinsicUnit("mod", mod, 2));

                //////////////////////////////////////////////////////
                Unit idiv(VM vm)
                {
                    Number value1 = vm.GetNumber(0);
                    Number value2 = vm.GetNumber(1);
                    return vm.numberPool.Get((Number)(int)(value1 / value2));
                }
                math.TableSet("idiv", new IntrinsicUnit("idiv", idiv, 2));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                TableUnit time = new TableUnit(null, null);

                Unit now(VM vm)
                {
                    return new WrapperUnit<long>(DateTime.Now.Ticks);
                }
                time.TableSet("now", new IntrinsicUnit("now", now, 0));

                Unit timeSpan(VM vm)
                {
                    long timeStart = vm.GetWrapperUnit<long>(0);
                    long timeEnd = vm.GetWrapperUnit<long>(1);
                    return vm.numberPool.Get((Number)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                time.TableSet("span", new IntrinsicUnit("span", timeSpan, 2));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// string
            {
                TableUnit string_table = new TableUnit(null, null);

                Unit stringSlice(VM vm)
                {
                    string input_string = vm.GetString(0);
                    Number start = vm.GetNumber(1);
                    Number end = vm.GetNumber(2);

                    if (end < input_string.Length)
                    {
                        string result = input_string.Substring((int)start, (int)(end - start));
                        return new StringUnit(result);
                    }
                    return Unit.Null;
                }
                string_table.TableSet("slice", new IntrinsicUnit("string_slice", stringSlice, 3));

                //////////////////////////////////////////////////////

                Unit stringSplit(VM vm)
                {
                    StringUnit val_input_string = vm.GetStringUnit(0);
                    string input_string = val_input_string.ToString();
                    Number start = vm.GetNumber(1);
                    if (start < input_string.Length)
                    {
                        Number end = input_string.Length;
                        string result = input_string.Substring((int)start, (int)(end - start));
                        val_input_string.content = input_string.Substring(0, (int)start);
                        return new StringUnit(result);
                    }
                    return Unit.Null;
                }
                string_table.TableSet("split", new IntrinsicUnit("string_split", stringSplit, 2));

                //////////////////////////////////////////////////////

                Unit stringLength(VM vm)
                {
                    StringUnit val_input_string = vm.GetStringUnit(0);
                    return vm.numberPool.Get(val_input_string.ToString().Length);
                }
                string_table.TableSet("length", new IntrinsicUnit("string_length", stringLength, 1));

                //////////////////////////////////////////////////////

                Unit stringCopy(VM vm)
                {
                    Unit val_input_string = vm.GetUnit(0);
                    if (val_input_string.GetType() == typeof(StringUnit))
                        return new StringUnit(val_input_string.ToString());
                    else
                        return Unit.Null;
                }
                string_table.TableSet("copy", new IntrinsicUnit("string_copy", stringCopy, 1));

                //////////////////////////////////////////////////////

                tables.Add("string", string_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null, null);
                Unit charAt(VM vm)
                {
                    string input_string = vm.GetString(0);
                    Number index = vm.GetNumber(1);
                    if (index < input_string.Length)
                    {
                        char result = input_string[(int)index];
                        return new StringUnit(result.ToString());
                    }
                    return Unit.Null;
                }
                char_table.TableSet("at", new IntrinsicUnit("char_at", charAt, 2));

                //////////////////////////////////////////////////////

                Unit isAlpha(VM vm)
                {
                    string input_string = vm.GetString(0);
                    if (1 <= input_string.Length)
                    {
                        char head = input_string[0];
                        if (Char.IsLetter(head))
                        {
                            return Unit.True;
                        }
                    }
                    return Unit.False;
                }
                char_table.TableSet("is_alpha", new IntrinsicUnit("is_alpha", isAlpha, 1));

                //////////////////////////////////////////////////////

                Unit isDigit(VM vm)
                {
                    string input_string = vm.GetString(0);
                    if (1 <= input_string.Length)
                    {
                        char head = input_string[0];
                        if (Char.IsDigit(head))
                        {
                            return Unit.True;
                        }
                        return Unit.False;
                    }
                    return Unit.Null;
                }
                char_table.TableSet("is_digit", new IntrinsicUnit("is_digit", isDigit, 1));

                tables.Add("char", char_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// file
            {
                TableUnit file = new TableUnit(null, null);
                Unit loadFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string input;
                    using (var sr = new StreamReader(path))
                    {
                        input = sr.ReadToEnd();
                    }
                    if (input != null)
                        return new StringUnit(input);

                    return Unit.Null;
                }
                file.TableSet("load", new IntrinsicUnit("load_file", loadFile, 1));

                //////////////////////////////////////////////////////
                Unit writeFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return Unit.Null;
                }
                file.TableSet("write", new IntrinsicUnit("write_file", writeFile, 2));

                //////////////////////////////////////////////////////
                Unit appendFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }

                    return Unit.Null;
                }
                file.TableSet("append", new IntrinsicUnit("append_file", appendFile, 2));

                tables.Add("file", file);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////// Global Intrinsics

            List<IntrinsicUnit> functions = new List<IntrinsicUnit>();

            //////////////////////////////////////////////////////
            Unit eval(VM vm)
            {
                string eval_code = vm.GetString(0);;
                Scanner scanner = new Scanner(eval_code);

                Parser parser = new Parser(scanner.Tokens);
                if (parser.Errors.Count > 0)
                {
                    Console.WriteLine("Parsing had errors:");
                    foreach (string error in parser.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return Unit.Null;
                }

                Node program = parser.ParsedTree;

                string eval_name = eval_code.GetHashCode().ToString();
                Chunker code_generator = new Chunker(program, eval_name, vm.GetChunk().Prelude);
                Chunk chunk = code_generator.Code;
                if (code_generator.Errors.Count > 0)
                {
                    Console.WriteLine("Code generation had errors:");
                    foreach (string error in code_generator.Errors)
                        Console.WriteLine(error);
                    return Unit.Null;
                }
                if (code_generator.HasChunked == true)
                {
                    VM imported_vm = new VM(chunk);
                    VMResult result = imported_vm.Run();
                    if (result.status == VMResultType.OK)
                    {
                        if (result.value.GetType() == typeof(TableUnit) || result.value.GetType() == typeof(FunctionUnit))
                            MakeModule(result.value, eval_name, vm, imported_vm);
                        return result.value;
                    }
                }
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("eval", eval, 1));

            ////////////////////////////////////////////////////
            Unit require(VM vm)
            {
                string path = vm.GetString(0);
                foreach (ModuleUnit v in vm.modules)// skip already imported modules
                {
                    if (v.name == path)
                        return v;
                }
                string module_code;
                using (var sr = new StreamReader(path))
                {
                    module_code = sr.ReadToEnd();
                }
                if (module_code != null)
                {

                    Scanner scanner = new Scanner(module_code);

                    Parser parser = new Parser(scanner.Tokens);
                    if (parser.Errors.Count > 0)
                    {
                        Console.WriteLine("Parsing had errors:");
                        foreach (string error in parser.Errors)
                        {
                            Console.WriteLine(error);
                        }
                        return Unit.Null;
                    }

                    Node program = parser.ParsedTree;

                    Chunker code_generator = new Chunker(program, path, vm.GetChunk().Prelude);
                    Chunk chunk = code_generator.Code;
                    if (code_generator.Errors.Count > 0)
                    {
                        Console.WriteLine("Code generation had errors:");
                        foreach (string error in code_generator.Errors)
                            Console.WriteLine(error);
                        return Unit.Null;
                    }

                    if (code_generator.HasChunked == true)
                    {
                        VM imported_vm = new VM(chunk);
                        VMResult result = imported_vm.Run();
                        if (result.status == VMResultType.OK)
                        {
                            MakeModule(result.value, path, vm, imported_vm);
                            //vm.GetChunk().Print();
                            return result.value;
                        }
                    }
                }
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("require", require, 1));

            //////////////////////////////////////////////////////
            Unit modules(VM vm)
            {
                string modules = "";
                bool first = true;
                foreach (KeyValuePair<string, int> entry in vm.loadedModules)
                {
                    if (first == true)
                    {
                        modules += entry.Key;
                        first = false;
                    }
                    else
                    {
                        modules += " " + entry.Key;
                    }
                }

                return new StringUnit(modules);
            }
            functions.Add(new IntrinsicUnit("modules", modules, 0));

            //////////////////////////////////////////////////////

            Unit writeLine(VM vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Unescape(vm.GetUnit(0).ToString()));
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("write_line", writeLine, 1));

            //////////////////////////////////////////////////////
            Unit write(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Unescape(vm.GetUnit(0).ToString()));
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("write", write, 1));

            //////////////////////////////////////////////////////
            Unit readln(VM vm)
            {
                string read = Console.ReadLine();
                return new StringUnit(read);
            }
            functions.Add(new IntrinsicUnit("read_line", readln, 0));

            //////////////////////////////////////////////////////
            Unit readNumber(VM vm)
            {
                string read = Console.ReadLine();
                if (Number.TryParse(read, out Number n))
                    return vm.numberPool.Get(n);
                else
                    return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("read_number", readNumber, 0));

            //////////////////////////////////////////////////////
            Unit read(VM vm)
            {
                int read = Console.Read();
                if (read > 0)
                {
                    char next = Convert.ToChar(read);
                    if (next == '\n')
                        return Unit.Null;
                    else
                        return new StringUnit(Char.ToString(next));
                }
                else
                    return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("read", read, 0));

            //////////////////////////////////////////////////////
            Unit count(VM vm)
            {
                TableUnit this_table = vm.GetTable(0);
                int count = this_table.Count;
                return vm.numberPool.Get(count);
            }
            functions.Add(new IntrinsicUnit("count_all", count, 1));

            //////////////////////////////////////////////////////
            Unit type(VM vm)
            {
                Type this_type = vm.GetUnit(0).GetType();
                return new StringUnit(this_type.ToString());
            }
            functions.Add(new IntrinsicUnit("type", type, 1));

            //////////////////////////////////////////////////////
            Unit maybe(VM vm)
            {
                Unit first = vm.stack.Peek(0);
                Unit second = vm.stack.Peek(1);
                if (first != Unit.Null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", maybe, 2));

            //////////////////////////////////////////////////////
            Unit resourcesTrim(VM vm)
            {
                vm.ResoursesTrim();
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("trim", resourcesTrim, 0));

            //////////////////////////////////////////////////////
            Unit releaseAllVMs(VM vm)
            {
                VM.ReleaseVMs();
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("release_all_vms", releaseAllVMs, 0));

            //////////////////////////////////////////////////////
            Unit releaseVMs(VM vm)
            {
                VM.ReleaseVMs((int)vm.GetNumber(0));
                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("release_vms", releaseVMs, 1));

            //////////////////////////////////////////////////////
            Unit countVMs(VM vm)
            {
                return vm.numberPool.Get(VM.CountVMs());
            }
            functions.Add(new IntrinsicUnit("count_vms", countVMs, 0));

            //////////////////////////////////////////////////////
            Unit forEach(VM vm)
            {
                TableUnit table = vm.GetTable(0);
                Unit func = vm.GetUnit(1);

                int init = 0;
                int end = table.ECount;
                VM[] vms = new VM[end];
                for (int i = init; i < end; i++)
                {
                    vms[i] = vm.GetVM();
                }
                System.Threading.Tasks.Parallel.For(init, end, (index) =>
                {
                    List<Unit> args = new List<Unit>();
                    args.Add(vm.numberPool.Get(index));
                    args.Add(table);
                    vms[index].CallFunction(func, args);
                });
                for (int i = init; i < end; i++)
                {
                    VM.RecycleVM(vms[i]);
                }
                return Unit.Null;
            }

            functions.Add(new IntrinsicUnit("foreach", forEach, 2));
            //////////////////////////////////////////////////////

            Unit ForRange(VM vm)
            {
                Number tasks = vm.GetNumber(0);
                TableUnit table = vm.GetTable(1);
                Unit func = vm.GetUnit(2);

                int n_tasks = (int)tasks;

                int init = 0;
                int end = n_tasks;
                VM[] vms = new VM[end];
                for (int i = 0; i < end; i++)
                {
                    vms[i] = vm.GetVM();
                }

                int count = table.ECount;
                int step = (count / n_tasks) + 1;

                System.Threading.Tasks.Parallel.For(init, end, (index) =>
                {
                    List<Unit> args = new List<Unit>();
                    int range_start = index * step;
                    args.Add(vm.numberPool.Get(range_start));
                    int range_end = range_start + step;
                    if (range_end > count) range_end = count;
                    args.Add(vm.numberPool.Get(range_end));
                    args.Add(table);
                    vms[index].CallFunction(func, args);
                });
                for (int i = 0; i < end; i++)
                {
                    VM.RecycleVM(vms[i]);
                }
                return Unit.Null;
            }

            functions.Add(new IntrinsicUnit("range", ForRange, 3));

            //////////////////////////////////////////////////////
            Unit tuple(VM vm)
            {
                Unit[] tuple = new Unit[2];
                tuple[0] = vm.GetUnit(0);
                tuple[1] = vm.GetUnit(1);

                return new WrapperUnit<Unit[]>(tuple);
            }
            functions.Add(new IntrinsicUnit("tuple", tuple, 2));

            //////////////////////////////////////////////////////
            Unit getTupleX(VM vm)
            {
                Unit x = vm.GetWrapperUnit<Unit[]>(0)[0];

                return x;
            }
            functions.Add(new IntrinsicUnit("tuple_get_x", getTupleX, 1));

            //////////////////////////////////////////////////////
            Unit getTupleY(VM vm)
            {
                Unit y = vm.GetWrapperUnit<Unit[]>(0)[1];

                return y;
            }
            functions.Add(new IntrinsicUnit("tuple_get_y", getTupleY, 1));

            //////////////////////////////////////////////////////
            Unit setTupleX(VM vm)
            {
                vm.GetWrapperUnit<Unit[]>(0)[0] = vm.GetUnit(1);

                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("tuple_set_x", setTupleX, 2));

            //////////////////////////////////////////////////////
            Unit setTupleY(VM vm)
            {
                vm.GetWrapperUnit<Unit[]>(0)[1] = vm.GetUnit(1);

                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("tuple_set_y", setTupleY, 2));

            //////////////////////////////////////////////////////
            Unit nuple(VM vm)
            {
                TableUnit table = vm.GetTable(0);
                int size = table.ECount;
                Unit[] nuple = new Unit[size];
                for(int i = 0; i < size; i++)
                    nuple[i] = table.elements[i];

                return new WrapperUnit<Unit[]>(nuple);
            }
            functions.Add(new IntrinsicUnit("nuple", nuple, 1));

            //////////////////////////////////////////////////////
            Unit getNuple(VM vm)
            {
                Unit x = vm.GetWrapperUnit<Unit[]>(0)[(int)vm.GetNumber(1)];

                return x;
            }
            functions.Add(new IntrinsicUnit("nuple_get", getNuple, 2));

            //////////////////////////////////////////////////////
            Unit setNuple(VM vm)
            {
                vm.GetWrapperUnit<Unit[]>(0)[(int)vm.GetNumber(1)] = vm.GetUnit(2);

                return Unit.Null;
            }
            functions.Add(new IntrinsicUnit("nuple_set", setNuple, 3));

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
            public Dictionary<Operand, Operand> relocatedConstants;
            public List<Operand> toBeRelocatedConstants;
            public List<int> relocatedTables;
            public Dictionary<Operand, Operand> relocatedModules;
            public ModuleUnit module;
            public Operand moduleIndex;
            public RelocationInfo(
                VM p_importingVM,
                VM p_importedVM,
                Dictionary<Operand, Operand> p_relocatedGlobals,
                List<Operand> p_toBeRelocatedGlobals,
                Dictionary<Operand, Operand> p_relocatedConstants,
                List<Operand> p_toBeRelocatedConstants,
                List<int> p_relocatedTables,
                Dictionary<Operand, Operand> p_relocatedModules,
                ModuleUnit p_module,
                Operand p_moduleIndex)
            {
                importingVM = p_importingVM;
                importedVM = p_importedVM;
                relocatedGlobals = p_relocatedGlobals;
                toBeRelocatedGlobals = p_toBeRelocatedGlobals;
                relocatedConstants = p_relocatedConstants;
                toBeRelocatedConstants = p_toBeRelocatedConstants;
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
                Operand old_module_index = m.importIndex;
                Operand copied_module_index;
                if (!importing_vm.modules.Contains(m))
                {
                    copied_module_index = importing_vm.AddModule(m);
                }
                else
                    copied_module_index = (Operand)importing_vm.modules.IndexOf(m);

                m.importIndex = copied_module_index;// ready for next import
                relocated_modules.Add(old_module_index, copied_module_index);
                ImportModule(m, (Operand)copied_module_index);
            }

            ModuleUnit module = new ModuleUnit(name, null, null, null, null);
            Operand module_index = importing_vm.AddModule(module);
            module.importIndex = module_index;
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

            if (this_value.GetType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)this_value, relocationInfo);
            else if (this_value.GetType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)this_value, relocationInfo);
            else if (this_value.GetType() == typeof(TableUnit)) FindFunction((TableUnit)this_value, relocationInfo);

            return module;
        }

        static void FindFunction(TableUnit table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<StringUnit, Unit> entry in table.table)
                {
                    if (entry.Value.GetType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)entry.Value, relocationInfo);
                    else if (entry.Value.GetType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)entry.Value, relocationInfo);
                    else if (entry.Value.GetType() == typeof(TableUnit)) FindFunction((TableUnit)entry.Value, relocationInfo);

                    relocationInfo.module.TableSet(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ClosureUnit closure, RelocationInfo relocationInfo)
        {
            foreach (UpValueUnit v in closure.upValues)
            {
                if (v.UpValue.GetType() == typeof(ClosureUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateClosure((ClosureUnit)v.UpValue, relocationInfo);
                }
                else if (v.UpValue.GetType() == typeof(FunctionUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateFunction((FunctionUnit)v.UpValue, relocationInfo);
                }
            }

            RelocateFunction(closure.function, relocationInfo);
        }

        static void RelocateFunction(FunctionUnit function, RelocationInfo relocationInfo)
        {
            List<TableUnit> relocation_stack = new List<TableUnit>();
            RelocateChunk(function, relocationInfo);

            for (Operand i = 0; i < relocationInfo.toBeRelocatedGlobals.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetGlobal(relocationInfo.toBeRelocatedGlobals[i]);

                relocationInfo.module.globals.Add(new_value);
                relocationInfo.relocatedGlobals.Add(relocationInfo.toBeRelocatedGlobals[i], (Operand)(relocationInfo.module.globals.Count - 1));

                if (new_value.GetType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();

            for (Operand i = 0; i < relocationInfo.toBeRelocatedConstants.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetChunk().GetConstant(relocationInfo.toBeRelocatedConstants[i]);

                relocationInfo.module.constants.Add(new_value);
                relocationInfo.relocatedConstants.Add(relocationInfo.toBeRelocatedConstants[i], (Operand)(relocationInfo.module.constants.Count - 1));

                if (new_value.GetType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value);
            }
            relocationInfo.toBeRelocatedConstants.Clear();

            foreach (TableUnit v in relocation_stack)
            {
                FindFunction(v, relocationInfo);
            }
        }

        static void RelocateChunk(FunctionUnit function, RelocationInfo relocationInfo)
        {
            for (Operand i = 0; i < function.body.Count; i++)
            {
                Instruction next = function.body[i];

                if (next.opCode == OpCode.LOAD_GLOBAL)
                {
                    if ((next.opCode == OpCode.LOAD_GLOBAL && next.opA >= relocationInfo.importedVM.GetChunk().Prelude.intrinsics.Count))
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
                            next.opA = (Operand)(relocationInfo.toBeRelocatedGlobals.IndexOf(next.opA) + relocationInfo.relocatedGlobals.Count);
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else
                        {
                            Operand global_count = (Operand)(relocationInfo.relocatedGlobals.Count + relocationInfo.toBeRelocatedGlobals.Count);
                            relocationInfo.toBeRelocatedGlobals.Add(next.opA);
                            next.opCode = OpCode.LOAD_IMPORTED_GLOBAL;
                            next.opA = global_count;
                            next.opB = relocationInfo.moduleIndex;
                        }
                    }
                }
                else if (next.opCode == OpCode.LOAD_CONSTANT)
                {
                    if (relocationInfo.relocatedConstants.ContainsKey(next.opA))
                    {
                        next.opCode = OpCode.LOAD_IMPORTED_CONSTANT;
                        next.opA = relocationInfo.relocatedConstants[next.opA];
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else if (relocationInfo.toBeRelocatedConstants.Contains(next.opA))
                    {
                        next.opCode = OpCode.LOAD_IMPORTED_CONSTANT;
                        next.opA = (Operand)(relocationInfo.toBeRelocatedConstants.IndexOf(next.opA) + relocationInfo.relocatedConstants.Count);
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else
                    {
                        Operand global_count = (Operand)(relocationInfo.relocatedConstants.Count + relocationInfo.toBeRelocatedConstants.Count);
                        relocationInfo.toBeRelocatedConstants.Add(next.opA);
                        next.opCode = OpCode.LOAD_IMPORTED_CONSTANT;
                        next.opA = global_count;
                        next.opB = relocationInfo.moduleIndex;
                    }
                }
                else if (next.opCode == OpCode.DECLARE_FUNCTION)
                {
                    Unit this_value = relocationInfo.importedVM.GetChunk().GetConstant(next.opC);
                    if (relocationInfo.importingVM.GetChunk().GetConstants().Contains(this_value))
                    {
                        next.opC = (Operand)relocationInfo.importingVM.GetChunk().GetConstants().IndexOf(this_value);
                    }
                    else
                    {
                        relocationInfo.importingVM.GetChunk().GetConstants().Add(this_value);
                        next.opC = (Operand)(relocationInfo.importingVM.GetChunk().GetConstants().Count - 1);
                        RelocateChunk(((ClosureUnit)this_value).function, relocationInfo);
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
                            if (function.module == v.name)
                            {
                                next.opB = v.importIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find LOAD_IMPORTED_GLOBAL index" + function.module);
                    }
                }
                else if (next.opCode == OpCode.LOAD_IMPORTED_CONSTANT)
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
                            if (function.module == v.name)
                            {
                                next.opB = v.importIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find LOAD_IMPORTED_CONSTANT index" + function.module);
                    }
                }

                //Chunk.PrintInstruction(next);
                //Console.WriteLine();
                function.body[i] = next;
            }
        }
        static void ImportModule(ModuleUnit module, Operand new_index)
        {
            foreach (KeyValuePair<StringUnit, Unit> entry in module.table)
            {
                if (entry.Value.GetType() == typeof(FunctionUnit))
                {
                    FunctionUnit function = (FunctionUnit)entry.Value;
                    for (Operand i = 0; i < function.body.Count; i++)
                    {
                        Instruction next = function.body[i];

                        if (next.opCode == OpCode.LOAD_IMPORTED_GLOBAL || next.opCode == OpCode.LOAD_IMPORTED_CONSTANT)
                        {
                            next.opB = new_index;
                            function.body[i] = next;
                        }
                    }
                }
            }
        }
    }
}
