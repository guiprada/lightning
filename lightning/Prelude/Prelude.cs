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
            ////////////////////////////////////////////////////////////////////////////////////////////////// intrinsic
            {
                TableUnit intrinsic = new TableUnit(null, null);
#if ROSLYN
                Unit createIntrinsic(VM vm)
                {
                    ValString name = (ValString)vm.StackPeek(0).value;
                    Number arity = vm.StackPeek(1).number;
                    ValString val_body = (ValString)vm.StackPeek(2).value;
                    string body = val_body.ToString();

                    var options = ScriptOptions.Default.AddReferences(
                        typeof(Unit).Assembly,
                        typeof(VM).Assembly).WithImports("lightning", "System");
                    Func<VM, Unit> new_intrinsic = CSharpScript.EvaluateAsync<Func<VM, Unit>>(body, options)
                        .GetAwaiter().GetResult();

                    return new Unit(new ValIntrinsic(name.ToString(), new_intrinsic, (int)arity));
                }
                intrinsic.TableSet(new ValString("create"), new Unit(new ValIntrinsic("create", createIntrinsic, 3)));
#else
                intrinsic.TableSet(new StringUnit("create"), new Unit("null"));
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
                    int max = (int)(vm.StackPeek(0)).unitValue;
                    return new Unit(rng.Next(max + 1));
                }
                rand.TableSet(new StringUnit("int"), new Unit(new IntrinsicUnit("int", nextInt, 1)));

                Unit nextFloat(VM vm)
                {
                    return new Unit((Number)rng.NextDouble());
                }
                rand.TableSet(new StringUnit("float"), new Unit(new IntrinsicUnit("float", nextFloat, 0)));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// list

            {
                TableUnit list = new TableUnit(null, null);

                Unit listPush(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    Unit value = vm.StackPeek(1);
                    list.elements.Add(value);

                    return new Unit("null");
                }
                list.TableSet(new StringUnit("push"), new Unit(new IntrinsicUnit("push", listPush, 2)));

                //////////////////////////////////////////////////////
                Unit listPop(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    Number value = list.elements[^1].unitValue;
                    list.elements.RemoveRange(list.elements.Count - 1, 1);

                    return new Unit(value);
                }
                list.TableSet(new StringUnit("pop"), new Unit(new IntrinsicUnit("pop", listPop, 1)));

                //////////////////////////////////////////////////////
                Unit listToString(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
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
                list.TableSet(new StringUnit("to_string"), new Unit(new IntrinsicUnit("to_string", listToString, 1)));

                ////////////////////////////////////////////////////
                Unit listCount(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                    int count = this_table.ECount;
                    return new Unit(count);
                }
                list.TableSet(new StringUnit("count"), new Unit(new IntrinsicUnit("count", listCount, 1)));

                //////////////////////////////////////////////////////
                Unit listClear(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    list.elements.Clear();
                    return new Unit("null");
                }
                list.TableSet(new StringUnit("clear"), new Unit(new IntrinsicUnit("clear", listClear, 1)));

                //////////////////////////////////////////////////////
                Unit listRemoveRange(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    int range_init = (int)vm.StackPeek(1).unitValue;
                    int range_end = (int)vm.StackPeek(2).unitValue;
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit("null");
                }
                list.TableSet(new StringUnit("remove"), new Unit(new IntrinsicUnit("remove", listRemoveRange, 3)));

                //////////////////////////////////////////////////////
                Unit listCopy(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in list.elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TableSet(new StringUnit("copy"), new Unit(new IntrinsicUnit("copy", listCopy, 1)));

                //////////////////////////////////////////////////////
                Unit listSplit(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    int range_init = (int)vm.StackPeek(1).unitValue;
                    List<Unit> new_list_elements = list.elements.GetRange(range_init, list.elements.Count - range_init);
                    list.elements.RemoveRange(range_init, list.elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TableSet(new StringUnit("split"), new Unit(new IntrinsicUnit("split", listSplit, 2)));

                //////////////////////////////////////////////////////
                Unit listSlice(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    int range_init = (int)vm.StackPeek(1).unitValue;
                    int range_end = (int)vm.StackPeek(2).unitValue;

                    List<Unit> new_list_elements = list.elements.GetRange(range_init, range_end - range_init + 1);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                list.TableSet(new StringUnit("slice"), new Unit(new IntrinsicUnit("slice", listSlice, 3)));
                //////////////////////////////////////////////////////
                Unit listReverse(VM vm)
                {
                    TableUnit list = (TableUnit)vm.StackPeek(0).heapValue;
                    list.elements.Reverse();

                    return new Unit("null");
                }
                list.TableSet(new StringUnit("reverse"), new Unit(new IntrinsicUnit("reverse", listReverse, 1)));

                //////////////////////////////////////////////////////

                Unit makeIndexesIterator(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
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
                    iterator.TableSet(new StringUnit("next"), new Unit(new IntrinsicUnit("iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                list.TableSet(new StringUnit("index_iterator"), new Unit(new IntrinsicUnit("list_index_iterator", makeIndexesIterator, 1)));

                //////////////////////////////////////////////////////

                Unit makeIterator(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
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
                    iterator.TableSet(new StringUnit("next"), new Unit(new IntrinsicUnit("iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                list.TableSet(new StringUnit("iterator"), new Unit(new IntrinsicUnit("list_iterator", makeIterator, 1)));

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
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                    int count = this_table.TCount;
                    return new Unit(count);
                }
                table.TableSet(new StringUnit("count"), new Unit(new IntrinsicUnit("table_count", tableCount, 1)));

                //////////////////////////////////////////////////////
                Unit tableIndexes(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (StringUnit v in this_table.table.Keys)
                    {
                        indexes.elements.Add(new Unit(v));
                    }

                    return new Unit(indexes);
                }
                table.TableSet(new StringUnit("indexes"), new Unit(new IntrinsicUnit("indexes", tableIndexes, 1)));

                //////////////////////////////////////////////////////
                Unit tableCopy(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                    Dictionary<StringUnit, Unit> table_copy = new Dictionary<StringUnit, Unit>();
                    foreach (KeyValuePair<StringUnit, Unit> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    TableUnit copy = new TableUnit(null, table_copy);

                    return new Unit(copy);
                }
                table.TableSet(new StringUnit("copy"), new Unit(new IntrinsicUnit("copy", tableCopy, 1)));

                //////////////////////////////////////////////////////
                Unit tableClear(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                    return new Unit("null");
                }
                table.TableSet(new StringUnit("clear"), new Unit(new IntrinsicUnit("clear", tableClear, 1)));

                Unit makeIteratorTable(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
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

                    iterator.TableSet(new StringUnit("next"), new Unit(new IntrinsicUnit("table_iterator_next", next, 0)));
                    return new Unit(iterator);
                }
                table.TableSet(new StringUnit("iterator"), new Unit(new IntrinsicUnit("iterator_table", makeIteratorTable, 1)));

                //////////////////////////////////////////////////////
                Unit tableToString(VM vm)
                {
                    TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
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
                table.TableSet(new StringUnit("to_string"), new Unit(new IntrinsicUnit("table_to_string", tableToString, 1)));

                tables.Add("table", table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                TableUnit math = new TableUnit(null, null);
                math.TableSet(new StringUnit("pi"), new Unit((Number)Math.PI));
                math.TableSet(new StringUnit("e"), new Unit((Number)Math.E));
#if DOUBLE
                math.TableSet(new StringUnit("double"), new Unit(true));
#else
                math.TableSet(new ValString("double"), new Unit(false));
#endif

                //////////////////////////////////////////////////////
                Unit sin(VM vm)
                {
                    return new Unit((Number)Math.Sin(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("sin"), new Unit(new IntrinsicUnit("sin", sin, 1)));

                //////////////////////////////////////////////////////
                Unit cos(VM vm)
                {
                    return new Unit((Number)Math.Cos(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("cos"), new Unit(new IntrinsicUnit("cos", cos, 1)));

                //////////////////////////////////////////////////////
                Unit tan(VM vm)
                {
                    return new Unit((Number)Math.Tan(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("tan"), new Unit(new IntrinsicUnit("tan", tan, 1)));

                //////////////////////////////////////////////////////
                Unit sec(VM vm)
                {
                    return new Unit((Number)(1 / Math.Cos(vm.StackPeek(0).unitValue)));
                }
                math.TableSet(new StringUnit("sec"), new Unit(new IntrinsicUnit("sec", sec, 1)));

                //////////////////////////////////////////////////////
                Unit cosec(VM vm)
                {
                    return new Unit((Number)(1 / Math.Sin(vm.StackPeek(0).unitValue)));
                }
                math.TableSet(new StringUnit("cosec"), new Unit(new IntrinsicUnit("cosec", cosec, 1)));

                //////////////////////////////////////////////////////
                Unit cotan(VM vm)
                {
                    return new Unit((Number)(1 / Math.Tan(vm.StackPeek(0).unitValue)));
                }
                math.TableSet(new StringUnit("cotan"), new Unit(new IntrinsicUnit("cotan", cotan, 1)));

                //////////////////////////////////////////////////////
                Unit asin(VM vm)
                {
                    return new Unit((Number)Math.Asin(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("asin"), new Unit(new IntrinsicUnit("asin", asin, 1)));

                //////////////////////////////////////////////////////
                Unit acos(VM vm)
                {
                    return new Unit((Number)Math.Acos(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("acos"), new Unit(new IntrinsicUnit("acos", acos, 1)));

                //////////////////////////////////////////////////////
                Unit atan(VM vm)
                {
                    return new Unit((Number)Math.Atan(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("atan"), new Unit(new IntrinsicUnit("atan", atan, 1)));

                //////////////////////////////////////////////////////
                Unit sinh(VM vm)
                {
                    return new Unit((Number)Math.Sinh(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("sinh"), new Unit(new IntrinsicUnit("sinh", sinh, 1)));

                //////////////////////////////////////////////////////
                Unit cosh(VM vm)
                {
                    return new Unit((Number)Math.Cosh(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("cosh"), new Unit(new IntrinsicUnit("cosh", cosh, 1)));

                //////////////////////////////////////////////////////
                Unit tanh(VM vm)
                {
                    return new Unit((Number)Math.Tanh(vm.StackPeek(0).unitValue));
                }
                math.TableSet(new StringUnit("tanh"), new Unit(new IntrinsicUnit("tanh", tanh, 1)));

                //////////////////////////////////////////////////////
                Unit pow(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    Number exponent = vm.StackPeek(1).unitValue;
                    return new Unit((Number)Math.Pow(value, exponent));
                }
                math.TableSet(new StringUnit("pow"), new Unit(new IntrinsicUnit("pow", pow, 2)));

                //////////////////////////////////////////////////////
                Unit root(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    Number exponent = vm.StackPeek(1).unitValue;
                    return new Unit((Number)Math.Pow(value, 1 / exponent));
                }
                math.TableSet(new StringUnit("root"), new Unit(new IntrinsicUnit("root", root, 2)));

                //////////////////////////////////////////////////////
                Unit sqroot(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    return new Unit((Number)Math.Sqrt(value));
                }
                math.TableSet(new StringUnit("sqroot"), new Unit(new IntrinsicUnit("sqroot", sqroot, 1)));
                //////////////////////////////////////////////////////
                Unit exp(VM vm)
                {
                    Number exponent = vm.StackPeek(0).unitValue;
                    return new Unit((Number)Math.Exp(exponent));
                }
                math.TableSet(new StringUnit("exp"), new Unit(new IntrinsicUnit("exp", exp, 1)));

                //////////////////////////////////////////////////////
                Unit log(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    Number this_base = vm.StackPeek(1).unitValue;
                    return new Unit((Number)Math.Log(value, this_base));
                }
                math.TableSet(new StringUnit("log"), new Unit(new IntrinsicUnit("log", log, 2)));

                //////////////////////////////////////////////////////
                Unit ln(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    return new Unit((Number)Math.Log(value, Math.E));
                }
                math.TableSet(new StringUnit("ln"), new Unit(new IntrinsicUnit("ln", ln, 1)));

                //////////////////////////////////////////////////////
                Unit log10(VM vm)
                {
                    Number value = vm.StackPeek(0).unitValue;
                    return new Unit((Number)Math.Log(value, (Number)10));
                }
                math.TableSet(new StringUnit("log10"), new Unit(new IntrinsicUnit("log10", log10, 1)));

                //////////////////////////////////////////////////////
                Unit mod(VM vm)
                {
                    Number value1 = vm.StackPeek(0).unitValue;
                    Number value2 = vm.StackPeek(1).unitValue;
                    return new Unit((Number)value1 % value2);
                }
                math.TableSet(new StringUnit("mod"), new Unit(new IntrinsicUnit("mod", mod, 2)));

                //////////////////////////////////////////////////////
                Unit idiv(VM vm)
                {
                    Number value1 = vm.StackPeek(0).unitValue;
                    Number value2 = vm.StackPeek(1).unitValue;
                    return new Unit((Number)(int)(value1 / value2));
                }
                math.TableSet(new StringUnit("idiv"), new Unit(new IntrinsicUnit("idiv", idiv, 2)));

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
                time.TableSet(new StringUnit("now"), new Unit(new IntrinsicUnit("now", now, 0)));

                Unit timeSpan(VM vm)
                {
                    long timeStart = ((WrapperUnit<long>)vm.StackPeek(0).heapValue).UnWrapp();
                    long timeEnd = ((WrapperUnit<long>)vm.StackPeek(1).heapValue).UnWrapp();
                    return new Unit((Number)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                time.TableSet(new StringUnit("span"), new Unit(new IntrinsicUnit("span", timeSpan, 2)));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// string
            {
                TableUnit string_table = new TableUnit(null, null);

                Unit stringSlice(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
                    string input_string = val_input_string.ToString();
                    Number start = vm.StackPeek(1).unitValue;
                    Number end = vm.StackPeek(2).unitValue;

                    if (end < input_string.Length)
                    {
                        string result = input_string.Substring((int)start, (int)(end - start));
                        return new Unit(new StringUnit(result));
                    }
                    return new Unit("null");
                }
                string_table.TableSet(new StringUnit("slice"), new Unit(new IntrinsicUnit("string_slice", stringSlice, 3)));

                //////////////////////////////////////////////////////

                Unit stringSplit(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
                    string input_string = val_input_string.ToString();
                    Number start = vm.StackPeek(1).unitValue;
                    if (start < input_string.Length)
                    {
                        Number end = input_string.Length;
                        string result = input_string.Substring((int)start, (int)(end - start));
                        val_input_string.content = input_string.Substring(0, (int)start);
                        return new Unit(new StringUnit(result));
                    }
                    return new Unit("null");
                }
                string_table.TableSet(new StringUnit("split"), new Unit(new IntrinsicUnit("string_split", stringSplit, 2)));

                //////////////////////////////////////////////////////

                Unit stringLength(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
                    return new Unit(val_input_string.ToString().Length);
                }
                string_table.TableSet(new StringUnit("length"), new Unit(new IntrinsicUnit("string_length", stringLength, 1)));

                //////////////////////////////////////////////////////

                Unit stringCopy(VM vm)
                {
                    Unit val_input_string = vm.StackPeek(0);
                    if (val_input_string.HeapValueType() == typeof(StringUnit))
                        return new Unit(new StringUnit(val_input_string.ToString()));
                    else
                        return new Unit("null");
                }
                string_table.TableSet(new StringUnit("copy"), new Unit(new IntrinsicUnit("string_copy", stringCopy, 1)));

                //////////////////////////////////////////////////////

                tables.Add("string", string_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null, null);
                Unit charAt(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
                    Number index = vm.StackPeek(1).unitValue;
                    string input_string = val_input_string.ToString();
                    if (index < input_string.Length)
                    {
                        char result = input_string[(int)index];
                        return new Unit(new StringUnit(result.ToString()));
                    }
                    return new Unit("null");
                }
                char_table.TableSet(new StringUnit("at"), new Unit(new IntrinsicUnit("char_at", charAt, 2)));

                //////////////////////////////////////////////////////

                Unit isAlpha(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
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
                char_table.TableSet(new StringUnit("is_alpha"), new Unit(new IntrinsicUnit("is_alpha", isAlpha, 1)));

                //////////////////////////////////////////////////////

                Unit isDigit(VM vm)
                {
                    StringUnit val_input_string = (StringUnit)vm.StackPeek(0).heapValue;
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
                char_table.TableSet(new StringUnit("is_digit"), new Unit(new IntrinsicUnit("is_digit", isDigit, 1)));

                tables.Add("char", char_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// file
            {
                TableUnit file = new TableUnit(null, null);
                Unit loadFile(VM vm)
                {
                    string path = (vm.StackPeek(0).heapValue).ToString();
                    string input;
                    using (var sr = new StreamReader(path))
                    {
                        input = sr.ReadToEnd();
                    }
                    if (input != null)
                        return new Unit(new StringUnit(input));

                    return new Unit("null");
                }
                file.TableSet(new StringUnit("load"), new Unit(new IntrinsicUnit("load_file", loadFile, 1)));

                //////////////////////////////////////////////////////
                Unit writeFile(VM vm)
                {
                    string path = (vm.StackPeek(0).heapValue).ToString();
                    string output = (vm.StackPeek(1).heapValue).ToString();
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return new Unit("null");
                }
                file.TableSet(new StringUnit("write"), new Unit(new IntrinsicUnit("write_file", writeFile, 2)));

                //////////////////////////////////////////////////////
                Unit appendFile(VM vm)
                {
                    string path = (vm.StackPeek(0).heapValue).ToString();
                    string output = (vm.StackPeek(1).heapValue).ToString();
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }

                    return new Unit("null");
                }
                file.TableSet(new StringUnit("append"), new Unit(new IntrinsicUnit("append_file", appendFile, 2)));

                tables.Add("file", file);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////// Global Intrinsics

            List<IntrinsicUnit> functions = new List<IntrinsicUnit>();

            //////////////////////////////////////////////////////
            Unit eval(VM vm)
            {
                string eval_code = (vm.StackPeek(0).heapValue).ToString();;
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
                        if (result.value.HeapValueType() == typeof(TableUnit) || result.value.HeapValueType() == typeof(FunctionUnit))
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
                string path = (vm.StackPeek(0).heapValue).ToString();
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
                Console.WriteLine(System.Text.RegularExpressions.Regex.Unescape(vm.StackPeek(0).ToString()));
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("write_line", writeLine, 1));

            //////////////////////////////////////////////////////
            Unit write(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Unescape(vm.StackPeek(0).ToString()));
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
                TableUnit this_table = (TableUnit)vm.StackPeek(0).heapValue;
                int count = this_table.Count;
                return new Unit(count);
            }
            functions.Add(new IntrinsicUnit("count_all", count, 1));

            //////////////////////////////////////////////////////
            Unit type(VM vm)
            {
                Type this_type = vm.StackPeek(0).HeapValueType();
                return new Unit(new StringUnit(this_type.ToString()));
            }
            functions.Add(new IntrinsicUnit("type", type, 1));

            //////////////////////////////////////////////////////
            Unit maybe(VM vm)
            {
                Unit first = vm.StackPeek(0);
                Unit second = vm.StackPeek(1);
                if (first.HeapValueType() != null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", maybe, 2));

            //////////////////////////////////////////////////////
            Unit stats(VM vm)
            {
                return new Unit(new StringUnit(vm.Stats()));
            }
            functions.Add(new IntrinsicUnit("stats", stats, 0));

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
                vm.ReleaseVMs();
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("release_all_vms", releaseAllVMs, 0));

            //////////////////////////////////////////////////////
            Unit releaseVMs(VM vm)
            {
                Unit count = vm.StackPeek(0);
                vm.ReleaseVMs((int)count.unitValue);
                return new Unit("null");
            }
            functions.Add(new IntrinsicUnit("release_vms", releaseVMs, 1));

            //////////////////////////////////////////////////////
            Unit countVMs(VM vm)
            {
                return new Unit(vm.CountVMs());
            }
            functions.Add(new IntrinsicUnit("count_vms", countVMs, 0));

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

            if (this_value.HeapValueType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)this_value.heapValue, relocationInfo);
            else if (this_value.HeapValueType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)this_value.heapValue, relocationInfo);
            else if (this_value.HeapValueType() == typeof(TableUnit)) FindFunction((TableUnit)this_value.heapValue, relocationInfo);

            return module;
        }

        static void FindFunction(TableUnit table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<StringUnit, Unit> entry in table.table)
                {
                    if (entry.Value.HeapValueType() == typeof(FunctionUnit)) RelocateFunction((FunctionUnit)entry.Value.heapValue, relocationInfo);
                    else if (entry.Value.HeapValueType() == typeof(ClosureUnit)) RelocateClosure((ClosureUnit)entry.Value.heapValue, relocationInfo);
                    else if (entry.Value.HeapValueType() == typeof(TableUnit)) FindFunction((TableUnit)entry.Value.heapValue, relocationInfo);

                    relocationInfo.module.TableSet(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ClosureUnit closure, RelocationInfo relocationInfo)
        {
            foreach (UpValueUnit v in closure.upValues)
            {
                if (v.Val.HeapValueType() == typeof(ClosureUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateClosure((ClosureUnit)v.Val.heapValue, relocationInfo);
                }
                else if (v.Val.HeapValueType() == typeof(FunctionUnit)/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateFunction((FunctionUnit)v.Val.heapValue, relocationInfo);
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

                if (new_value.HeapValueType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value.heapValue);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();

            for (Operand i = 0; i < relocationInfo.toBeRelocatedConstants.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetChunk().GetConstant(relocationInfo.toBeRelocatedConstants[i]);

                relocationInfo.module.constants.Add(new_value);
                relocationInfo.relocatedConstants.Add(relocationInfo.toBeRelocatedConstants[i], (Operand)(relocationInfo.module.constants.Count - 1));

                if (new_value.HeapValueType() == typeof(TableUnit)) relocation_stack.Add((TableUnit)new_value.heapValue);
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

                if (next.opCode == OpCode.LOADG)
                {
                    if ((next.opCode == OpCode.LOADG && next.opA >= relocationInfo.importedVM.GetChunk().Prelude.intrinsics.Count))
                    {
                        if (relocationInfo.relocatedGlobals.ContainsKey(next.opA))
                        {
                            next.opCode = OpCode.LOADGI;
                            next.opA = relocationInfo.relocatedGlobals[next.opA];
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else if (relocationInfo.toBeRelocatedGlobals.Contains(next.opA))
                        {
                            next.opCode = OpCode.LOADGI;
                            next.opA = (Operand)(relocationInfo.toBeRelocatedGlobals.IndexOf(next.opA) + relocationInfo.relocatedGlobals.Count);
                            next.opB = relocationInfo.moduleIndex;
                        }
                        else
                        {
                            Operand global_count = (Operand)(relocationInfo.relocatedGlobals.Count + relocationInfo.toBeRelocatedGlobals.Count);
                            relocationInfo.toBeRelocatedGlobals.Add(next.opA);
                            next.opCode = OpCode.LOADGI;
                            next.opA = global_count;
                            next.opB = relocationInfo.moduleIndex;
                        }
                    }
                }
                else if (next.opCode == OpCode.LOADC)
                {
                    if (relocationInfo.relocatedConstants.ContainsKey(next.opA))
                    {
                        next.opCode = OpCode.LOADCI;
                        next.opA = relocationInfo.relocatedConstants[next.opA];
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else if (relocationInfo.toBeRelocatedConstants.Contains(next.opA))
                    {
                        next.opCode = OpCode.LOADCI;
                        next.opA = (Operand)(relocationInfo.toBeRelocatedConstants.IndexOf(next.opA) + relocationInfo.relocatedConstants.Count);
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else
                    {
                        Operand global_count = (Operand)(relocationInfo.relocatedConstants.Count + relocationInfo.toBeRelocatedConstants.Count);
                        relocationInfo.toBeRelocatedConstants.Add(next.opA);
                        next.opCode = OpCode.LOADCI;
                        next.opA = global_count;
                        next.opB = relocationInfo.moduleIndex;
                    }
                }
                else if (next.opCode == OpCode.FUNDCL)
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
                        RelocateChunk(((ClosureUnit)this_value.heapValue).function, relocationInfo);
                    }
                }
                else if (next.opCode == OpCode.LOADGI)
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
                            Console.WriteLine("Can not find LOADGI index" + function.module);
                    }
                }
                else if (next.opCode == OpCode.LOADCI)
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
                            Console.WriteLine("Can not find LOADCI index" + function.module);
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
                if (entry.Value.HeapValueType() == typeof(FunctionUnit))
                {
                    FunctionUnit function = (FunctionUnit)entry.Value.heapValue;
                    for (Operand i = 0; i < function.body.Count; i++)
                    {
                        Instruction next = function.body[i];

                        if (next.opCode == OpCode.LOADGI || next.opCode == OpCode.LOADCI)
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
