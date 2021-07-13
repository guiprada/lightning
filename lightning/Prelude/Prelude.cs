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
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
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
                    mem_use.Set("stack_count", vm.StackCount());
                    mem_use.Set("globals_count", vm.GlobalsCount());
                    mem_use.Set("variables_count", vm.VariablesCount());
                    mem_use.Set("variables_capacity", vm.VariablesCapacity());
                    mem_use.Set("upvalues_count", vm.UpValuesCount());
                    mem_use.Set("upvalue_capacity", vm.UpValueCapacity());

                    return new Unit(mem_use);
                }
                machine.Set("memory_use", new IntrinsicUnit("memory_use", memoryUse, 0));

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

                    return new Unit(modules);
                }
                machine.Set("modules", new IntrinsicUnit("modules", modules, 0));

                //////////////////////////////////////////////////////
                Unit resourcesTrim(VM vm)
                {
                    vm.ResoursesTrim();
                    return new Unit(UnitType.Null);
                }
                machine.Set("trim", new IntrinsicUnit("trim", resourcesTrim, 0));

                //////////////////////////////////////////////////////
                Unit releaseAllVMs(VM vm)
                {
                    VM.ReleaseVMs();
                    return new Unit(UnitType.Null);
                }
                machine.Set("release_all_vms", new IntrinsicUnit("release_all_vms", releaseAllVMs, 0));

                //////////////////////////////////////////////////////
                Unit releaseVMs(VM vm)
                {
                    VM.ReleaseVMs((int)vm.GetNumber(0));
                    return new Unit(UnitType.Null);
                }
                machine.Set("release_vms", new IntrinsicUnit("release_vms", releaseVMs, 1));

                //////////////////////////////////////////////////////
                Unit countVMs(VM vm)
                {
                    return new Unit(VM.CountVMs());
                }
                machine.Set("count_vms", new IntrinsicUnit("count_vms", countVMs, 0));

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
                    Float arity = vm.GetNumber(1);
                    string body = vm.GetString(2);

                    var options = ScriptOptions.Default.AddReferences(
                        typeof(Unit).Assembly,
                        typeof(VM).Assembly).WithImports("lightning", "System");
                    Func<VM, Unit> new_intrinsic = CSharpScript.EvaluateAsync<Func<VM, Unit>>(body, options)
                        .GetAwaiter().GetResult();

                    return new Unit(new IntrinsicUnit(name, new_intrinsic, (int)arity));
                }
                intrinsic.Set("create", new IntrinsicUnit("create", createIntrinsic, 3));
