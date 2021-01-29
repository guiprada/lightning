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
        public List<ValIntrinsic> intrinsics;
        public Dictionary<string, ValTable> tables;

        public Library(List<ValIntrinsic> p_intrinsics, Dictionary<string, ValTable> p_tables)
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
            foreach (ValIntrinsic i in lib2.intrinsics)
            {
                lib1.intrinsics.Add(i);
            }
            foreach (KeyValuePair<string, ValTable> entry in lib2.tables)
            {
                lib1.tables.Add(entry.Key, entry.Value);
            }
        }

        public static Library GetPrelude()
        {

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// Tables

            Dictionary<string, ValTable> tables = new Dictionary<string, ValTable>();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// rand

            {
                ValTable rand = new ValTable(null, null);

                var rng = new Random();

                Value nextInt(VM vm)
                {
                    int max = (int)((ValNumber)vm.StackPeek(0)).content;
                    return new ValNumber(rng.Next(max + 1));
                }
                rand.TableSet(new ValString("int"), new ValIntrinsic("int", nextInt, 1));

                Value nextFloat(VM vm)
                {
                    return new ValNumber((Number)rng.NextDouble());
                }
                rand.TableSet(new ValString("float"), new ValIntrinsic("float", nextFloat, 0));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// list

            {
                ValTable list = new ValTable(null, null);

                Value listPush(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    Value value = vm.StackPeek(1);
                    list.elements.Add(value);

                    return Value.Nil;
                }
                list.TableSet(new ValString("push"), new ValIntrinsic("push", listPush, 2));

                //////////////////////////////////////////////////////
                Value listPop(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    Value value = list.elements[^1];
                    list.elements.RemoveRange(list.elements.Count - 1, 1);

                    return value;
                }
                list.TableSet(new ValString("pop"), new ValIntrinsic("pop", listPop, 1));

                //////////////////////////////////////////////////////
                Value listToString(VM vm)
                {
                    ValTable this_table = vm.StackPeek(0) as ValTable;
                    bool first = true;
                    string value = "";
                    foreach (Value v in this_table.elements)
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
                    return new ValString(value);
                }
                list.TableSet(new ValString("to_string"), new ValIntrinsic("to_string", listToString, 1));

                ////////////////////////////////////////////////////
                Value listCount(VM vm)
                {
                    ValTable this_table = vm.StackPeek(0) as ValTable;
                    int count = this_table.ECount;
                    return new ValNumber(count);
                }
                list.TableSet(new ValString("count"), new ValIntrinsic("count", listCount, 1));

                //////////////////////////////////////////////////////
                Value listClear(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    list.elements.Clear();
                    return Value.Nil;
                }
                list.TableSet(new ValString("clear"), new ValIntrinsic("clear", listClear, 1));

                //////////////////////////////////////////////////////
                Value listRemoveRange(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    int range_init = (int)((ValNumber)vm.StackPeek(1)).content;
                    int range_end = (int)((ValNumber)vm.StackPeek(2)).content;
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);

                    return Value.Nil;
                }
                list.TableSet(new ValString("remove"), new ValIntrinsic("remove", listRemoveRange, 3));

                //////////////////////////////////////////////////////
                Value listCopy(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    List<Value> new_list_elements = new List<Value>();
                    foreach (Value v in list.elements)
                    {
                        new_list_elements.Add(v);
                    }
                    ValTable new_list = new ValTable(new_list_elements, null);

                    return new_list;
                }
                list.TableSet(new ValString("copy"), new ValIntrinsic("copy", listCopy, 1));

                //////////////////////////////////////////////////////
                Value listSplit(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    int range_init = (int)((ValNumber)vm.StackPeek(1)).content;
                    List<Value> new_list_elements = list.elements.GetRange(range_init, list.elements.Count - range_init);
                    list.elements.RemoveRange(range_init, list.elements.Count - range_init);
                    ValTable new_list = new ValTable(new_list_elements, null);

                    return new_list;
                }
                list.TableSet(new ValString("split"), new ValIntrinsic("split", listSplit, 2));

                //////////////////////////////////////////////////////
                Value listSlice(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    int range_init = (int)((ValNumber)vm.StackPeek(1)).content;
                    int range_end = (int)((ValNumber)vm.StackPeek(2)).content;

                    List<Value> new_list_elements = list.elements.GetRange(range_init, range_end - range_init + 1);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);
                    ValTable new_list = new ValTable(new_list_elements, null);

                    return new_list;
                }
                list.TableSet(new ValString("slice"), new ValIntrinsic("slice", listSlice, 3));
                //////////////////////////////////////////////////////
                Value listReverse(VM vm)
                {
                    ValTable list = (ValTable)vm.StackPeek(0);
                    list.elements.Reverse();

                    return Value.Nil;
                }
                list.TableSet(new ValString("reverse"), new ValIntrinsic("reverse", listReverse, 1));

                tables.Add("list", list);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// table

            {
                ValTable table = new ValTable(null, null);

                //////////////////////////////////////////////////////
                Value tableIndexes(VM vm)
                {
                    ValTable this_table = vm.StackPeek(0) as ValTable;
                    ValTable indexes = new ValTable(null, null);

                    foreach (ValString v in this_table.table.Keys)
                    {
                        indexes.elements.Add(v);
                    }

                    return indexes;
                }
                table.TableSet(new ValString("indexes"), new ValIntrinsic("indexes", tableIndexes, 1));

                //////////////////////////////////////////////////////
                Value tableCopy(VM vm)
                {
                    ValTable this_table = vm.StackPeek(0) as ValTable;
                    Dictionary<ValString, Value> table_copy = new Dictionary<ValString, Value>();
                    foreach (KeyValuePair<ValString, Value> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    ValTable copy = new ValTable(null, table_copy);

                    return copy;
                }
                table.TableSet(new ValString("copy"), new ValIntrinsic("copy", tableCopy, 1));

                //////////////////////////////////////////////////////
                Value tableClear(VM vm)
                {
                    ValTable this_table = vm.StackPeek(0) as ValTable;
                    return Value.Nil;
                }
                table.TableSet(new ValString("clear"), new ValIntrinsic("clear", tableClear, 1));

                tables.Add("table", table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                ValTable math = new ValTable(null, null);
                math.TableSet(new ValString("pi"), new ValNumber((Number)Math.PI));
                math.TableSet(new ValString("e"), new ValNumber((Number)Math.E));
#if DOUBLE
                math.TableSet(new ValString("double"), Value.True);
#else
                math.TableSet(new ValString("double"), Value.False);
#endif

                //////////////////////////////////////////////////////
                Value sin(VM vm)
                {
                    return new ValNumber((Number)Math.Sin(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("sin"), new ValIntrinsic("sin", sin, 1));

                //////////////////////////////////////////////////////
                Value cos(VM vm)
                {
                    return new ValNumber((Number)Math.Cos(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("cos"), new ValIntrinsic("cos", cos, 1));

                //////////////////////////////////////////////////////
                Value tan(VM vm)
                {
                    return new ValNumber((Number)Math.Tan(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("tan"), new ValIntrinsic("tan", tan, 1));

                //////////////////////////////////////////////////////
                Value sec(VM vm)
                {
                    return new ValNumber((Number)(1 / Math.Cos(((ValNumber)vm.StackPeek(0)).content)));
                }
                math.TableSet(new ValString("sec"), new ValIntrinsic("sec", sec, 1));

                //////////////////////////////////////////////////////
                Value cosec(VM vm)
                {
                    return new ValNumber((Number)(1 / Math.Sin(((ValNumber)vm.StackPeek(0)).content)));
                }
                math.TableSet(new ValString("cosec"), new ValIntrinsic("cosec", cosec, 1));

                //////////////////////////////////////////////////////
                Value cotan(VM vm)
                {
                    return new ValNumber((Number)(1 / Math.Tan(((ValNumber)vm.StackPeek(0)).content)));
                }
                math.TableSet(new ValString("cotan"), new ValIntrinsic("cotan", cotan, 1));

                //////////////////////////////////////////////////////
                Value asin(VM vm)
                {
                    return new ValNumber((Number)Math.Asin(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("asin"), new ValIntrinsic("asin", asin, 1));

                //////////////////////////////////////////////////////
                Value acos(VM vm)
                {
                    return new ValNumber((Number)Math.Acos(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("acos"), new ValIntrinsic("acos", acos, 1));

                //////////////////////////////////////////////////////
                Value atan(VM vm)
                {
                    return new ValNumber((Number)Math.Atan(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("atan"), new ValIntrinsic("atan", atan, 1));

                //////////////////////////////////////////////////////
                Value sinh(VM vm)
                {
                    return new ValNumber((Number)Math.Sinh(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("sinh"), new ValIntrinsic("sinh", sinh, 1));

                //////////////////////////////////////////////////////
                Value cosh(VM vm)
                {
                    return new ValNumber((Number)Math.Cosh(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("cosh"), new ValIntrinsic("cosh", cosh, 1));

                //////////////////////////////////////////////////////
                Value tanh(VM vm)
                {
                    return new ValNumber((Number)Math.Tanh(((ValNumber)vm.StackPeek(0)).content));
                }
                math.TableSet(new ValString("tanh"), new ValIntrinsic("tanh", tanh, 1));

                //////////////////////////////////////////////////////
                Value pow(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    Number exponent = ((ValNumber)vm.StackPeek(1)).content;
                    return new ValNumber((Number)Math.Pow(value, exponent));
                }
                math.TableSet(new ValString("pow"), new ValIntrinsic("pow", pow, 2));

                //////////////////////////////////////////////////////
                Value root(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    Number exponent = ((ValNumber)vm.StackPeek(1)).content;
                    return new ValNumber((Number)Math.Pow(value, 1 / exponent));
                }
                math.TableSet(new ValString("root"), new ValIntrinsic("root", root, 2));

                //////////////////////////////////////////////////////
                Value sqroot(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    return new ValNumber((Number)Math.Sqrt(value));
                }
                math.TableSet(new ValString("sqroot"), new ValIntrinsic("sqroot", sqroot, 1));
                //////////////////////////////////////////////////////
                Value exp(VM vm)
                {
                    Number exponent = ((ValNumber)vm.StackPeek(0)).content;
                    return new ValNumber((Number)Math.Exp(exponent));
                }
                math.TableSet(new ValString("exp"), new ValIntrinsic("exp", exp, 1));

                //////////////////////////////////////////////////////
                Value log(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    Number this_base = ((ValNumber)vm.StackPeek(1)).content;
                    return new ValNumber((Number)Math.Log(value, this_base));
                }
                math.TableSet(new ValString("log"), new ValIntrinsic("log", log, 2));

                //////////////////////////////////////////////////////
                Value ln(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    return new ValNumber((Number)Math.Log(value, Math.E));
                }
                math.TableSet(new ValString("ln"), new ValIntrinsic("ln", ln, 1));

                //////////////////////////////////////////////////////
                Value log10(VM vm)
                {
                    Number value = ((ValNumber)vm.StackPeek(0)).content;
                    return new ValNumber((Number)Math.Log(value, (Number)10));
                }
                math.TableSet(new ValString("log10"), new ValIntrinsic("log10", log10, 1));

                //////////////////////////////////////////////////////
                Value mod(VM vm)
                {
                    Number value1 = ((ValNumber)vm.StackPeek(0)).content;
                    Number value2 = ((ValNumber)vm.StackPeek(1)).content;
                    return new ValNumber((Number)value1 % value2);
                }
                math.TableSet(new ValString("mod"), new ValIntrinsic("mod", mod, 2));

                //////////////////////////////////////////////////////
                Value idiv(VM vm)
                {
                    Number value1 = ((ValNumber)vm.StackPeek(0)).content;
                    Number value2 = ((ValNumber)vm.StackPeek(1)).content;
                    return new ValNumber((Number)(int)(value1 / value2));
                }
                math.TableSet(new ValString("idiv"), new ValIntrinsic("idiv", idiv, 2));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                ValTable time = new ValTable(null, null);

                Value now(VM vm)
                {
                    return new ValNumber((Number)(DateTime.UtcNow.Ticks / 10000));// Convert to seconds
                }
                time.TableSet(new ValString("now"), new ValIntrinsic("now", now, 0));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////// Global Intrinsics

            List<ValIntrinsic> functions = new List<ValIntrinsic>();

            //////////////////////////////////////////////////////
            Value eval(VM vm)
            {
                string eval_code = ((ValString)vm.StackPeek(0)).ToString();
                Scanner scanner = new Scanner(eval_code);

                Parser parser = new Parser(scanner.Tokens);
                if (parser.Errors.Count > 0)
                {
                    Console.WriteLine("Parsing had errors:");
                    foreach (string error in parser.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return Value.Nil;
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
                    return Value.Nil;
                }
                if (code_generator.HasChunked == true)
                {
                    VM imported_vm = new VM(chunk);
                    VMResult result = imported_vm.Run();
                    if (result.status == VMResultType.OK)
                    {
                        if (result.value.GetType() == typeof(ValTable) || result.value.GetType() == typeof(ValFunction))
                            MakeModule(result.value, eval_name, vm, imported_vm);
                        //vm.GetChunk().Print();                    
                        return result.value;
                    }
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("eval", eval, 1));

            //////////////////////////////////////////////////////
            Value loadFile(VM vm)
            {
                string path = ((ValString)vm.StackPeek(0)).ToString();
                string input;
                using (var sr = new StreamReader(path))
                {
                    input = sr.ReadToEnd();
                }
                if (input != null)
                    return new ValString(input);

                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("load_file", loadFile, 1));

            //////////////////////////////////////////////////////
            Value writeFile(VM vm)
            {
                string path = ((ValString)vm.StackPeek(0)).ToString();
                string output = ((ValString)vm.StackPeek(1)).ToString();
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                {
                    file.Write(output);
                }

                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("write_file", writeFile, 2));

            //////////////////////////////////////////////////////
            Value appendFile(VM vm)
            {
                string path = ((ValString)vm.StackPeek(0)).ToString();
                string output = ((ValString)vm.StackPeek(1)).ToString();
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                {
                    file.Write(output);
                }

                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("append_file", appendFile, 2));

            ////////////////////////////////////////////////////
            Value require(VM vm)
            {
                string path = ((ValString)vm.StackPeek(0)).ToString();
                foreach(ValModule v in vm.modules)// skip already imported modules
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
                        return Value.Nil;
                    }

                    Node program = parser.ParsedTree;

                    Chunker code_generator = new Chunker(program, path, vm.GetChunk().Prelude);
                    Chunk chunk = code_generator.Code;
                    if (code_generator.Errors.Count > 0)
                    {
                        Console.WriteLine("Code generation had errors:");
                        foreach (string error in code_generator.Errors)
                            Console.WriteLine(error);
                        return Value.Nil;
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
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("require", require, 1));

            //////////////////////////////////////////////////////
            Value modules(VM vm)
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

                return new ValString(modules);
            }
            functions.Add(new ValIntrinsic("modules", modules, 0));


            //////////////////////////////////////////////////////            
            Value makeIterator(VM vm)
            {
                ValTable this_table = vm.StackPeek(0) as ValTable;
                int i = -1;
                Value value = Value.Nil;
                ValString value_string = new ValString("value");

                ValTable iterator = new ValTable(null, null);
                Value next(VM vm)
                {
                    if (i < (this_table.ECount - 1))
                    {
                        i++;
                        iterator.table[value_string] = this_table.elements[i];
                        return Value.True;
                    }
                    return Value.False;
                };
                iterator.TableSet(new ValString("next"), new ValIntrinsic("iterator_next", next, 0));
                return iterator;
            }
            functions.Add(new ValIntrinsic("iterator", makeIterator, 1));

            //////////////////////////////////////////////////////
            Value makeIteratorTable(VM vm)
            {
                ValTable this_table = vm.StackPeek(0) as ValTable;
                System.Collections.IDictionaryEnumerator enumerator = this_table.table.GetEnumerator();

                ValString value_string = new ValString("value");
                ValString key_string = new ValString("key");
                ValTable iterator = new ValTable(null, null);
                iterator.table[key_string] = Value.Nil;
                iterator.table[value_string] = Value.Nil;

                Value next(VM vm)
                {
                    if (enumerator.MoveNext())
                    {
                        iterator.table[key_string] = (ValString)enumerator.Key;
                        iterator.table[value_string] = enumerator.Value as Value;
                        return Value.True;
                    }
                    return Value.False;
                };

                iterator.TableSet(new ValString("next"), new ValIntrinsic("iterator_table_next", next, 0));
                return iterator;
            }
            functions.Add(new ValIntrinsic("iterator_table", makeIteratorTable, 1));

            //////////////////////////////////////////////////////
            Value writeLine(VM vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Unescape(vm.StackPeek(0).ToString()));
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("write_line", writeLine, 1));

            //////////////////////////////////////////////////////
            Value write(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Unescape(vm.StackPeek(0).ToString()));
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("write", write, 1));

            //////////////////////////////////////////////////////
            Value writeTable(VM vm)
            {
                ValTable this_table = vm.StackPeek(0) as ValTable;
                bool first = true;
                foreach (KeyValuePair<ValString, Value> entry in this_table.table)
                {
                    if (first)
                    {
                        Console.Write(
                            System.Text.RegularExpressions.Regex.Unescape(entry.Key.ToString())
                            + " : " + System.Text.RegularExpressions.Regex.Unescape(entry.Value.ToString()));
                        first = false;
                    }
                    else
                    {
                        Console.Write(
                            ", "
                            + System.Text.RegularExpressions.Regex.Unescape(entry.Key.ToString())
                            + " : " + System.Text.RegularExpressions.Regex.Unescape(entry.Value.ToString()));
                    }
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("write_table", writeTable, 1));

            //////////////////////////////////////////////////////
            Value readln(VM vm)
            {
                string read = Console.ReadLine();
                return new ValString(read);
            }
            functions.Add(new ValIntrinsic("read_line", readln, 0));

            //////////////////////////////////////////////////////
            Value readNumber(VM vm)
            {
                string read = Console.ReadLine();    
                if (Number.TryParse(read, out Number n))
                    return new ValNumber(n);
                else
                    return Value.Nil;
            }
            functions.Add(new ValIntrinsic("read_number", readNumber, 0));

            //////////////////////////////////////////////////////
            Value read(VM vm)
            {
                int read = Console.Read();
                if (read > 0) {
                    char next = Convert.ToChar(read);
                    if (next == '\n')
                        return Value.Nil;
                    else
                        return new ValString(Char.ToString(next));
                 }
                else
                    return Value.Nil;
            }
            functions.Add(new ValIntrinsic("read", read, 0));


            //////////////////////////////////////////////////////
            Value tCount(VM vm)
            {
                ValTable this_table = vm.StackPeek(0) as ValTable;
                int count = this_table.TCount;
                return new ValNumber(count);
            }
            functions.Add(new ValIntrinsic("count_table", tCount, 1));

            //////////////////////////////////////////////////////
            Value count(VM vm)
            {
                ValTable this_table = vm.StackPeek(0) as ValTable;
                int count = this_table.Count;
                return new ValNumber(count);
            }
            functions.Add(new ValIntrinsic("count_all", count, 1));

            //////////////////////////////////////////////////////
            Value type(VM vm)
            {
                Type this_type = vm.StackPeek(0).GetType();
                return new ValString(this_type.ToString());
            }
            functions.Add(new ValIntrinsic("type", type, 1));

            //////////////////////////////////////////////////////
            Value maybe(VM vm)
            {
                Value first = vm.StackPeek(0);
                Value second = vm.StackPeek(1);
                if (first.GetType() != typeof(ValNil))
                    return first;
                else
                    return second;
            }
            functions.Add(new ValIntrinsic("maybe", maybe, 2));

            //////////////////////////////////////////////////////
#if ROSLYN
            Value createIntrinsic(VM vm)
            {
                ValString name = vm.StackPeek(0) as ValString;
                ValNumber arity = vm.StackPeek(1) as ValNumber;
                ValString val_body = vm.StackPeek(2) as ValString;                
                string body = val_body.ToString();

                var options = ScriptOptions.Default.AddReferences(
                    typeof(Value).Assembly,
                    typeof(VM).Assembly).WithImports("lightning", "System");
                Func<VM, Value> new_intrinsic = CSharpScript.EvaluateAsync<Func<VM, Value>>(body, options)
                    .GetAwaiter().GetResult();

                return new ValIntrinsic(name.ToString(), new_intrinsic, (int)arity.content);
            }
            functions.Add(new ValIntrinsic("intrinsic", createIntrinsic, 3));
#endif

            ////////////////////////////////////////////// strings
            
            Value charAt(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                ValNumber index = vm.StackPeek(1) as ValNumber;
                string input_string = val_input_string.ToString();
                if (index.content < input_string.Length)
                {
                    char result = input_string[(int)index.content];
                    return new ValString(result.ToString());
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("char_at", charAt, 2));

            //////////////////////////////////////////////////////

            Value isAlpha(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                string input_string = val_input_string.ToString();
                if (1 <= input_string.Length)
                {
                    char head = input_string[0];
                    if (Char.IsLetter(head))
                    {
                        return Value.True;
                    }
                }
                return Value.False;
            }
            functions.Add(new ValIntrinsic("is_alpha", isAlpha, 1));

            //////////////////////////////////////////////////////

            Value isDigit(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                string input_string = val_input_string.ToString();
                if (1 <= input_string.Length)
                {
                    char head = input_string[0];
                    if (Char.IsDigit(head))
                    {
                        return Value.True;
                    }
                    return Value.False;
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("is_digit", isDigit, 1));

            //////////////////////////////////////////////////////

            Value stringSlice(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                string input_string = val_input_string.ToString();
                Number start = (vm.StackPeek(1) as ValNumber).content;
                Number end = (vm.StackPeek(2) as ValNumber).content;

                if (end < input_string.Length)
                {
                    string result = input_string.Substring((int)start, (int)(end - start));
                    return new ValString(result);
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("string_slice", stringSlice, 3));

            //////////////////////////////////////////////////////
            
            Value stringSplit(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                string input_string = val_input_string.ToString();
                Number start = (vm.StackPeek(1) as ValNumber).content; 
                if (start < input_string.Length)
                {
                    Number end = input_string.Length ;                    
                    string result = input_string.Substring((int)start, (int)(end - start));                    
                    val_input_string.content = input_string.Substring(0, (int)start);
                    return new ValString(result);
                }
                return Value.Nil;
            }
            functions.Add(new ValIntrinsic("string_split", stringSplit, 2));

            //////////////////////////////////////////////////////

            Value stringLength(VM vm)
            {
                ValString val_input_string = vm.StackPeek(0) as ValString;
                return new ValNumber(val_input_string.ToString().Length);
            }
            functions.Add(new ValIntrinsic("string_length", stringLength, 1));

            //////////////////////////////////////////////////////

            Value stringCopy(VM vm)
            {
                Value val_input_string = vm.StackPeek(0);
                if (val_input_string.GetType() == typeof(ValString))
                    return new ValString(val_input_string.ToString());
                else
                    return Value.Nil;
            }
            functions.Add(new ValIntrinsic("string_copy", stringCopy, 1));

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
            public List<int> relocatedTables;
            public Dictionary<Operand, Operand> relocatedModules;            
            public ValModule module;
            public Operand moduleIndex;
            public RelocationInfo(
                VM p_importingVM,
                VM p_importedVM,
                Dictionary<Operand, Operand> p_relocatedGlobals,
                List<Operand> p_toBeRelocatedGlobals,
                List<int> p_relocatedTables,
                Dictionary<Operand, Operand> p_relocatedModules,

                ValModule p_module,
                Operand p_moduleIndex)
            {
                importingVM = p_importingVM;
                importedVM = p_importedVM;
                relocatedGlobals = p_relocatedGlobals;
                toBeRelocatedGlobals = p_toBeRelocatedGlobals;
                relocatedTables = p_relocatedTables;
                relocatedModules = p_relocatedModules;
                module = p_module;
                moduleIndex = p_moduleIndex;
            }
        }

        static ValModule MakeModule(Value this_value, string name, VM importing_vm, VM imported_vm)
        {
            Dictionary<Operand, Operand> relocated_modules = new Dictionary<Operand, Operand>();
            foreach (ValModule m in imported_vm.modules)
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
                //ImportModule(m, (Operand)copied_module_index);
            }

            ValModule module = new ValModule(name, null, null, null);
            Operand module_index = importing_vm.AddModule(module);
            module.importIndex = module_index;
            RelocationInfo relocationInfo = new RelocationInfo(
                importing_vm,
                imported_vm,
                new Dictionary<Operand, Operand>(),
                new List<Operand>(),
                new List<int>(),
                relocated_modules,
                module,
                (Operand)module_index);

            if (this_value.GetType() == typeof(ValFunction)) RelocateFunction(this_value as ValFunction, relocationInfo);
            else if (this_value.GetType() == typeof(ValClosure)) RelocateClosure(this_value as ValClosure, relocationInfo);
            else if (this_value.GetType() == typeof(ValTable)) FindFunction(this_value as ValTable, relocationInfo);


            return module;
        }

        static void FindFunction(ValTable table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<ValString, Value> entry in table.table)
                {
                    if (entry.Value.GetType() == typeof(ValFunction)) RelocateFunction(entry.Value as ValFunction, relocationInfo);
                    else if (entry.Value.GetType() == typeof(ValClosure)) RelocateClosure(entry.Value as ValClosure, relocationInfo);
                    else if (entry.Value.GetType() == typeof(ValTable)) FindFunction(entry.Value as ValTable, relocationInfo);

                    relocationInfo.module.TableSet(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ValClosure closure, RelocationInfo relocationInfo)
        {
            foreach (ValUpValue v in closure.upValues)
            {
                if (v.Val.GetType() == typeof(ValClosure)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    Console.WriteLine("here--------");
                    RelocateClosure(v.Val as ValClosure, relocationInfo);
                }
                else if (v.Val.GetType() == typeof(ValFunction)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    Console.WriteLine("here--------");
                    RelocateFunction(v.Val as ValFunction, relocationInfo);
                }
            }

            RelocateFunction(closure.function, relocationInfo);
        }

        static void RelocateFunction(ValFunction function, RelocationInfo relocationInfo)
        {
            List<ValTable> relocation_stack = new List<ValTable>();
            RelocateChunk(function, relocationInfo);

            for (Operand i = 0; i < relocationInfo.toBeRelocatedGlobals.Count; i++)
            {
                Value new_value = relocationInfo.importedVM.GetGlobal(relocationInfo.toBeRelocatedGlobals[i]);

                relocationInfo.module.globals.Add(new_value);
                relocationInfo.relocatedGlobals.Add(relocationInfo.toBeRelocatedGlobals[i], (Operand)(relocationInfo.module.globals.Count - 1));

                if (new_value.GetType() == typeof(ValTable)) relocation_stack.Add(new_value as ValTable);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();
        
            foreach (ValTable v in relocation_stack)
            {
                FindFunction(v, relocationInfo);
            }
        }

        static void RelocateChunk(ValFunction function, RelocationInfo relocationInfo)
        {
            for (Operand i = 0; i < function.body.Count; i++)
            {                
                Instruction next = function.body[i];
                
                if (next.opCode == OpCode.LOADG)
                {
                    if ((next.opCode == OpCode.LOADG && next.opA >= relocationInfo.importedVM.GetChunk().Prelude.intrinsics.Count))
                    {

                        OpCode this_opcode = OpCode.LOADI;

                        if (relocationInfo.relocatedGlobals.ContainsKey(next.opA))
                        {
                            next.opCode = this_opcode;
                            next.opA = relocationInfo.relocatedGlobals[next.opA];
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else if (relocationInfo.toBeRelocatedGlobals.Contains(next.opA))
                        {
                            next.opCode = this_opcode;
                            next.opA = (Operand)(relocationInfo.toBeRelocatedGlobals.IndexOf(next.opA) + relocationInfo.relocatedGlobals.Count);
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else
                        {
                            Operand global_count = (Operand)(relocationInfo.relocatedGlobals.Count + relocationInfo.toBeRelocatedGlobals.Count);
                            relocationInfo.toBeRelocatedGlobals.Add(next.opA);
                            next.opCode = this_opcode;
                            next.opA = global_count;
                            next.opB = relocationInfo.moduleIndex;
                        }
                    }
                }
                else if (next.opCode == OpCode.LOADI)
                {
                    if (function.module != relocationInfo.module.name)
                    {
                        bool found = false;
                        foreach(ValModule v  in relocationInfo.importingVM.modules)
                        {
                            if(function.module == v.name)
                            {
                                next.opB = v.importIndex;
                                found = true;
                                break;
                            }
                        }
                        if(found == false)
                            Console.WriteLine("Can not find LOADI index" + function.module);
                    }
                    else
                    {
                        next.opB = relocationInfo.relocatedModules[next.opB];
                    }
                }
                else if (next.opCode == OpCode.LOADC)
                {
                    if (function.module == relocationInfo.module.name)
                    {
                        Value this_value = relocationInfo.importedVM.GetChunk().GetConstant(next.opA);

                        if (relocationInfo.importingVM.GetChunk().GetConstants().Contains(this_value))
                        {
                            next.opA = (Operand)relocationInfo.importingVM.GetChunk().GetConstants().IndexOf(this_value);
                        }
                        else
                        {
                            relocationInfo.importingVM.GetChunk().GetConstants().Add(this_value);
                            next.opA = (Operand)(relocationInfo.importingVM.GetChunk().GetConstants().Count - 1);
                        }
                    }
                }
                else if (next.opCode == OpCode.FUNDCL)
                {
                    Value this_value = relocationInfo.importedVM.GetChunk().GetConstant(next.opC);
                    if (relocationInfo.importingVM.GetChunk().GetConstants().Contains(this_value))
                    {
                        next.opC = (Operand)relocationInfo.importingVM.GetChunk().GetConstants().IndexOf(this_value);
                    }
                    else
                    {
                        relocationInfo.importingVM.GetChunk().GetConstants().Add(this_value);
                        next.opC = (Operand)(relocationInfo.importingVM.GetChunk().GetConstants().Count - 1);
                        RelocateChunk((this_value as ValClosure).function, relocationInfo);
                    }
                }
                //Chunk.PrintInstruction(next);
                //Console.WriteLine();
                function.body[i] = next;
            }
        }

        //static void ImportModule(ValModule module, Operand new_index)
        //{
        //    Console.WriteLine("Importing module " + module.name);
        //    foreach (KeyValuePair<ValString, Value> entry in module.table)
        //    {
        //        if (entry.Value.GetType() == typeof(ValFunction))
        //        {
        //            ValFunction function = entry.Value as ValFunction;
        //            for (Operand i = 0; i < function.body.Count; i++)
        //            {
        //                Instruction next = function.body[i];

        //                if (next.opCode == OpCode.LOADI)
        //                {
        //                    Chunk.PrintInstruction(next);
        //                    Console.Write(" ");
        //                    Console.WriteLine("here");
        //                    next.opB = new_index;
        //                    function.body[i] = next;
        //                    Chunk.PrintInstruction(function.body[i]);
        //                    Console.WriteLine();
        //                }
        //            }
        //        }
        //    }
        //    Console.WriteLine("end importing module " + module.name);
        //}
    }
}
