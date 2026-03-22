using System;
using System.Collections.Generic;

using lightningExceptions;
using lightningTools;
using lightningVM;

namespace lightningUnit
{
    public class TableUnit : HeapUnit
    {
        public List<Unit> Elements { get; private set; }
        public Dictionary<Unit, Unit> Map { get; private set; }
        public TableUnit ExtensionTable { get; set; }
        public override UnitType Type
        {
            get
            {
                return UnitType.Table;
            }
        }
        public int ElemCount => Elements?.Count ?? 0;
        public int MapCount => Map.Count;
        public int Count => ElemCount + MapCount;

        // Constructor for keyed (map-only) tables
        public TableUnit(Dictionary<Unit, Unit> p_map)
        {
            Elements = null;
            Map = p_map ?? new Dictionary<Unit, Unit>();
            ExtensionTable = null;
        }

        // Constructor for positional (list-mode) tables, optionally with a map too
        public TableUnit(List<Unit> p_elements, Dictionary<Unit, Unit> p_map)
        {
            Elements = p_elements ?? new List<Unit>();
            Map = p_map ?? new Dictionary<Unit, Unit>();
            ExtensionTable = null;
        }

        public void Set(string p_key, Unit p_value)
        {
            Set(new Unit(p_key), p_value);
        }
        public void Set(string p_key, Float p_value)
        {
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, Integer p_value)
        {
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, char p_value)
        {
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, bool p_value)
        {
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, HeapUnit p_value)
        {
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(Unit p_key, Float p_value)
        {
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, Integer p_value)
        {
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, char p_value)
        {
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, bool p_value)
        {
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, HeapUnit p_value)
        {
            Set(p_key, new Unit(p_value));
        }
        public override void Set(Unit p_key, Unit p_value)
        {
            if (p_key.Type == UnitType.Integer && Elements != null)
            {
                int idx = (int)p_key.integerValue;
                if (idx >= 0 && idx < Elements.Count)
                {
                    Elements[idx] = p_value;
                    return;
                }
                else if (idx == Elements.Count)
                {
                    Elements.Add(p_value);
                    return;
                }
            }
            Map[p_key] = p_value;
        }
        public override Unit Get(Unit p_key)
        {
            Unit this_unit;

            if (p_key.Type == UnitType.Integer && Elements != null)
            {
                int idx = (int)p_key.integerValue;
                if (idx >= 0 && idx < Elements.Count)
                    return Elements[idx];
            }

            if (Map.TryGetValue(p_key, out this_unit))
                return this_unit;
            else if ((ExtensionTable != null) && ExtensionTable.Map.TryGetValue(p_key, out this_unit))
                return this_unit;
            else if (methodTable.Map.TryGetValue(p_key, out this_unit))
                return this_unit;
            else
            {
                Logger.LogLine("Table does not contain index: " + p_key.ToString(), Defaults.Config.VMLogFile);
                throw Exceptions.not_found;
            }
        }

        public override string ToString()
        {
            string this_string = "";
            if (Elements != null && Elements.Count > 0)
            {
                this_string += "list: ";
                int counter = 0;
                foreach (Unit v in Elements)
                {
                    if (counter == 0)
                    {
                        this_string += counter + ":" + v.ToString();
                    }
                    else
                    {
                        this_string += ", " + counter + ":" + v.ToString();
                    }
                    counter++;
                }
                if (Map.Count > 0) this_string += " | ";
            }
            if (Map.Count > 0)
            {
                this_string += "table: ";
                bool first = true;
                foreach (KeyValuePair<Unit, Unit> entry in Map)
                {
                    if (first)
                    {
                        this_string += '{' + entry.Key.ToString() + ':' + entry.Value + '}';
                        first = false;
                    }
                    else
                    {
                        this_string += ", {" + entry.Key.ToString() + ':' + entry.Value + '}';
                    }
                }
            }
            if (this_string == "") this_string = "table: ";
            return this_string;
        }

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)other).Type == UnitType.Table)
                {
                    if (this == ((Unit)other).heapUnitValue as TableUnit) return true;
                }
            }
            if (other_type == typeof(TableUnit))
            {
                if (other == this) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Map.GetHashCode();
        }

        public override void SetExtensionTable(TableUnit p_ExtensionTable)
        {
            ExtensionTable = p_ExtensionTable;
        }

        public override void UnsetExtensionTable()
        {
            ExtensionTable = null;
        }

        public override TableUnit GetExtensionTable()
        {
            return ExtensionTable;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit methodTable = new TableUnit(null);
        public static TableUnit ClassMethodTable { get { return methodTable; } }
        static TableUnit()
        {
            initMethodTable();
        }
        private static void initMethodTable()
        {
            {
                //////////////////////////////////////////////////////
                // Table methods
                //////////////////////////////////////////////////////

                Unit Clone(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Dictionary<Unit, Unit> table_copy = new Dictionary<Unit, Unit>();
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Map)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }
                    List<Unit> elements_copy = null;
                    if (this_table.Elements != null)
                    {
                        elements_copy = new List<Unit>(this_table.Elements);
                    }

                    TableUnit copy = new TableUnit(elements_copy, table_copy);
                    copy.ExtensionTable = this_table.ExtensionTable;

                    return new Unit(copy);
                }
                methodTable.Set("clone", new IntrinsicUnit("table_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.Count;
                    return new Unit(count);
                }
                methodTable.Set("count", new IntrinsicUnit("table_count", Count, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.Map.Clear();
                    this_table.Elements?.Clear();
                    return new Unit(true);
                }
                methodTable.Set("clear", new IntrinsicUnit("table_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit ToStringTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    string value = "";
                    bool first = true;
                    // List-mode: join Elements as comma-separated values
                    if (this_table.Elements != null)
                    {
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
                    }
                    // Map-mode: join entries as key : value pairs
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Map)
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
                methodTable.Set("to_string", new IntrinsicUnit("table_to_string", ToStringTable, 1));

                //////////////////////////////////////////////////////
                Unit MakeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Map.GetEnumerator();

                    TableUnit iterator = new TableUnit(null);
                    iterator.Set("key", new Unit(UnitType.Void));
                    iterator.Set("value", new Unit(UnitType.Void));

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

                    iterator.Set("next", new IntrinsicUnit("table_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                methodTable.Set("iterator", new IntrinsicUnit("table_iterator", MakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit MakeNumericIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Map.GetEnumerator();

                    TableUnit iterator = new TableUnit(null);
                    iterator.Set("key", new Unit(UnitType.Void));
                    iterator.Set("value", new Unit(UnitType.Void));

                    Unit next(VM vm)
                    {
                        while (true)
                        {
                            if (enumerator.MoveNext())
                            {
                                if (Unit.IsNumeric((Unit)enumerator.Key))
                                {
                                    iterator.Set("key", (Unit)(enumerator.Key));
                                    iterator.Set("value", (Unit)(enumerator.Value));
                                    return new Unit(true);
                                }
                            }
                            else
                            {
                                return new Unit(false);
                            }
                        }
                    };

                    iterator.Set("next", new IntrinsicUnit("table_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                methodTable.Set("numeric_iterator", new IntrinsicUnit("table_numeric_iterator", MakeNumericIterator, 1));

                //////////////////////////////////////////////////////
                Unit Indexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    List<Unit> indexes = new List<Unit>();

                    foreach (Unit v in this_table.Map.Keys)
                    {
                        indexes.Add(v);
                    }

                    return new Unit(new TableUnit(indexes, null));
                }
                methodTable.Set("indexes", new IntrinsicUnit("table_indexes", Indexes, 1));

                //////////////////////////////////////////////////////
                Unit NumericIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    List<Unit> indexes = new List<Unit>();

                    foreach (Unit v in this_table.Map.Keys)
                    {
                        if (Unit.IsNumeric(v))
                            indexes.Add(v);
                    }

                    return new Unit(new TableUnit(indexes, null));
                }
                methodTable.Set("numeric_indexes", new IntrinsicUnit("table_numeric_indexes", NumericIndexes, 1));

                //////////////////////////////////////////////////////
                Unit SetExtensionTable(VM vm)
                {
                    Unit this_unit = vm.GetUnit(0);
                    TableUnit extension_table = vm.GetTable(1);

                    if (extension_table.GetExtensionTable() != null)
                    {
                        Logger.LogLine("Extension Table has an Extention Table!", Defaults.Config.VMLogFile);
                        throw Exceptions.extension_table_has_extension_table;
                    }
                    if (this_unit.heapUnitValue.GetExtensionTable() != null)
                    {
                        Logger.LogLine("Table already has an Extention Table!", Defaults.Config.VMLogFile);
                        throw Exceptions.can_not_override_extension_table;
                    }

                    this_unit.heapUnitValue.SetExtensionTable(extension_table);
                    return new Unit(true);
                }
                methodTable.Set("set_extension_table", new IntrinsicUnit("table_set_extension_table", SetExtensionTable, 2));

                //////////////////////////////////////////////////////
                Unit UnsetExtensionTable(VM vm)
                {
                    Unit this_unit = vm.GetUnit(0);
                    this_unit.heapUnitValue.UnsetExtensionTable();

                    return new Unit(true);
                }
                methodTable.Set("unset_extension_table", new IntrinsicUnit("table_unset_extension_table", UnsetExtensionTable, 1));

                //////////////////////////////////////////////////////
                Unit GetExtensionTable(VM vm)
                {
                    Unit this_unit = vm.GetUnit(0);
                    TableUnit extension_table = this_unit.heapUnitValue.GetExtensionTable();
                    if (extension_table == null)
                        return new Unit(new OptionUnit());

                    return new Unit(new OptionUnit(new Unit(extension_table)));
                }
                methodTable.Set("get_extension_table", new IntrinsicUnit("table_get_extension_table", GetExtensionTable, 1));

                //////////////////////////////////////////////////////
                Unit Merge(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit merging_table = vm.GetTable(1);

                    foreach (KeyValuePair<Unit, Unit> u in merging_table.Map)
                    {
                        if (!this_table.Map.ContainsKey(u.Key))
                            this_table.Map.Add(u.Key, u.Value);
                    }
                    return new Unit(true);
                }
                methodTable.Set("merge", new IntrinsicUnit("merge", Merge, 2));

                //////////////////////////////////////////////////////
                // List methods (merged from ListUnit)
                //////////////////////////////////////////////////////

                Unit ToStringList(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    if (this_table.Elements == null) return new Unit("");
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
                methodTable.Set("list_to_string", new IntrinsicUnit("list_to_string", ToStringList, 1));

                //////////////////////////////////////////////////////
                Unit MakeIndexesIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null);
                    Unit next(VM vm)
                    {
                        int count = this_table.ElemCount;
                        if (i < (count - 1))
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
                methodTable.Set("index_iterator", new IntrinsicUnit("list_index_iterator", MakeIndexesIterator, 1));

                //////////////////////////////////////////////////////
                Unit MakeListIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null);
                    Unit next(VM vm)
                    {
                        int count = this_table.ElemCount;
                        if (i < (count - 1))
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
                methodTable.Set("list_iterator", new IntrinsicUnit("list_iterator", MakeListIterator, 1));

                //////////////////////////////////////////////////////
                Unit Init(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Integer new_end = vm.GetInteger(1);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("init called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    int size = this_table.Elements.Count;

                    for (int i = size; i < new_end; i++)
                        this_table.Elements.Add(new Unit(new OptionUnit()));

                    return new Unit(true);
                }
                methodTable.Set("init", new IntrinsicUnit("list_init", Init, 2));

                //////////////////////////////////////////////////////
                Unit Push(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Unit value = vm.GetUnit(1);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("push called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    this_table.Elements.Add(value);

                    return new Unit(true);
                }
                methodTable.Set("push", new IntrinsicUnit("list_push", Push, 2));

                //////////////////////////////////////////////////////
                Unit Pop(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    if (this_table.Elements == null || this_table.Elements.Count == 0)
                    {
                        Logger.LogLine("pop called on empty or map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.out_of_bounds;
                    }
                    Float value = this_table.Elements[^1].floatValue;
                    this_table.Elements.RemoveAt(this_table.Elements.Count - 1);

                    return new Unit(value);
                }
                methodTable.Set("pop", new IntrinsicUnit("list_pop", Pop, 1));

                //////////////////////////////////////////////////////
                Unit RemoveRange(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("remove called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    this_table.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(true);
                }
                methodTable.Set("remove", new IntrinsicUnit("list_remove", RemoveRange, 3));

                //////////////////////////////////////////////////////
                Unit Split(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("split called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    List<Unit> new_elements = this_table.Elements.GetRange(range_init, this_table.Elements.Count - range_init);
                    this_table.Elements.RemoveRange(range_init, this_table.Elements.Count - range_init);

                    return new Unit(new TableUnit(new_elements, null));
                }
                methodTable.Set("split", new IntrinsicUnit("list_split", Split, 2));

                //////////////////////////////////////////////////////
                Unit Slice(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("slice called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    List<Unit> new_elements = this_table.Elements.GetRange(range_init, range_end - range_init + 1);
                    this_table.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(new TableUnit(new_elements, null));
                }
                methodTable.Set("slice", new IntrinsicUnit("list_slice", Slice, 3));

                //////////////////////////////////////////////////////
                Unit Reverse(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("reverse called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    this_table.Elements.Reverse();

                    return new Unit(true);
                }
                methodTable.Set("reverse", new IntrinsicUnit("list_reverse", Reverse, 1));

                //////////////////////////////////////////////////////
                Unit Sort(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("sort called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    this_table.Elements.Sort();

                    return new Unit(true);
                }
                methodTable.Set("sort", new IntrinsicUnit("list_sort", Sort, 1));

                //////////////////////////////////////////////////////
                var rng = new Random();
                Unit Shuffle(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("shuffle called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    List<Unit> list = this_table.Elements;
                    int n = list.Count;
                    for (int i = 0; i < list.Count; i++)
                    {
                        int rand_position = rng.Next(n);
                        Unit swap = list[rand_position];
                        list[rand_position] = list[i];
                        list[i] = swap;
                    }

                    return new Unit(true);
                }
                methodTable.Set("shuffle", new IntrinsicUnit("list_shuffle", Shuffle, 1));

                //////////////////////////////////////////////////////
                Unit Map(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Unit func = vm.GetUnit(1);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("map called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }

                    for (int index = 0; index < this_table.Elements.Count; index++)
                    {
                        List<Unit> args = new List<Unit>();
                        Unit index_unit = new Unit(index);
                        args.Add(this_table.Elements[index]);
                        args.Add(index_unit);
                        Unit result = vm.ProtectedCallFunction(func, args);
                        this_table.Elements[index] = result;
                    }

                    return new Unit(true);
                }
                methodTable.Set("map", new IntrinsicUnit("list_map", Map, 2));

                //////////////////////////////////////////////////////
                Unit ParallelMap(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Unit func = vm.GetUnit(1);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("pmap called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }

                    int init = 0;
                    int end = this_table.Elements.Count;
                    VM[] vms = new VM[end];
                    for (int i = init; i < end; i++)
                    {
                        vms[i] = vm.GetParallelVM();
                    }
                    System.Threading.Tasks.Parallel.For(init, end, (index) =>
                    {
                        List<Unit> args = new List<Unit>();
                        Unit index_unit = new Unit(index);
                        args.Add(this_table.Elements[index]);
                        args.Add(index_unit);
                        Unit result = vms[index].ProtectedCallFunction(func, args);
                        this_table.Elements[index] = result;
                    });
                    for (int i = init; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(true);
                }
                methodTable.Set("pmap", new IntrinsicUnit("list_pmap", ParallelMap, 2));

                //////////////////////////////////////////////////////
                Unit RangeMap(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Integer n_tasks = vm.GetInteger(1);
                    Unit func = vm.GetUnit(2);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("rmap called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }

                    int init = 0;
                    int end = (int)n_tasks;
                    VM[] vms = new VM[end];
                    for (int i = 0; i < end; i++)
                    {
                        vms[i] = vm.GetParallelVM();
                    }

                    int count = this_table.Elements.Count;
                    int step = (count / (int)n_tasks) + 1;

                    System.Threading.Tasks.Parallel.For(init, end, (index) =>
                    {
                        List<Unit> args = new List<Unit>();
                        int range_start = index * step;
                        int range_end = range_start + step;
                        if (range_end > count) range_end = count;

                        for (int i = range_start; i < range_end; i++)
                        {
                            args.Clear();
                            Unit index_unit = new Unit((Integer)i);
                            args.Add(this_table.Elements[i]);
                            args.Add(index_unit);
                            Unit result = vms[index].ProtectedCallFunction(func, args);
                            this_table.Elements[i] = result;
                        }
                    });
                    for (int i = 0; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(true);
                }
                methodTable.Set("rmap", new IntrinsicUnit("list_rmap", RangeMap, 3));

                //////////////////////////////////////////////////////
                Unit Reduce(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    Unit func = vm.GetUnit(1);
                    Unit accumulator = vm.GetUnit(2);
                    if (this_table.Elements == null)
                    {
                        Logger.LogLine("reduce called on a map-only table", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }

                    for (int index = 0; index < this_table.Elements.Count; index++)
                    {
                        List<Unit> args = new List<Unit>();
                        Unit index_unit = new Unit(index);
                        args.Add(this_table.Elements[index]);
                        args.Add(index_unit);
                        args.Add(accumulator);
                        vm.ProtectedCallFunction(func, args);
                    }

                    return accumulator;
                }
                methodTable.Set("reduce", new IntrinsicUnit("list_reduce", Reduce, 2));
            }
        }
    }
}