#else
                intrinsic.Set("create", new Unit(UnitType.Null));
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
                    return new Unit(rng.Next(max + 1));
                }
                rand.Set("int", new IntrinsicUnit("int", nextInt, 1));

                Unit nextFloat(VM vm)
                {
                    return new Unit((Float)rng.NextDouble());
                }
                rand.Set("float", new IntrinsicUnit("float", nextFloat, 0));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// Table

            {
                TableUnit table = new TableUnit(null, null);

                Unit TableNew(VM vm)
                {
                    TableUnit new_table = new TableUnit(null, null);
                    new_table.superTable = table;

                    return new Unit(new_table);
                }
                table.Set("new", new IntrinsicUnit("new", TableNew, 0));

                Unit Clone(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<Unit, Unit> table_copy = new Dictionary<Unit, Unit>();
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in this_table.elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    TableUnit copy = new TableUnit(new_list_elements, table_copy);

                    copy.superTable = this_table.superTable;

                    return new Unit(copy);
                }
                table.Set("clone", new IntrinsicUnit("table_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit listNew(VM vm)
                {
                    Integer size = vm.GetInteger(0);
                    TableUnit list = new TableUnit(null, null);
                    for(int i=0; i<size; i++)
                        list.elements.Add(new Unit(UnitType.Null));

                    return new Unit(list);
                }
                table.Set("list_new", new IntrinsicUnit("list_new", listNew, 1));

                //////////////////////////////////////////////////////

                Unit listInit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Integer new_end = vm.GetInteger(1);
                    int size = list.Count;
                    for(int i=size; i<(new_end); i++)
                        list.elements.Add(new Unit(UnitType.Null));

                    return new Unit(UnitType.Null);
                }
                table.Set("list_init", new IntrinsicUnit("list_init", listInit, 2));

                //////////////////////////////////////////////////////
                Unit listPush(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Unit value = vm.GetUnit(1);
                    list.elements.Add(value);

                    return new Unit(UnitType.Null);
                }
                table.Set("push", new IntrinsicUnit("push", listPush, 2));

                //////////////////////////////////////////////////////
                Unit listPop(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Float value = list.elements[^1].floatValue;
                    list.elements.RemoveRange(list.elements.Count - 1, 1);

                    return new Unit(value);
                }
                table.Set("pop", new IntrinsicUnit("pop", listPop, 1));

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
                    return new Unit(value);
                }
                table.Set("list_to_string", new IntrinsicUnit("list_to_string", listToString, 1));

                ////////////////////////////////////////////////////
                Unit listCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.ECount;
                    return new Unit(count);
                }
                table.Set("list_count", new IntrinsicUnit("list_count", listCount, 1));

                //////////////////////////////////////////////////////
                Unit listClear(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.elements.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("list_clear", new IntrinsicUnit("list_clear", listClear, 1));

                //////////////////////////////////////////////////////
                Unit listRemoveRange(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(UnitType.Null);
                }
                table.Set("list_remove", new IntrinsicUnit("list_remove", listRemoveRange, 3));

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

                    return new Unit(new_list);
                }
                table.Set("list_copy", new IntrinsicUnit("list_copy", listCopy, 1));

                //////////////////////////////////////////////////////
                Unit listSplit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    List<Unit> new_list_elements = list.elements.GetRange(range_init, list.elements.Count - range_init);
                    list.elements.RemoveRange(range_init, list.elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                table.Set("list_split", new IntrinsicUnit("list_split", listSplit, 2));

                //////////////////////////////////////////////////////
                Unit listSlice(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);

                    List<Unit> new_list_elements = list.elements.GetRange(range_init, range_end - range_init + 1);
                    list.elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                table.Set("list_slice", new IntrinsicUnit("list_slice", listSlice, 3));
                //////////////////////////////////////////////////////
                Unit listReverse(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.elements.Reverse();

                    return new Unit(UnitType.Null);
                }
                table.Set("list_reverse", new IntrinsicUnit("list_reverse", listReverse, 1));

                //////////////////////////////////////////////////////

                Unit ListMakeIndexesIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.Set("key", i);
                            iterator.Set("value", this_table.elements[i]);
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.Set("next", new IntrinsicUnit("list_index_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                table.Set("list_index_iterator", new IntrinsicUnit("list_index_iterator", ListMakeIndexesIterator, 1));

                //////////////////////////////////////////////////////

                Unit ListMakeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;
                    Unit value = new Unit(UnitType.Null);

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_table.ECount - 1))
                        {
                            i++;
                            iterator.Set("value", this_table.elements[i]);
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.Set("next", new IntrinsicUnit("list_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                table.Set("list_iterator", new IntrinsicUnit("list_iterator", ListMakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit MapCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.TCount;
                    return new Unit(count);
                }
                table.Set("map_count", new IntrinsicUnit("map_count", MapCount, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.Count;
                    return new Unit(count);
                }
                table.Set("count", new IntrinsicUnit("table_count", Count, 1));

                //////////////////////////////////////////////////////
                Unit MapIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (Unit v in this_table.table.Keys)
                    {
                        indexes.elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                table.Set("map_indexes", new IntrinsicUnit("map_indexes", MapIndexes, 1));

                //////////////////////////////////////////////////////
                Unit MapNumericIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (Unit v in this_table.table.Keys)
                    {
                        if(Unit.isNumeric(v))
                            indexes.elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                table.Set("map_numeric_indexes", new IntrinsicUnit("map_numeric_indexes", MapNumericIndexes, 1));

                //////////////////////////////////////////////////////
                Unit MapCopy(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<Unit, Unit> table_copy = new Dictionary<Unit, Unit>();
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    TableUnit copy = new TableUnit(null, table_copy);

                    return new Unit(copy);
                }
                table.Set("map_copy", new IntrinsicUnit("map_copy", MapCopy, 1));

                //////////////////////////////////////////////////////
                Unit MapClear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.table.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("map_clear", new IntrinsicUnit("map_clear", MapClear, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.elements.Clear();
                    this_table.table.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("clear", new IntrinsicUnit("table_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit MapMakeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.table.GetEnumerator();

                    TableUnit iterator = new TableUnit(null, null);
                    iterator.Set("key", new Unit(UnitType.Null));
                    iterator.Set("value", new Unit(UnitType.Null));

                    Unit next(VM vm)
                    {
                        if (enumerator.MoveNext())
                        {
                            iterator.Set("key", (Unit)(enumerator.Key));
                            iterator.Set("value", (Unit)(enumerator.Value));
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };

                    iterator.Set("next", new IntrinsicUnit("map_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                table.Set("map_iterator", new IntrinsicUnit("map_iterator", MapMakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit MapToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    string value = "";
                    bool first = true;
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.table)
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
                    return new Unit(value);
                }
                table.Set("map_to_string", new IntrinsicUnit("map_to_string", MapToString, 1));

                //////////////////////////////////////////////////////
                Unit SetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit super_table = vm.GetTable(1);
                    this_table.superTable = super_table;

                    return new Unit(UnitType.Null);
                }
                table.Set("set_super", new IntrinsicUnit("set_super", SetSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit InsertSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit super_table = vm.GetTable(1);
                    super_table.superTable = this_table.superTable;
                    this_table.superTable = super_table;

                    return new Unit(UnitType.Null);
                }
                table.Set("insert_super", new IntrinsicUnit("insert_super", InsertSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit UnsetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.superTable = null;

                    return new Unit(UnitType.Null);
                }
                table.Set("unset_super", new IntrinsicUnit("unset_super", UnsetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit GetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);

                    return new Unit(this_table.superTable);
                }
                table.Set("get_super", new IntrinsicUnit("get_super", GetSuperTable, 0));

                tables.Add("table", table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// math

            {
                TableUnit math = new TableUnit(null, null);
                math.Set("pi", (Float)Math.PI);
                math.Set("e", (Float)Math.E);
#if DOUBLE
                math.Set("double", true);
#else
                math.Set("double", false);
#endif

                //////////////////////////////////////////////////////
                Unit sin(VM vm)
                {
                    return new Unit((Float)Math.Sin(vm.GetNumber(0)));
                }
                math.Set("sin", new IntrinsicUnit("sin", sin, 1));

                //////////////////////////////////////////////////////
                Unit cos(VM vm)
                {
                    return new Unit((Float)Math.Cos(vm.GetNumber(0)));
                }
                math.Set("cos", new IntrinsicUnit("cos", cos, 1));

                //////////////////////////////////////////////////////
                Unit tan(VM vm)
                {
                    return new Unit((Float)Math.Tan(vm.GetNumber(0)));
                }
                math.Set("tan", new IntrinsicUnit("tan", tan, 1));

                //////////////////////////////////////////////////////
                Unit sec(VM vm)
                {
                    return new Unit((Float)(1 / Math.Cos(vm.GetNumber(0))));
                }
                math.Set("sec", new IntrinsicUnit("sec", sec, 1));

                //////////////////////////////////////////////////////
                Unit cosec(VM vm)
                {
                    return new Unit((Float)(1 / Math.Sin(vm.GetNumber(0))));
                }
                math.Set("cosec", new IntrinsicUnit("cosec", cosec, 1));

                //////////////////////////////////////////////////////
                Unit cotan(VM vm)
                {
                    return new Unit((Float)(1 / Math.Tan(vm.GetNumber(0))));
                }
                math.Set("cotan", new IntrinsicUnit("cotan", cotan, 1));

                //////////////////////////////////////////////////////
                Unit asin(VM vm)
                {
                    return new Unit((Float)Math.Asin(vm.GetNumber(0)));
                }
                math.Set("asin", new IntrinsicUnit("asin", asin, 1));

                //////////////////////////////////////////////////////
                Unit acos(VM vm)
                {
                    return new Unit((Float)Math.Acos(vm.GetNumber(0)));
                }
                math.Set("acos", new IntrinsicUnit("acos", acos, 1));

                //////////////////////////////////////////////////////
                Unit atan(VM vm)
                {
                    return new Unit((Float)Math.Atan(vm.GetNumber(0)));
                }
                math.Set("atan", new IntrinsicUnit("atan", atan, 1));

                //////////////////////////////////////////////////////
                Unit sinh(VM vm)
                {
                    return new Unit((Float)Math.Sinh(vm.GetNumber(0)));
                }
                math.Set("sinh", new IntrinsicUnit("sinh", sinh, 1));

                //////////////////////////////////////////////////////
                Unit cosh(VM vm)
                {
                    return new Unit((Float)Math.Cosh(vm.GetNumber(0)));
                }
                math.Set("cosh", new IntrinsicUnit("cosh", cosh, 1));

                //////////////////////////////////////////////////////
                Unit tanh(VM vm)
                {
                    return new Unit((Float)Math.Tanh(vm.GetNumber(0)));
                }
                math.Set("tanh", new IntrinsicUnit("tanh", tanh, 1));

                //////////////////////////////////////////////////////
                Unit pow(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float exponent = vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, exponent));
                }
                math.Set("pow", new IntrinsicUnit("pow", pow, 2));

                //////////////////////////////////////////////////////
                Unit root(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float exponent = vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, 1 / exponent));
                }
                math.Set("root", new IntrinsicUnit("root", root, 2));

                //////////////////////////////////////////////////////
                Unit sqroot(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Sqrt(value));
                }
                math.Set("sqroot", new IntrinsicUnit("sqroot", sqroot, 1));
                //////////////////////////////////////////////////////
                Unit exp(VM vm)
                {
                    Float exponent = vm.GetNumber(0);
                    return new Unit((Float)Math.Exp(exponent));
                }
                math.Set("exp", new IntrinsicUnit("exp", exp, 1));

                //////////////////////////////////////////////////////
                Unit log(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float this_base = vm.GetNumber(1);
                    return new Unit((Float)Math.Log(value, this_base));
                }
                math.Set("log", new IntrinsicUnit("log", log, 2));

                //////////////////////////////////////////////////////
                Unit ln(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, Math.E));
                }
                math.Set("ln", new IntrinsicUnit("ln", ln, 1));

                //////////////////////////////////////////////////////
                Unit log10(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, (Float)10));
                }
                math.Set("log10", new IntrinsicUnit("log10", log10, 1));

                //////////////////////////////////////////////////////
                Unit mod(VM vm)
                {
                    Float value1 = vm.GetNumber(0);
                    Float value2 = vm.GetNumber(1);
                    return new Unit(value1 % value2);
                }
                math.Set("mod", new IntrinsicUnit("mod", mod, 2));

                //////////////////////////////////////////////////////
                Unit idiv(VM vm)
                {
                    Float value1 = vm.GetNumber(0);
                    Float value2 = vm.GetNumber(1);
                    return new Unit((Integer)(value1 / value2));
                }
                math.Set("idiv", new IntrinsicUnit("idiv", idiv, 2));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                TableUnit time = new TableUnit(null, null);

                Unit now(VM vm)
                {
                    return new Unit(new WrapperUnit<long>(DateTime.Now.Ticks, time));
                }
                time.Set("now", new IntrinsicUnit("now", now, 0));

                Unit ElapsedTime(VM vm)
                {
                    long timeStart = vm.GetWrapperUnit<long>(0);
                    long timeEnd = DateTime.Now.Ticks;
                    return new Unit((Float)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                time.Set("elapsed_time", new IntrinsicUnit("time_elapsed_time", ElapsedTime, 1));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////// string
            {
                TableUnit string_table = new TableUnit(null, null);

                Unit stringSlice(VM vm)
                {
                    string input_string = vm.GetString(0);
                    Float start = vm.GetNumber(1);
                    Float end = vm.GetNumber(2);

                    if (end < input_string.Length)
                    {
                        string result = input_string.Substring((int)start, (int)(end - start));
                        return new Unit(result);
                    }
                    return new Unit(UnitType.Null);
                }
                string_table.Set("slice", new IntrinsicUnit("string_slice", stringSlice, 3));

                //////////////////////////////////////////////////////

                Unit stringSplit(VM vm)
                {
                    StringUnit val_input_string = vm.GetStringUnit(0);
                    string input_string = val_input_string.ToString();
                    Float start = vm.GetNumber(1);
                    if (start < input_string.Length)
                    {
                        Float end = input_string.Length;
                        string result = input_string.Substring((int)start, (int)(end - start));
                        val_input_string.content = input_string.Substring(0, (int)start);
                        return new Unit(result);
                    }
                    return new Unit(UnitType.Null);
                }
                string_table.Set("split", new IntrinsicUnit("string_split", stringSplit, 2));

                //////////////////////////////////////////////////////

                Unit stringLength(VM vm)
                {
                    StringUnit val_input_string = vm.GetStringUnit(0);
                    return new Unit(val_input_string.content.Length);
                }
                string_table.Set("length", new IntrinsicUnit("string_length", stringLength, 1));

                //////////////////////////////////////////////////////

                Unit stringCopy(VM vm)
                {
                    Unit val_input_string = vm.GetUnit(0);
                    if (val_input_string.Type == UnitType.String)
                        return new Unit(val_input_string.ToString());
                    else
                        return new Unit(UnitType.Null);
                }
                string_table.Set("copy", new IntrinsicUnit("string_copy", stringCopy, 1));

                //////////////////////////////////////////////////////
                Unit toList(VM vm)
                {
                    string val_input_string = vm.GetString(0);
                    List<Unit> string_list = new List<Unit>();
                    foreach(char c in val_input_string.ToCharArray()){
                        string_list.Add(new Unit(c));
                    }
                    return new Unit(new TableUnit(string_list, null));
                }
                string_table.Set("to_list", new IntrinsicUnit("string_to_list", toList, 1));

                //////////////////////////////////////////////////////
                Unit charAt(VM vm)
                {
                    Integer index = vm.GetInteger(0);
                    string input_string = vm.GetString(1);
                    if (index < input_string.Length)
                    {
                        char result = input_string[(int)index];
                        return new Unit(result);
                    }
                    return new Unit(UnitType.Null);
                }
                string_table.Set("char_at", new IntrinsicUnit("string_char_at", charAt, 2));

                //////////////////////////////////////////////////////
                Unit Contains(VM vm)
                {
                    string input_string = vm.GetString(0);
                    string contained_string = vm.GetString(1);

                    return new Unit(input_string.Contains(contained_string));
                }
                string_table.Set("contains", new IntrinsicUnit("string_contains", Contains, 2));

                //////////////////////////////////////////////////////
                Unit ContainsChar(VM vm)
                {
                    string input_string = vm.GetString(0);
                    char contained_char = vm.GetChar(1);

                    return new Unit(input_string.Contains(contained_char));
                }
                string_table.Set("contains_char", new IntrinsicUnit("string_contains_char", ContainsChar, 2));

                //////////////////////////////////////////////////////
                Unit NewLine(VM vm)
                {
                    return new Unit(Environment.NewLine);
                }
                string_table.Set("new_line", new IntrinsicUnit("string_new_line", NewLine, 0));

                tables.Add("string", string_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null, null);

                //////////////////////////////////////////////////////

                Unit isAlpha(VM vm)
                {
                    string input_string = vm.GetString(0);
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
                char_table.Set("is_alpha", new IntrinsicUnit("char_is_alpha", isAlpha, 1));

                //////////////////////////////////////////////////////

                Unit isDigit(VM vm)
                {
                    string input_string = vm.GetString(0);
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
                char_table.Set("is_digit", new IntrinsicUnit("char_is_digit", isDigit, 1));

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
                        return new Unit(input);

                    return new Unit(UnitType.Null);
                }
                file.Set("load", new IntrinsicUnit("file_load_file", loadFile, 1));

                //////////////////////////////////////////////////////
                Unit writeFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return new Unit(UnitType.Null);
                }
                file.Set("write", new IntrinsicUnit("file_write_file", writeFile, 2));

                //////////////////////////////////////////////////////
                Unit appendFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }

                    return new Unit(UnitType.Null);
                }
                file.Set("append", new IntrinsicUnit("file_append_file", appendFile, 2));

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
                    return new Unit(UnitType.Null);
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
                    return new Unit(UnitType.Null);
                }
                if (code_generator.HasChunked == true)
                {
                    VM imported_vm = new VM(chunk);
                    VMResult result = imported_vm.Run();
                    if (result.status == VMResultType.OK)
                    {
                        if (result.value.Type == UnitType.Table || result.value.Type == UnitType.Function)
                            MakeModule(result.value, eval_name, vm, imported_vm);
                        return result.value;
                    }
                }
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("eval", eval, 1));

            ////////////////////////////////////////////////////
            Unit require(VM vm)
            {
                string path = vm.GetString(0);
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
                        return new Unit(UnitType.Null);
                    }

                    Node program = parser.ParsedTree;

                    Chunker code_generator = new Chunker(program, path, vm.GetChunk().Prelude);
                    Chunk chunk = code_generator.Code;
                    if (code_generator.Errors.Count > 0)
                    {
                        Console.WriteLine("Code generation had errors:");
                        foreach (string error in code_generator.Errors)
                            Console.WriteLine(error);
                        return new Unit(UnitType.Null);
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
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("require", require, 1));

            //////////////////////////////////////////////////////
            Unit writeLine(VM vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Unescape(vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line", writeLine, 1));

            //////////////////////////////////////////////////////
            Unit write(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Unescape(vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write", write, 1));

            //////////////////////////////////////////////////////
            Unit writeLineRaw(VM vm)
            {
                Console.WriteLine(vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line_raw", writeLineRaw, 1));

            //////////////////////////////////////////////////////
            Unit writeRaw(VM vm)
            {
                Console.Write(vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_raw", writeRaw, 1));

            //////////////////////////////////////////////////////
            Unit readln(VM vm)
            {
                string read = Console.ReadLine();
                return new Unit(read);
            }
            functions.Add(new IntrinsicUnit("read_line", readln, 0));

            //////////////////////////////////////////////////////
            Unit readNumber(VM vm)
            {
                string read = Console.ReadLine();
                if (Float.TryParse(read, out Float n))
                    return new Unit(n);
                else
                    return new Unit(UnitType.Null);
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
                        return new Unit(UnitType.Null);
                    else
                        return new Unit(Char.ToString(next));
                }
                else
                    return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("read", read, 0));

            //////////////////////////////////////////////////////
            Unit type(VM vm)
            {
                UnitType this_type = vm.GetUnit(0).Type;
                return new Unit(this_type.ToString());
            }
            functions.Add(new IntrinsicUnit("type", type, 1));

            //////////////////////////////////////////////////////
            Unit maybe(VM vm)
            {
                Unit first = vm.stack.Peek(0);
                Unit second = vm.stack.Peek(1);
                if (first.Type != UnitType.Null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", maybe, 2));

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
                    args.Add(new Unit(index));
                    args.Add(new Unit(table));
                    vms[index].CallFunction(func, args);
                });
                for (int i = init; i < end; i++)
                {
                    VM.RecycleVM(vms[i]);
                }
                return new Unit(UnitType.Null);
            }

            functions.Add(new IntrinsicUnit("for_each", forEach, 2));
            //////////////////////////////////////////////////////

            Unit ForRange(VM vm)
            {
                Integer n_tasks = vm.GetInteger(0);
                TableUnit table = vm.GetTable(1);
                Unit func = vm.GetUnit(2);


                int init = 0;
                int end = (int)n_tasks;
                VM[] vms = new VM[end];
                for (int i = 0; i < end; i++)
                {
                    vms[i] = vm.GetVM();
                }

                int count = table.ECount;
                int step = (count / (int)n_tasks) + 1;

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
                return new Unit(UnitType.Null);
            }

            functions.Add(new IntrinsicUnit("for_range", ForRange, 3));

            //////////////////////////////////////////////////////
            Unit tuple(VM vm)
            {
                Unit[] tuple = new Unit[2];
                tuple[0] = vm.GetUnit(0);
                tuple[1] = vm.GetUnit(1);

                return new Unit(new WrapperUnit<Unit[]>(tuple));
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

                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("tuple_set_x", setTupleX, 2));

            //////////////////////////////////////////////////////
            Unit setTupleY(VM vm)
            {
                vm.GetWrapperUnit<Unit[]>(0)[1] = vm.GetUnit(1);

                return new Unit(UnitType.Null);
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

                return new Unit(new WrapperUnit<Unit[]>(nuple));
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

                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("nuple_set", setNuple, 3));

            //////////////////////////////////////////////////////
            Unit getOs(VM vm)
            {
                return new Unit(Environment.OSVersion.VersionString);
            }
            functions.Add(new IntrinsicUnit("get_os", getOs, 0));

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

            if (this_value.Type == UnitType.Function) RelocateFunction((FunctionUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.Type == UnitType.Closure) RelocateClosure((ClosureUnit)this_value.heapUnitValue, relocationInfo);
            else if (this_value.Type == UnitType.Table) FindFunction((TableUnit)this_value.heapUnitValue, relocationInfo);

            return module;
        }

        static void FindFunction(TableUnit table, RelocationInfo relocationInfo)
        {
            if (!relocationInfo.relocatedTables.Contains(table.GetHashCode()))
            {
                relocationInfo.relocatedTables.Add(table.GetHashCode());
                foreach (KeyValuePair<Unit, Unit> entry in table.table)
                {
                    if (entry.Value.Type == UnitType.Function) RelocateFunction((FunctionUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.Type == UnitType.Closure) RelocateClosure((ClosureUnit)entry.Value.heapUnitValue, relocationInfo);
                    else if (entry.Value.Type == UnitType.Table) FindFunction((TableUnit)entry.Value.heapUnitValue, relocationInfo);

                    relocationInfo.module.Set(entry.Key, entry.Value);
                }
            }
        }

        static void RelocateClosure(ClosureUnit closure, RelocationInfo relocationInfo)
        {
            foreach (UpValueUnit v in closure.upValues)
            {
                if (v.UpValue.Type == UnitType.Closure/* && relocationInfo.module.name != closure.function.module*/)
                {
                    RelocateClosure((ClosureUnit)v.UpValue.heapUnitValue, relocationInfo);
                }
                else if (v.UpValue.Type == UnitType.Function/* && relocationInfo.module.name != closure.function.module*/)
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

                if (new_value.Type == UnitType.Table) relocation_stack.Add((TableUnit)new_value.heapUnitValue);
            }
            relocationInfo.toBeRelocatedGlobals.Clear();

            for (Operand i = 0; i < relocationInfo.toBeRelocatedConstants.Count; i++)
            {
                Unit new_value = relocationInfo.importedVM.GetChunk().GetConstant(relocationInfo.toBeRelocatedConstants[i]);

                relocationInfo.module.constants.Add(new_value);
                relocationInfo.relocatedConstants.Add(relocationInfo.toBeRelocatedConstants[i], (Operand)(relocationInfo.module.constants.Count - 1));

                if (new_value.Type == UnitType.Table) relocation_stack.Add((TableUnit)new_value.heapUnitValue);
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
            foreach (KeyValuePair<Unit, Unit> entry in module.table)
            {
                if (entry.Value.Type == UnitType.Function)
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
