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
                    mem_use.TABLE_SET(new StringUnit("stack_count"), new Unit(vm.StackCount()));
                    mem_use.TABLE_SET(new StringUnit("globals_count"), new Unit(vm.GlobalsCount()));
                    mem_use.TABLE_SET(new StringUnit("variables_count"), new Unit(vm.VariablesCount()));
                    mem_use.TABLE_SET(new StringUnit("variables_capacity"), new Unit(vm.VariablesCapacity()));
                    mem_use.TABLE_SET(new StringUnit("upvalues_count"), new Unit(vm.UpValuesCount()));
                    mem_use.TABLE_SET(new StringUnit("upvalue_capacity"), new Unit(vm.UpValueCapacity()));

                    return new Unit(mem_use);
                }
                machine.TABLE_SET(new StringUnit("memory_use"), new Unit(new IntrinsicUnit("memory_use", memoryUse, 0)));

                tables.Add("machine", machine);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////// intrinsic
            {
                TableUnit intrinsic = new TableUnit(null, null);
#if ROSLYN
                Unit createIntrinsic(VM vm)
                {
                    StringUnit name = (StringUnit)vm.stack.Peek(0).value;
                    Number arity = vm.stack.Peek(1).number;
                    StringUnit val_body = (StringUnit)vm.stack.Peek(2).value;
                    string body = val_body.ToString();

                    var options = ScriptOptions.Default.AddReferences(
                        typeof(Unit).Assembly,
                        typeof(VM).Assembly).WithImports("lightning", "System");
                    Func<VM, Unit> new_intrinsic = CSharpScript.EvaluateAsync<Func<VM, Unit>>(body, options)
                        .GetAwaiter().GetResult();

                    return new Unit(new IntrinsicUnit(name.ToString(), new_intrinsic, (int)arity));
                }
                intrinsic.TABLE_SET(new StringUnit("create"), new Unit(new IntrinsicUnit("create", createIntrinsic, 3)));
#else
                intrinsic.TABLE_SET(new StringUnit("create"), new Unit("null"));
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
                    int max = (int)(vm.stack.Peek(0)).unitValue;
                    return new Unit(rng.Next(max + 1));
                }
                rand.TABLE_SET(new StringUnit("int"), new Unit(new IntrinsicUnit("int", nextInt, 1)));

                Unit nextFloat(VM vm)
                {
                    return new Unit((Number)rng.NextDouble());
                }
                rand.TABLE_SET(new StringUnit("float"), new Unit(new IntrinsicUnit("float", nextFloat, 0)));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// list

            {
                TableUnit list = new TableUnit(null, null);

                Unit listPush(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    Unit value = vm.stack.Peek(1);
                    list.elements.Add(value);

                    return new Unit("null");
                }
                list.TABLE_SET(new StringUnit("push"), new Unit(new IntrinsicUnit("push", listPush, 2)));

                //////////////////////////////////////////////////////
                Unit listPop(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    Number value = list.elements[^1].unitValue;
                    list.elements.RemoveRange(list.elements.Count - 1, 1);

                    return new Unit(value);
                }
                list.TABLE_SET(new StringUnit("pop"), new Unit(new IntrinsicUnit("pop", listPop, 1)));

                //////////////////////////////////////////////////////
                Unit listToString(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
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
                    return new Unit(new StringUnit(value));
                }
                list.TABLE_SET(new StringUnit("to_string"), new Unit(new IntrinsicUnit("to_string", listToString, 1)));

                ////////////////////////////////////////////////////
                Unit listCount(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int count = this_table.ECount;
                    return new Unit(count);
                }
                list.TABLE_SET(new StringUnit("count"), new Unit(new IntrinsicUnit("count", listCount, 1)));

                //////////////////////////////////////////////////////
                Unit listClear(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    list.elements.Clear();
                    return new Unit("null");
                }
                list.TABLE_SET(new StringUnit("clear"), new Unit(new IntrinsicUnit("clear", listClear, 1)));

                //////////////////////////////////////////////////////
                Unit listRemoveRange(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int range_init = (int)vm.stack.Peek(1).unitValue;
                    int range_end = (int)vm.stack.Peek(2).unitValue;
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit("null");
                }
                list.TABLE_SET(new StringUnit("remove"), new Unit(new IntrinsicUnit("remove", listRemoveRange, 3)));

                //////////////////////////////////////////////////////
                Unit listCopy(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in list.elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TABLE_SET(new StringUnit("copy"), new Unit(new IntrinsicUnit("copy", listCopy, 1)));

                //////////////////////////////////////////////////////
                Unit listSplit(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int range_init = (int)vm.stack.Peek(1).unitValue;
                    List<Unit> new_list_elements = list.elements.GetRange(range_init, list.elements.Count - range_init);
                    list.elements.RemoveRange(range_init, list.elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TABLE_SET(new StringUnit("split"), new Unit(new IntrinsicUnit("split", listSplit, 2)));

                //////////////////////////////////////////////////////
                Unit listSlice(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int range_init = (int)vm.stack.Peek(1).unitValue;
                    int range_end = (int)vm.stack.Peek(2).unitValue;

                    List<Unit> new_list_elements = list.elements.GetRange(range_init, range_end - range_init + 1);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TABLE_SET(new StringUnit("slice"), new Unit(new IntrinsicUnit("slice", listSlice, 3)));
                //////////////////////////////////////////////////////
                Unit listReverse(VM vm)
                {
                    TableUnit list = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    list.elements.Reverse();

                    return new Unit("null");
                }
                list.TABLE_SET(new StringUnit("reverse"), new Unit(new IntrinsicUnit("reverse", listReverse, 1)));

                //////////////////////////////////////////////////////

                Unit makeIndexesIterator(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int i = -1;
                    StringUnit value_string = new StringUnit("value");
                    StringUnit key_string = new StringUnit("key");

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.table[key_string] = new Unit(i);
                            iterator.table[value_string] = this_table.elements[i];
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.TABLE_SET(new StringUnit("next"), new Unit(new IntrinsicUnit("iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                list.TABLE_SET(new StringUnit("index_iterator"), new Unit(new IntrinsicUnit("list_index_iterator", makeIndexesIterator, 1)));

                //////////////////////////////////////////////////////

                Unit makeIterator(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int i = -1;
                    Unit value = new Unit("null");
                    StringUnit value_string = new StringUnit("value");

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.table[value_string] = this_table.elements[i];
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.TABLE_SET(new StringUnit("next"), new Unit(new IntrinsicUnit("iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                list.TABLE_SET(new StringUnit("iterator"), new Unit(new IntrinsicUnit("list_iterator", makeIterator, 1)));

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
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    int count = this_table.TCount;
                    return new Unit(count);
                }
                table.TABLE_SET(new StringUnit("count"), new Unit(new IntrinsicUnit("table_count", tableCount, 1)));

                //////////////////////////////////////////////////////
                Unit tableIndexes(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (StringUnit v in this_table.table.Keys)
                    {
                        indexes.elements.Add(new Unit(v));
                    }

                    return new Unit(indexes);
                }
                table.TABLE_SET(new StringUnit("indexes"), new Unit(new IntrinsicUnit("indexes", tableIndexes, 1)));

                //////////////////////////////////////////////////////
                Unit tableCopy(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    Dictionary<StringUnit, Unit> table_copy = new Dictionary<StringUnit, Unit>();
                    foreach (KeyValuePair<StringUnit, Unit> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    TableUnit copy = new TableUnit(null, table_copy);

                    return new Unit(copy);
                }
                table.TABLE_SET(new StringUnit("copy"), new Unit(new IntrinsicUnit("copy", tableCopy, 1)));

                //////////////////////////////////////////////////////
                Unit tableClear(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    return new Unit("null");
                }
                table.TABLE_SET(new StringUnit("clear"), new Unit(new IntrinsicUnit("clear", tableClear, 1)));

                Unit makeIteratorTable(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                    System.Collections.IDictionaryEnumerator enumerator = this_table.table.GetEnumerator();

                    StringUnit value_string = new StringUnit("value");
                    StringUnit key_string = new StringUnit("key");
                    TableUnit iterator = new TableUnit(null, null);
                    iterator.table[key_string] = new Unit("null");
                    iterator.table[value_string] = new Unit("null");

                    Unit next(VM vm)
                    {
                        if (enumerator.MoveNext())
                        {
                            iterator.table[key_string] = new Unit((StringUnit)enumerator.Key);
                            iterator.table[value_string] = (Unit)enumerator.Value;
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };

                    iterator.TABLE_SET(new StringUnit("next"), new Unit(new IntrinsicUnit("table_iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                table.TABLE_SET(new StringUnit("iterator"), new Unit(new IntrinsicUnit("iterator_table", makeIteratorTable, 1)));

                //////////////////////////////////////////////////////
                Unit tableToString(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
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
                    return new Unit(new StringUnit(value));
                }
                table.TABLE_SET(new StringUnit("to_string"), new Unit(new IntrinsicUnit("table_to_string", tableToString, 1)));

                tables.Add("table", table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                TableUnit math = new TableUnit(null, null);
                math.TABLE_SET(new StringUnit("pi"), new Unit((Number)Math.PI));
                math.TABLE_SET(new StringUnit("e"), new Unit((Number)Math.E));
#if DOUBLE
                math.TABLE_SET(new StringUnit("double"), new Unit(true));
#else
                math.TABLE_SET(new StringUnit("double"), new Unit(false));
#endif

                //////////////////////////////////////////////////////
                Unit sin(VM vm)
                {
                    return new Unit((Number)Math.Sin(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("sin"), new Unit(new IntrinsicUnit("sin", sin, 1)));

                //////////////////////////////////////////////////////
                Unit cos(VM vm)
                {
                    return new Unit((Number)Math.Cos(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("cos"), new Unit(new IntrinsicUnit("cos", cos, 1)));

                //////////////////////////////////////////////////////
                Unit tan(VM vm)
                {
                    return new Unit((Number)Math.Tan(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("tan"), new Unit(new IntrinsicUnit("tan", tan, 1)));

                //////////////////////////////////////////////////////
                Unit sec(VM vm)
                {
                    return new Unit((Number)(1 / Math.Cos(vm.stack.Peek(0).unitValue)));
                }
                math.TABLE_SET(new StringUnit("sec"), new Unit(new IntrinsicUnit("sec", sec, 1)));

                //////////////////////////////////////////////////////
                Unit cosec(VM vm)
                {
                    return new Unit((Number)(1 / Math.Sin(vm.stack.Peek(0).unitValue)));
                }
                math.TABLE_SET(new StringUnit("cosec"), new Unit(new IntrinsicUnit("cosec", cosec, 1)));

                //////////////////////////////////////////////////////
                Unit cotan(VM vm)
                {
                    return new Unit((Number)(1 / Math.Tan(vm.stack.Peek(0).unitValue)));
                }
                math.TABLE_SET(new StringUnit("cotan"), new Unit(new IntrinsicUnit("cotan", cotan, 1)));

                //////////////////////////////////////////////////////
                Unit asin(VM vm)
                {
                    return new Unit((Number)Math.Asin(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("asin"), new Unit(new IntrinsicUnit("asin", asin, 1)));

                //////////////////////////////////////////////////////
                Unit acos(VM vm)
                {
                    return new Unit((Number)Math.Acos(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("acos"), new Unit(new IntrinsicUnit("acos", acos, 1)));

                //////////////////////////////////////////////////////
                Unit atan(VM vm)
                {
                    return new Unit((Number)Math.Atan(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("atan"), new Unit(new IntrinsicUnit("atan", atan, 1)));

                //////////////////////////////////////////////////////
                Unit sinh(VM vm)
                {
                    return new Unit((Number)Math.Sinh(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("sinh"), new Unit(new IntrinsicUnit("sinh", sinh, 1)));

                //////////////////////////////////////////////////////
                Unit cosh(VM vm)
                {
                    return new Unit((Number)Math.Cosh(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("cosh"), new Unit(new IntrinsicUnit("cosh", cosh, 1)));

                //////////////////////////////////////////////////////
                Unit tanh(VM vm)
                {
                    return new Unit((Number)Math.Tanh(vm.stack.Peek(0).unitValue));
                }
                math.TABLE_SET(new StringUnit("tanh"), new Unit(new IntrinsicUnit("tanh", tanh, 1)));

                //////////////////////////////////////////////////////
                Unit pow(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    Number exponent = vm.stack.Peek(1).unitValue;
                    return new Unit((Number)Math.Pow(value, exponent));
                }
                math.TABLE_SET(new StringUnit("pow"), new Unit(new IntrinsicUnit("pow", pow, 2)));

                //////////////////////////////////////////////////////
                Unit root(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    Number exponent = vm.stack.Peek(1).unitValue;
                    return new Unit((Number)Math.Pow(value, 1 / exponent));
                }
                math.TABLE_SET(new StringUnit("root"), new Unit(new IntrinsicUnit("root", root, 2)));

                //////////////////////////////////////////////////////
                Unit sqroot(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    return new Unit((Number)Math.Sqrt(value));
                }
                math.TABLE_SET(new StringUnit("sqroot"), new Unit(new IntrinsicUnit("sqroot", sqroot, 1)));
                //////////////////////////////////////////////////////
                Unit exp(VM vm)
                {
                    Number exponent = vm.stack.Peek(0).unitValue;
                    return new Unit((Number)Math.Exp(exponent));
                }
                math.TABLE_SET(new StringUnit("exp"), new Unit(new IntrinsicUnit("exp", exp, 1)));

                //////////////////////////////////////////////////////
                Unit log(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    Number this_base = vm.stack.Peek(1).unitValue;
                    return new Unit((Number)Math.Log(value, this_base));
                }
                math.TABLE_SET(new StringUnit("log"), new Unit(new IntrinsicUnit("log", log, 2)));

                //////////////////////////////////////////////////////
                Unit ln(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    return new Unit((Number)Math.Log(value, Math.E));
                }
                math.TABLE_SET(new StringUnit("ln"), new Unit(new IntrinsicUnit("ln", ln, 1)));

                //////////////////////////////////////////////////////
                Unit log10(VM vm)
                {
                    Number value = vm.stack.Peek(0).unitValue;
                    return new Unit((Number)Math.Log(value, (Number)10));
                }
                math.TABLE_SET(new StringUnit("log10"), new Unit(new IntrinsicUnit("log10", log10, 1)));

                //////////////////////////////////////////////////////
                Unit mod(VM vm)
                {
                    Number value1 = vm.stack.Peek(0).unitValue;
                    Number value2 = vm.stack.Peek(1).unitValue;
                    return new Unit((Number)value1 % value2);
                }
                math.TABLE_SET(new StringUnit("mod"), new Unit(new IntrinsicUnit("mod", mod, 2)));

                //////////////////////////////////////////////////////
                Unit idiv(VM vm)
                {
                    Number value1 = vm.stack.Peek(0).unitValue;
                    Number value2 = vm.stack.Peek(1).unitValue;
                    return new Unit((Number)(int)(value1 / value2));
                }
                math.TABLE_SET(new StringUnit("idiv"), new Unit(new IntrinsicUnit("idiv", idiv, 2)));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                TableUnit time = new TableUnit(null, null);

                Unit now(VM vm)
                {
                    return new Unit(new WrapperUnit<long>(DateTime.Now.Ticks));
                }
                time.TABLE_SET(new StringUnit("now"), new Unit(new IntrinsicUnit("now", now, 0)));

                Unit timeSpan(VM vm)
                {
                    long timeStart = ((WrapperUnit<long>)vm.stack.Peek(0).heapUnitValue).UnWrapp();
                    long timeEnd = ((WrapperUnit<long>)vm.stack.Peek(1).heapUnitValue).UnWrapp();
                    return new Unit((Number)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                time.TABLE_SET(new StringUnit("span"), new Unit(new IntrinsicUnit("span", timeSpan, 2)));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// string
            {
                TableUnit string_table = new TableUnit(null, null);

                Unit stringSlice(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    string input_string = val_input_string.ToString();
                    Number start = vm.stack.Peek(1).unitValue;
                    Number end = vm.stack.Peek(2).unitValue;

                    if (end < input_string.Length)
                    {
                        string result = input_string.Substring((int)start, (int)(end - start));
                        return new Unit(new StringUnit(result));
                    }
                    return new Unit("null");
                }
                string_table.TABLE_SET(new StringUnit("slice"), new Unit(new IntrinsicUnit("string_slice", stringSlice, 3)));

                //////////////////////////////////////////////////////

                Unit stringSplit(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    string input_string = val_input_string.ToString();
                    Number start = vm.stack.Peek(1).unitValue;
                    if (start < input_string.Length)
                    {
                        Number end = input_string.Length;
                        string result = input_string.Substring((int)start, (int)(end - start));
                        val_input_string.content = input_string.Substring(0, (int)start);
                        return new Unit(new StringUnit(result));
                    }
                    return new Unit("null");
                }
                string_table.TABLE_SET(new StringUnit("split"), new Unit(new IntrinsicUnit("string_split", stringSplit, 2)));

                //////////////////////////////////////////////////////

                Unit stringLength(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    return new Unit(val_input_string.ToString().Length);
                }
                string_table.TABLE_SET(new StringUnit("length"), new Unit(new IntrinsicUnit("string_length", stringLength, 1)));

                //////////////////////////////////////////////////////

                Unit stringCopy(VM vm)
                {
                    Unit val_input_string = vm.stack.Peek(0);
                    if (val_input_string.HeapUnitType() == typeof(StringUnit))
                        return new Unit(new StringUnit(val_input_string.ToString()));
                    else
                        return new Unit("null");
                }
                string_table.TABLE_SET(new StringUnit("copy"), new Unit(new IntrinsicUnit("string_copy", stringCopy, 1)));

                //////////////////////////////////////////////////////

                tables.Add("string", string_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null, null);
                Unit charAt(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    Number index = vm.stack.Peek(1).unitValue;
                    string input_string = val_input_string.ToString();
                    if (index < input_string.Length)
                    {
                        char result = input_string[(int)index];
                        return new Unit(new StringUnit(result.ToString()));
                    }
                    return new Unit("null");
                }
                char_table.TABLE_SET(new StringUnit("at"), new Unit(new IntrinsicUnit("char_at", charAt, 2)));

                //////////////////////////////////////////////////////

                Unit isAlpha(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    string input_string = val_input_string.ToString();
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
                char_table.TABLE_SET(new StringUnit("is_alpha"), new Unit(new IntrinsicUnit("is_alpha", isAlpha, 1)));

                //////////////////////////////////////////////////////

                Unit isDigit(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.stack.Peek(0).heapUnitValue;
                    string input_string = val_input_string.ToString();
                    if (1 <= input_string.Length)
                    {
                        char head = input_string[0];
                        if (Char.IsDigit(head))
                        {
                            return new Unit(true);
                        }
                        return new Unit(false);
                    }
                    return new Unit("null");
                }
                char_table.TABLE_SET(new StringUnit("is_digit"), new Unit(new IntrinsicUnit("is_digit", isDigit, 1)));

                tables.Add("char", char_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// file
            {
                TableUnit file = new TableUnit(null, null);
                Unit loadFile(VM vm)
                {
                    string path = (vm.stack.Peek(0).heapUnitValue).ToString();
                    string input;
                    using (var sr = new StreamReader(path))
                    {
                        input = sr.ReadToEnd();
                    }
                    if (input != null)
                        return new Unit(new StringUnit(input));

                    return new Unit("null");
                }
                file.TABLE_SET(new StringUnit("load"), new Unit(new IntrinsicUnit("load_file", loadFile, 1)));

                //////////////////////////////////////////////////////
                Unit writeFile(VM vm)
                {
                    string path = (vm.stack.Peek(0).heapUnitValue).ToString();
                    string output = (vm.stack.Peek(1).heapUnitValue).ToString();
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return new Unit("null");
                }
                file.TABLE_SET(new StringUnit("write"), new Unit(new IntrinsicUnit("write_file", writeFile, 2)));

                //////////////////////////////////////////////////////
                Unit appendFile(VM vm)
                {
                    string path = (vm.stack.Peek(0).heapUnitValue).ToString();
                    string output = (vm.stack.Peek(1).heapUnitValue).ToString();
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }

                    return new Unit("null");
                }
                file.TABLE_SET(new StringUnit("append"), new Unit(new IntrinsicUnit("append_file", appendFile, 2)));

                tables.Add("file", file);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////// Global Intrinsics

            List<IntrinsicUnit> functions = new List<IntrinsicUnit>();

            //////////////////////////////////////////////////////
            Unit eval(VM vm)
            {
                string eval_code = (vm.stack.Peek(0).heapUnitValue).ToString();;
                Scanner scanner = new Scanner(eval_code);

                Parser parser = new Parser(scanner.Tokens);
                if (parser.Errors.Count > 0)
                {
                    Console.WriteLine("Parsing had errors:");
                    foreach (string error in parser.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return new Unit("null");
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
                    return new Unit("null");
                }
                if (code_generator.HasChunked == true)
                {
                    VM imported_vm = new VM(chunk);
                    VMResult result = imported_vm.Run();
                    if (result.status == VMResultType.OK)
                    {
                        if (result.value.HeapUnitType() == typeof(TableUnit) || result.value.HeapUnitType() == typeof(FunctionUnit))
                            MakeModule(result.value, eval_name, vm, imported_vm);
                        //vm.GetChunk().Print();
                        return result.value;
                    }
                }
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("eval", eval, 1));

            ////////////////////////////////////////////////////
            Unit require(VM vm)
            {
                string path = (vm.stack.Peek(0).heapUnitValue).ToString();
                foreach (ModuleUnit v in vm.modules)// skip already imported modules
                {
                    if (v.name == path)
                        return new Unit(v);
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
                        return new Unit("null");
                    }

                    Node program = parser.ParsedTree;

                    Chunker code_generator = new Chunker(program, path, vm.GetChunk().Prelude);
                    Chunk chunk = code_generator.Code;
                    if (code_generator.Errors.Count > 0)
                    {
                        Console.WriteLine("Code generation had errors:");
                        foreach (string error in code_generator.Errors)
                            Console.WriteLine(error);
                        return new Unit("null");
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
                return new Unit("null");
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

                return new Unit(new StringUnit(modules));
            }
            functions.Add(new IntrinsicUnit("modules", modules, 0));

            //////////////////////////////////////////////////////

            Unit writeLine(VM vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Unescape(vm.stack.Peek(0).ToString()));
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("write_line", writeLine, 1));

            //////////////////////////////////////////////////////
            Unit write(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Unescape(vm.stack.Peek(0).ToString()));
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("write", write, 1));

            //////////////////////////////////////////////////////
            Unit readln(VM vm)
            {
                string read = Console.ReadLine();
                return new Unit(new StringUnit(read));
            }
            functions.Add(new IntrinsicUnit("read_line", readln, 0));

            //////////////////////////////////////////////////////
            Unit readNumber(VM vm)
            {
                string read = Console.ReadLine();
                if (Number.TryParse(read, out Number n))
                    return new Unit(n);
                else
                    return new Unit("null");
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
                        return new Unit("null");
                    else
                        return new Unit(new StringUnit(Char.ToString(next)));
                }
                else
                    return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("read", read, 0));

            //////////////////////////////////////////////////////
            Unit count(VM vm)
            {
                TableUnit this_table = (TableUnit)vm.stack.Peek(0).heapUnitValue;
                int count = this_table.Count;
                return new Unit(count);
            }
            functions.Add(new IntrinsicUnit("count_all", count, 1));

            //////////////////////////////////////////////////////
            Unit type(VM vm)
            {
                Type this_type = vm.stack.Peek(0).HeapUnitType();
                return new Unit(new StringUnit(this_type.ToString()));
            }
            functions.Add(new IntrinsicUnit("type", type, 1));

            //////////////////////////////////////////////////////
            Unit maybe(VM vm)
            {
                Unit first = vm.stack.Peek(0);
                Unit second = vm.stack.Peek(1);
                if (first.HeapUnitType() != null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", maybe, 2));

            //////////////////////////////////////////////////////
            Unit resourcesTrim(VM vm)
            {
                vm.ResoursesTrim();
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("trim", resourcesTrim, 0));

            //////////////////////////////////////////////////////
            Unit releaseAllVMs(VM vm)
            {
                VM.ReleaseVMs();
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("release_all_vms", releaseAllVMs, 0));

            //////////////////////////////////////////////////////
            Unit releaseVMs(VM vm)
            {
                Unit count = vm.stack.Peek(0);
                VM.ReleaseVMs((int)count.unitValue);
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("release_vms", releaseVMs, 1));

            //////////////////////////////////////////////////////
            Unit countVMs(VM vm)
            {
                return new Unit(VM.CountVMs());
            }
            functions.Add(new IntrinsicUnit("count_vms", countVMs, 0));

            //////////////////////////////////////////////////////
            Unit forEach(VM vm)
            {
                TableUnit table = (TableUnit)(vm.stack.Peek(0).heapUnitValue);
                Unit func = vm.stack.Peek(1);

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
                    args.Add(new Unit(index));
                    args.Add(new Unit(table));
                    vms[index].CallFunction(func, args);
                });
                for (int i = init; i < end; i++)
                {
                    VM.RecycleVM(vms[i]);
                }
                return new Unit("null");
            }

            functions.Add(new IntrinsicUnit("foreach", forEach, 2));
            //////////////////////////////////////////////////////

            Unit ForRange(VM vm)
            {
                Number tasks = vm.stack.Peek(0).unitValue;
                TableUnit table = (TableUnit)(vm.stack.Peek(1).heapUnitValue);
                Unit func = vm.stack.Peek(2);

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
                    args.Add(new Unit(range_start));
                    int range_end = range_start + step;
                    if (range_end > count) range_end = count;
                    args.Add(new Unit(range_end));
                    args.Add(new Unit(table));
                    vms[index].CallFunction(func, args);
                });
                for (int i = 0; i < end; i++)
                {
                    VM.RecycleVM(vms[i]);
                }
                return new Unit("null");
            }

            functions.Add(new IntrinsicUnit("range", ForRange, 3));

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

            if (this_value.HeapUnitType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.HeapUnitType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.HeapUnitType() == typeof(TableUnit)) FindFunction((TableUnit)this_value.heapUnitValue, relocationInfo);

            return module;
        }

        static void FindFunction(TableUnit table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<StringUnit, Unit> entry in table.table)
                {
                    if (entry.Value.HeapUnitType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.HeapUnitType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.HeapUnitType() == typeof(TableUnit)) FindFunction((TableUnit)entry.Value.heapUnitValue, relocationInfo);

                    relocationInfo.module.TABLE_SET(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ClosureUnit closure, RelocationInfo relocationInfo)
        {
            foreach (UpValueUnit v in closure.upValues)
            {
                if (v.UpValue.HeapUnitType() == typeof(ClosureUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateClosure((ClosureUnit)v.UpValue.heapUnitValue, relocationInfo);
                }
                else if (v.UpValue.HeapUnitType() == typeof(FunctionUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateFunction((FunctionUnit)v.UpValue.heapUnitValue, relocationInfo);
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

                if (new_value.HeapUnitType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value.heapUnitValue);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();

            for (Operand i = 0; i < relocationInfo.toBeRelocatedConstants.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetChunk().GetConstant(relocationInfo.toBeRelocatedConstants[i]);

                relocationInfo.module.constants.Add(new_value);
                relocationInfo.relocatedConstants.Add(relocationInfo.toBeRelocatedConstants[i], (Operand)(relocationInfo.module.constants.Count - 1));

                if (new_value.HeapUnitType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value.heapUnitValue);
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
                        RelocateChunk(((ClosureUnit)this_value.heapUnitValue).function, relocationInfo);
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
                if (entry.Value.HeapUnitType() == typeof(FunctionUnit))
                {
                    FunctionUnit function = (FunctionUnit)entry.Value.heapUnitValue;
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
