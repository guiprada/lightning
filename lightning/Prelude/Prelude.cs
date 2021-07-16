using System;
using System.Collections.Generic;
using System.IO;

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

                Unit MemoryUse(VM vm){
                    TableUnit mem_use = new TableUnit(null, null);
                    mem_use.Set("stack_count", vm.StackCount());
                    mem_use.Set("globals_count", vm.GlobalsCount());
                    mem_use.Set("variables_count", vm.VariablesCount());
                    mem_use.Set("variables_capacity", vm.VariablesCapacity());
                    mem_use.Set("upvalues_count", vm.UpValuesCount());
                    mem_use.Set("upvalue_capacity", vm.UpValueCapacity());

                    return new Unit(mem_use);
                }
                machine.Set("memory_use", new IntrinsicUnit("memory_use", MemoryUse, 0));

                //////////////////////////////////////////////////////
                Unit Modules(VM vm)
                {
                    string modules = "";
                    bool first = true;
                    foreach (KeyValuePair<string, int> entry in vm.LoadedModules)
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
                machine.Set("modules", new IntrinsicUnit("modules", Modules, 0));

                //////////////////////////////////////////////////////
                Unit ResourcesTrim(VM vm)
                {
                    vm.ResoursesTrim();
                    return new Unit(UnitType.Null);
                }
                machine.Set("trim", new IntrinsicUnit("trim", ResourcesTrim, 0));

                //////////////////////////////////////////////////////
                Unit ReleaseAllVMs(VM vm)
                {
                    VM.ReleaseVMs();
                    return new Unit(UnitType.Null);
                }
                machine.Set("release_all_vms", new IntrinsicUnit("release_all_vms", ReleaseAllVMs, 0));

                //////////////////////////////////////////////////////
                Unit ReleaseVMs(VM vm)
                {
                    VM.ReleaseVMs((int)vm.GetNumber(0));
                    return new Unit(UnitType.Null);
                }
                machine.Set("release_vms", new IntrinsicUnit("release_vms", ReleaseVMs, 1));

                //////////////////////////////////////////////////////
                Unit CountVMs(VM vm)
                {
                    return new Unit(VM.CountVMs());
                }
                machine.Set("count_vms", new IntrinsicUnit("count_vms", CountVMs, 0));

                tables.Add("machine", machine);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// tuple
            {
                TableUnit tuple = new TableUnit(null, null);
                TableUnit tupleMethods = new TableUnit(null, null);

                Unit TupleNew(VM vm)
                {
                    Unit[] new_tuple = new Unit[2];
                    new_tuple[0] = vm.GetUnit(0);
                    new_tuple[1] = vm.GetUnit(1);

                    WrapperUnit<Unit[]> tuple_object = new WrapperUnit<Unit[]>(new_tuple, tupleMethods);

                    return new Unit(tuple_object);
                }
                tuple.Set("new", new IntrinsicUnit("tuple_new", TupleNew, 2));

                //////////////////////////////////////////////////////
                Unit TupleGetX(VM vm)
                {
                    Unit[] this_tuple = vm.GetWrappedContent<Unit[]>(0);

                    return this_tuple[0];
                }
                tupleMethods.Set("get_x", new IntrinsicUnit("tuple_get_x", TupleGetX, 1));

                //////////////////////////////////////////////////////
                Unit TupleGetY(VM vm)
                {
                    Unit[] this_tuple = vm.GetWrappedContent<Unit[]>(0);

                    return this_tuple[1];
                }
                tupleMethods.Set("get_y", new IntrinsicUnit("tuple_get_y", TupleGetY, 1));

                //////////////////////////////////////////////////////
                Unit TupleSetX(VM vm)
                {
                    vm.GetWrappedContent<Unit[]>(0)[0] = vm.GetUnit(1);

                    return new Unit(UnitType.Null);
                }
                tupleMethods.Set("set_x", new IntrinsicUnit("tuple_set_x", TupleSetX, 2));

                //////////////////////////////////////////////////////
                Unit TupleSetY(VM vm)
                {
                    vm.GetWrappedContent<Unit[]>(0)[1] = vm.GetUnit(1);

                    return new Unit(UnitType.Null);
                }
                tupleMethods.Set("set_y", new IntrinsicUnit("tuple_set_y", TupleSetY, 2));

                tables.Add("tuple", tuple);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// nuple
            {
                TableUnit nuple = new TableUnit(null, null);
                TableUnit nupleMethods = new TableUnit(null, null);

                Unit NupleNew(VM vm)
                {
                    Integer size = vm.GetInteger(0);
                    Unit[] new_nuple = new Unit[size];
                    for(int i = 0; i < size; i++)
                        new_nuple[i] = new Unit(UnitType.Null);
                    WrapperUnit<Unit[]> new_nuple_object = new WrapperUnit<Unit[]>(new_nuple, nupleMethods);

                    return new Unit(new_nuple_object);
                }
                nuple.Set("new", new IntrinsicUnit("tuple_new", NupleNew, 1));

                //////////////////////////////////////////////////////

                Unit NupleFromTable(VM vm)
                {
                    TableUnit table = vm.GetTable(0);
                    int size = table.ECount;
                    Unit[] new_nuple = new Unit[size];
                    for(int i = 0; i < size; i++)
                        new_nuple[i] = table.Elements[i];
                    WrapperUnit<Unit[]> new_nuple_object = new WrapperUnit<Unit[]>(new_nuple, nupleMethods);

                    return new Unit(new_nuple_object);
                }
                nuple.Set("from_table", new IntrinsicUnit("tuple_from_table", NupleFromTable, 1));

                //////////////////////////////////////////////////////
                Unit NupleGet(VM vm)
                {
                    Unit[] this_nuple = vm.GetWrappedContent<Unit[]>(0);

                    return this_nuple[(int)vm.GetNumber(1)];
                }
                nupleMethods.Set("get", new IntrinsicUnit("nuple_get", NupleGet, 2));

                //////////////////////////////////////////////////////
                Unit NupleSet(VM vm)
                {
                    vm.GetWrappedContent<Unit[]>(0)[(int)vm.GetNumber(1)] = vm.GetUnit(2);

                    return new Unit(UnitType.Null);
                }
                nupleMethods.Set("set", new IntrinsicUnit("nuple_set", NupleSet, 3));

                //////////////////////////////////////////////////////
                Unit NupleSize(VM vm)
                {
                    Unit[] this_nuple = vm.GetWrappedContent<Unit[]>(0);

                    return new Unit(this_nuple.Length);
                }
                nupleMethods.Set("size", new IntrinsicUnit("nuple_size", NupleSize, 1));

                tables.Add("nuple", nuple);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////// intrinsic
            {
                TableUnit intrinsic = new TableUnit(null, null);
#if ROSLYN
                Unit CreateIntrinsic(VM vm)
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
                intrinsic.Set("create", new IntrinsicUnit("create", CreateIntrinsic, 3));
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

                Unit NextInt(VM vm)
                {
                    int max = (int)(vm.GetNumber(0));
                    return new Unit(rng.Next(max));
                }
                rand.Set("int", new IntrinsicUnit("int", NextInt, 1));

                Unit NextFloat(VM vm)
                {
                    return new Unit((Float)rng.NextDouble());
                }
                rand.Set("float", new IntrinsicUnit("float", NextFloat, 0));

                tables.Add("rand", rand);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////// Table

            {
                TableUnit table = new TableUnit(null, null);

                Unit TableNew(VM vm)
                {
                    TableUnit new_table = new TableUnit(null, null, table);

                    return new Unit(new_table);
                }
                table.Set("new", new IntrinsicUnit("new", TableNew, 0));

                Unit Clone(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<Unit, Unit> table_copy = new Dictionary<Unit, Unit>();
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Table)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in this_table.Elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    TableUnit copy = new TableUnit(new_list_elements, table_copy, this_table.SuperTable);

                    return new Unit(copy);
                }
                table.Set("clone", new IntrinsicUnit("table_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit ListNew(VM vm)
                {
                    Integer size = vm.GetInteger(0);
                    TableUnit list = new TableUnit(null, null);
                    for(int i=0; i<size; i++)
                        list.Elements.Add(new Unit(UnitType.Null));

                    return new Unit(list);
                }
                table.Set("list_new", new IntrinsicUnit("list_new", ListNew, 1));

                //////////////////////////////////////////////////////

                Unit ListInit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Integer new_end = vm.GetInteger(1);
                    int size = list.Count;
                    for(int i=size; i<(new_end); i++)
                        list.Elements.Add(new Unit(UnitType.Null));

                    return new Unit(UnitType.Null);
                }
                table.Set("list_init", new IntrinsicUnit("list_init", ListInit, 2));

                //////////////////////////////////////////////////////
                Unit ListPush(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Unit value = vm.GetUnit(1);
                    list.Elements.Add(value);

                    return new Unit(UnitType.Null);
                }
                table.Set("push", new IntrinsicUnit("push", ListPush, 2));

                //////////////////////////////////////////////////////
                Unit ListPop(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Float value = list.Elements[^1].floatValue;
                    list.Elements.RemoveRange(list.Elements.Count - 1, 1);

                    return new Unit(value);
                }
                table.Set("pop", new IntrinsicUnit("pop", ListPop, 1));

                //////////////////////////////////////////////////////
                Unit ListToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    bool first = true;
                    string value = "";
                    foreach (Unit v in this_table.Elements)
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
                table.Set("list_to_string", new IntrinsicUnit("list_to_string", ListToString, 1));

                ////////////////////////////////////////////////////
                Unit ListCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.ECount;
                    return new Unit(count);
                }
                table.Set("list_count", new IntrinsicUnit("list_count", ListCount, 1));

                //////////////////////////////////////////////////////
                Unit ListClear(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("list_clear", new IntrinsicUnit("list_clear", ListClear, 1));

                //////////////////////////////////////////////////////
                Unit ListRemoveRange(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);
                    list.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(UnitType.Null);
                }
                table.Set("list_remove", new IntrinsicUnit("list_remove", ListRemoveRange, 3));

                //////////////////////////////////////////////////////
                Unit ListCopy(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in list.Elements)
                    {
                        new_list_elements.Add(v);
                    }
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                table.Set("list_copy", new IntrinsicUnit("list_copy", ListCopy, 1));

                //////////////////////////////////////////////////////
                Unit ListSplit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    List<Unit> new_list_elements = list.Elements.GetRange(range_init, list.Elements.Count - range_init);
                    list.Elements.RemoveRange(range_init, list.Elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                table.Set("list_split", new IntrinsicUnit("list_split", ListSplit, 2));

                //////////////////////////////////////////////////////
                Unit ListSlice(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetNumber(1);
                    int range_end = (int)vm.GetNumber(2);

                    List<Unit> new_list_elements = list.Elements.GetRange(range_init, range_end - range_init + 1);
                    list.Elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                table.Set("list_slice", new IntrinsicUnit("list_slice", ListSlice, 3));
                //////////////////////////////////////////////////////
                Unit ListReverse(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Reverse();

                    return new Unit(UnitType.Null);
                }
                table.Set("list_reverse", new IntrinsicUnit("list_reverse", ListReverse, 1));

                //////////////////////////////////////////////////////
                Unit ListSort(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Sort();

                    return new Unit(UnitType.Null);
                }
                table.Set("list_sort", new IntrinsicUnit("list_sort", ListSort, 1));

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
                            iterator.Set("value", this_table.Elements[i]);
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
                            iterator.Set("value", this_table.Elements[i]);
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

                    foreach (Unit v in this_table.Table.Keys)
                    {
                        indexes.Elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                table.Set("map_indexes", new IntrinsicUnit("map_indexes", MapIndexes, 1));

                //////////////////////////////////////////////////////
                Unit MapNumericIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit indexes = new TableUnit(null, null);

                    foreach (Unit v in this_table.Table.Keys)
                    {
                        if(Unit.IsNumeric(v))
                            indexes.Elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                table.Set("map_numeric_indexes", new IntrinsicUnit("map_numeric_indexes", MapNumericIndexes, 1));

                //////////////////////////////////////////////////////
                Unit MapCopy(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<Unit, Unit> table_copy = new Dictionary<Unit, Unit>();
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Table)
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
                    this_table.Table.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("map_clear", new IntrinsicUnit("map_clear", MapClear, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.Elements.Clear();
                    this_table.Table.Clear();
                    return new Unit(UnitType.Null);
                }
                table.Set("clear", new IntrinsicUnit("table_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit MapMakeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Table.GetEnumerator();

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
                Unit MapMakeNumericIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Table.GetEnumerator();

                    TableUnit iterator = new TableUnit(null, null);
                    iterator.Set("key", new Unit(UnitType.Null));
                    iterator.Set("value", new Unit(UnitType.Null));

                    Unit next(VM vm)
                    {
                        while (true)
                        {
                            if(enumerator.MoveNext()){
                                if(Unit.IsNumeric((Unit)enumerator.Key)){
                                    iterator.Set("key", (Unit)(enumerator.Key));
                                    iterator.Set("value", (Unit)(enumerator.Value));
                                    return new Unit(true);
                                }
                            }else{
                                return new Unit(false);
                            }
                        }
                    };

                    iterator.Set("next", new IntrinsicUnit("map_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                table.Set("map_numeric_iterator", new IntrinsicUnit("map_numeric_iterator", MapMakeNumericIterator, 1));

                //////////////////////////////////////////////////////
                Unit MapToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    string value = "";
                    bool first = true;
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Table)
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
                    this_table.SuperTable = super_table;

                    return new Unit(UnitType.Null);
                }
                table.Set("set_super", new IntrinsicUnit("set_super", SetSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit InsertSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit super_table = vm.GetTable(1);
                    super_table.SuperTable = this_table.SuperTable;
                    this_table.SuperTable = super_table;

                    return new Unit(UnitType.Null);
                }
                table.Set("insert_super", new IntrinsicUnit("insert_super", InsertSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit UnsetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.SuperTable = null;

                    return new Unit(UnitType.Null);
                }
                table.Set("unset_super", new IntrinsicUnit("unset_super", UnsetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit GetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);

                    return new Unit(this_table.SuperTable);
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
                Unit Sin(VM vm)
                {
                    return new Unit((Float)Math.Sin(vm.GetNumber(0)));
                }
                math.Set("sin", new IntrinsicUnit("sin", Sin, 1));

                //////////////////////////////////////////////////////
                Unit Cos(VM vm)
                {
                    return new Unit((Float)Math.Cos(vm.GetNumber(0)));
                }
                math.Set("cos", new IntrinsicUnit("cos", Cos, 1));

                //////////////////////////////////////////////////////
                Unit Tan(VM vm)
                {
                    return new Unit((Float)Math.Tan(vm.GetNumber(0)));
                }
                math.Set("tan", new IntrinsicUnit("tan", Tan, 1));

                //////////////////////////////////////////////////////
                Unit Sec(VM vm)
                {
                    return new Unit((Float)(1 / Math.Cos(vm.GetNumber(0))));
                }
                math.Set("sec", new IntrinsicUnit("sec", Sec, 1));

                //////////////////////////////////////////////////////
                Unit Cosec(VM vm)
                {
                    return new Unit((Float)(1 / Math.Sin(vm.GetNumber(0))));
                }
                math.Set("cosec", new IntrinsicUnit("cosec", Cosec, 1));

                //////////////////////////////////////////////////////
                Unit Cotan(VM vm)
                {
                    return new Unit((Float)(1 / Math.Tan(vm.GetNumber(0))));
                }
                math.Set("cotan", new IntrinsicUnit("cotan", Cotan, 1));

                //////////////////////////////////////////////////////
                Unit Asin(VM vm)
                {
                    return new Unit((Float)Math.Asin(vm.GetNumber(0)));
                }
                math.Set("asin", new IntrinsicUnit("asin", Asin, 1));

                //////////////////////////////////////////////////////
                Unit Acos(VM vm)
                {
                    return new Unit((Float)Math.Acos(vm.GetNumber(0)));
                }
                math.Set("acos", new IntrinsicUnit("acos", Acos, 1));

                //////////////////////////////////////////////////////
                Unit Atan(VM vm)
                {
                    return new Unit((Float)Math.Atan(vm.GetNumber(0)));
                }
                math.Set("atan", new IntrinsicUnit("atan", Atan, 1));

                //////////////////////////////////////////////////////
                Unit Sinh(VM vm)
                {
                    return new Unit((Float)Math.Sinh(vm.GetNumber(0)));
                }
                math.Set("sinh", new IntrinsicUnit("sinh", Sinh, 1));

                //////////////////////////////////////////////////////
                Unit Cosh(VM vm)
                {
                    return new Unit((Float)Math.Cosh(vm.GetNumber(0)));
                }
                math.Set("cosh", new IntrinsicUnit("cosh", Cosh, 1));

                //////////////////////////////////////////////////////
                Unit Tanh(VM vm)
                {
                    return new Unit((Float)Math.Tanh(vm.GetNumber(0)));
                }
                math.Set("tanh", new IntrinsicUnit("tanh", Tanh, 1));

                //////////////////////////////////////////////////////
                Unit Pow(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float exponent = vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, exponent));
                }
                math.Set("pow", new IntrinsicUnit("pow", Pow, 2));

                //////////////////////////////////////////////////////
                Unit Root(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float exponent = vm.GetNumber(1);
                    return new Unit((Float)Math.Pow(value, 1 / exponent));
                }
                math.Set("root", new IntrinsicUnit("root", Root, 2));

                //////////////////////////////////////////////////////
                Unit Sqroot(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Sqrt(value));
                }
                math.Set("sqroot", new IntrinsicUnit("sqroot", Sqroot, 1));
                //////////////////////////////////////////////////////
                Unit Exp(VM vm)
                {
                    Float exponent = vm.GetNumber(0);
                    return new Unit((Float)Math.Exp(exponent));
                }
                math.Set("exp", new IntrinsicUnit("exp", Exp, 1));

                //////////////////////////////////////////////////////
                Unit Log(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    Float this_base = vm.GetNumber(1);
                    return new Unit((Float)Math.Log(value, this_base));
                }
                math.Set("log", new IntrinsicUnit("log", Log, 2));

                //////////////////////////////////////////////////////
                Unit Ln(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, Math.E));
                }
                math.Set("ln", new IntrinsicUnit("ln", Ln, 1));

                //////////////////////////////////////////////////////
                Unit Log10(VM vm)
                {
                    Float value = vm.GetNumber(0);
                    return new Unit((Float)Math.Log(value, (Float)10));
                }
                math.Set("log10", new IntrinsicUnit("log10", Log10, 1));

                //////////////////////////////////////////////////////
                Unit Mod(VM vm)
                {
                    Float value1 = vm.GetNumber(0);
                    Float value2 = vm.GetNumber(1);
                    return new Unit(value1 % value2);
                }
                math.Set("mod", new IntrinsicUnit("mod", Mod, 2));

                //////////////////////////////////////////////////////
                Unit Idiv(VM vm)
                {
                    Float value1 = vm.GetNumber(0);
                    Float value2 = vm.GetNumber(1);
                    return new Unit((Integer)(value1 / value2));
                }
                math.Set("idiv", new IntrinsicUnit("idiv", Idiv, 2));

                tables.Add("math", math);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// time
            {
                TableUnit time = new TableUnit(null, null);
                TableUnit timeMethods = new TableUnit(null, null);

                Unit TimeNow(VM vm)
                {
                    return new Unit(new WrapperUnit<long>(DateTime.Now.Ticks, timeMethods));
                }
                time.Set("now", new IntrinsicUnit("time_now", TimeNow, 0));

                //////////////////////////////////////////////////////
                Unit TimeReset(VM vm)
                {
                    WrapperUnit<long> this_time = vm.GetWrapperUnit<long>(0);
                    this_time.content = DateTime.Now.Ticks;
                    return new Unit(UnitType.Null);
                }
                timeMethods.Set("reset", new IntrinsicUnit("time_reset", TimeReset, 1));

                //////////////////////////////////////////////////////
                Unit TimeElapsed(VM vm)
                {
                    long timeStart = vm.GetWrappedContent<long>(0);
                    long timeEnd = DateTime.Now.Ticks;
                    return new Unit((Integer)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
                }
                timeMethods.Set("elapsed", new IntrinsicUnit("time_elapsed", TimeElapsed, 1));

                tables.Add("time", time);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// char
            {
                TableUnit char_table = new TableUnit(null, null);

                //////////////////////////////////////////////////////

                Unit IsAlpha(VM vm)
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
                char_table.Set("is_alpha", new IntrinsicUnit("char_is_alpha", IsAlpha, 1));

                //////////////////////////////////////////////////////

                Unit IsDigit(VM vm)
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
                char_table.Set("is_digit", new IntrinsicUnit("char_is_digit", IsDigit, 1));

                tables.Add("char", char_table);
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////////// file
            {
                TableUnit file = new TableUnit(null, null);
                Unit LoadFile(VM vm)
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
                file.Set("load", new IntrinsicUnit("file_load_file", LoadFile, 1));

                //////////////////////////////////////////////////////
                Unit WriteFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }

                    return new Unit(UnitType.Null);
                }
                file.Set("write", new IntrinsicUnit("file_write_file", WriteFile, 2));

                //////////////////////////////////////////////////////
                Unit AppendFile(VM vm)
                {
                    string path = vm.GetString(0);
                    string output = vm.GetString(1);
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
            Unit Eval(VM vm)
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
                Chunker code_generator = new Chunker(program, eval_name, vm.Prelude);
                Chunk chunk = code_generator.Chunk;
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
            functions.Add(new IntrinsicUnit("eval", Eval, 1));

            ////////////////////////////////////////////////////
            Unit Require(VM vm)
            {
                string path = vm.GetString(0);
                foreach (ModuleUnit v in vm.modules)// skip already imported modules
                {
                    if (v.Name == path)
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

                    Chunker code_generator = new Chunker(program, path, vm.Prelude);
                    Chunk chunk = code_generator.Chunk;
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
            functions.Add(new IntrinsicUnit("require", Require, 1));

            //////////////////////////////////////////////////////
            Unit Try(VM vm)
            {
                Unit this_callable = vm.GetUnit(0);
                TableUnit this_arguments = vm.GetTable(1);
                VM try_vm = vm.GetVM();
                try{
                    try_vm.CallFunction(this_callable, this_arguments.Elements);
                    VM.RecycleVM(try_vm);
                    return new Unit(true);
                }catch{//(Exception e){
                    // return new Unit(e.ToString());
                    VM.RecycleVM(try_vm);
                    return new Unit(false);
                }
            }
            functions.Add(new IntrinsicUnit("try", Try, 2));

            //////////////////////////////////////////////////////
            Unit WriteLine(VM vm)
            {
                Console.WriteLine(vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line", WriteLine, 1));

            //////////////////////////////////////////////////////
            Unit Write(VM vm)
            {
                Console.Write(vm.GetUnit(0).ToString());
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write", Write, 1));

            //////////////////////////////////////////////////////
            Unit WriteLineRaw(VM vm)
            {
                Console.WriteLine(System.Text.RegularExpressions.Regex.Escape(vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_line_raw", WriteLineRaw, 1));

            //////////////////////////////////////////////////////
            Unit WriteRaw(VM vm)
            {
                Console.Write(System.Text.RegularExpressions.Regex.Escape(vm.GetUnit(0).ToString()));
                return new Unit(UnitType.Null);
            }
            functions.Add(new IntrinsicUnit("write_raw", WriteRaw, 1));

            //////////////////////////////////////////////////////
            Unit Readln(VM vm)
            {
                string read = Console.ReadLine();
                return new Unit(read);
            }
            functions.Add(new IntrinsicUnit("read_line", Readln, 0));

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
            Unit Read(VM vm)
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
            Unit Type(VM vm)
            {
                UnitType this_type = vm.GetUnit(0).Type;
                return new Unit(this_type.ToString());
            }
            functions.Add(new IntrinsicUnit("type", Type, 1));

            //////////////////////////////////////////////////////
            Unit IsNumber(VM vm)
            {
                Unit this_unit = vm.GetUnit(0);
                return new Unit(Unit.IsNumeric(this_unit));
            }
            functions.Add(new IntrinsicUnit("is_number", IsNumber, 1));

            //////////////////////////////////////////////////////
            Unit Maybe(VM vm)
            {
                Unit first = vm.stack.Peek(0);
                Unit second = vm.stack.Peek(1);
                if (first.Type != UnitType.Null)
                    return first;
                else
                    return second;
            }
            functions.Add(new IntrinsicUnit("maybe", Maybe, 2));

            //////////////////////////////////////////////////////
            Unit ForEach(VM vm)
            {
                TableUnit table = vm.GetTable(0);
                Unit func = vm.GetUnit(1);

                int init = 0;
                int end = table.ECount;
                VM[] vms = new VM[end];
                for (int i = init; i < end; i++)
                {
                    vms[i] = vm.GetParallelVM();
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

            functions.Add(new IntrinsicUnit("for_each", ForEach, 2));
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
                    vms[i] = vm.GetParallelVM();
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
            Unit GetOS(VM vm)
            {
                return new Unit(Environment.OSVersion.VersionString);
            }
            functions.Add(new IntrinsicUnit("get_os", GetOS, 0));

            //////////////////////////////////////////////////////
            Unit NewLine(VM vm)
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
                foreach (KeyValuePair<Unit, Unit> entry in table.Table)
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

            for (Operand i = 0; i < relocationInfo.toBeRelocatedConstants.Count; i++)
            {
                Unit new_value =
                    relocationInfo.importedVM.Constants[relocationInfo.toBeRelocatedConstants[i]];

                relocationInfo.module.Constants.Add(new_value);
                relocationInfo.relocatedConstants.Add(
                    relocationInfo.toBeRelocatedConstants[i],
                    (Operand)(relocationInfo.module.Constants.Count - 1));

                if (new_value.Type == UnitType.Table)
                    relocation_stack.Add((TableUnit)new_value.heapUnitValue);
            }
            relocationInfo.toBeRelocatedConstants.Clear();

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
                    if ((next.opCode == OpCode.LOAD_GLOBAL) &&
                        (next.opA >= relocationInfo.importedVM.Prelude.intrinsics.Count))
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
                        next.opA =
                            (Operand)(relocationInfo.toBeRelocatedConstants.IndexOf(next.opA) +
                            relocationInfo.relocatedConstants.Count);
                        next.opB = relocationInfo.moduleIndex;
                    }
                    else
                    {
                        Operand global_count =
                            (Operand)(relocationInfo.relocatedConstants.Count +
                            relocationInfo.toBeRelocatedConstants.Count);
                        relocationInfo.toBeRelocatedConstants.Add(next.opA);
                        next.opCode = OpCode.LOAD_IMPORTED_CONSTANT;
                        next.opA = global_count;
                        next.opB = relocationInfo.moduleIndex;
                    }
                }
                else if (next.opCode == OpCode.DECLARE_FUNCTION)
                {
                    Unit this_value = relocationInfo.importedVM.Constants[next.opC];
                    if (relocationInfo.importingVM.Constants.Contains(this_value))
                    {
                        next.opC =
                            (Operand)relocationInfo.importingVM.Constants.IndexOf(this_value);
                    }
                    else
                    {
                        relocationInfo.importingVM.Constants.Add(this_value);
                        next.opC = (Operand)(relocationInfo.importingVM.Constants.Count - 1);
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
                            if (function.Module == v.Name)
                            {
                                next.opB = v.ImportIndex;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                            Console.WriteLine("Can not find LOAD_IMPORTED_CONSTANT index" + function.Module);
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
                            (next.opCode == OpCode.LOAD_IMPORTED_CONSTANT))
                        {
                            next.opB = new_index;
                            function.Body[i] = next;
                        }
                    }
                }
            }
        }
    }
}
