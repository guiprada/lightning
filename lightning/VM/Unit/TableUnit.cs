using System;
using System.Collections.Generic;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
	public class TableUnit : HeapUnit
    {
        public List<Unit> Elements{get; private set;}
        public Dictionary<Unit, Unit> Table{get; private set;}
        public TableUnit SuperTable{get; set;}

        public override UnitType Type{
            get{
                return UnitType.Table;
            }
        }
        public int ECount {
            get{
                return Elements.Count;
            }
        }
        public int TCount {
            get{
                return Table.Count;
            }
        }
        public int Count {
            get{
                return Elements.Count + Table.Count;
            }
        }
        public TableUnit(List<Unit> p_elements, Dictionary<Unit, Unit> p_table)
        {
            Elements = p_elements ??= new List<Unit>();
            Table = p_table ??= new Dictionary<Unit, Unit>();
            SuperTable = superTable;
        }
        public TableUnit(List<Unit> p_elements, Dictionary<Unit, Unit> p_table, TableUnit p_superTable)
        {
            Elements = p_elements ??= new List<Unit>();
            Table = p_table ??= new Dictionary<Unit, Unit>();
            SuperTable = p_superTable;
        }

        public void Set(Unit p_value){
            ElementAdd(p_value);
        }
        public void Set(string p_key, Unit p_value){
            Set(new Unit(p_key), p_value);
        }
        public void Set(string p_key, Float p_value){
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, Integer p_value){
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, char p_value){
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, bool p_value){
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(string p_key, HeapUnit p_value){
            Set(new Unit(p_key), new Unit(p_value));
        }
        public void Set(Unit p_key, Float p_value){
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, Integer p_value){
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, char p_value){
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, bool p_value){
            Set(p_key, new Unit(p_value));
        }
        public void Set(Unit p_key, HeapUnit p_value){
            Set(p_key, new Unit(p_value));
        }
        public override void Set(Unit p_key, Unit p_value){
            UnitType key_type = p_key.Type;
            switch(key_type){
                case UnitType.Integer:
                    ElementSet(p_key, p_value);
                    break;
                default:
                    TableSet(p_key, p_value);
                    break;
            }
        }
        public override Unit Get(Unit p_key){
            UnitType key_type = p_key.Type;
            switch(key_type){
                case UnitType.Integer:
                    return GetElement(p_key);
                default:
                    return GetTable(p_key);
            }
        }
        Unit GetTable(Unit p_key){
            Unit this_unit;
            if(Table.TryGetValue(p_key, out this_unit))
                return this_unit;
            else if(SuperTable != null)
                return SuperTable.GetTable(p_key);
            throw new Exception("Table or Super Table does not contain index: " + p_key.ToString());
        }

        Unit GetElement(Unit p_key){
            Integer index = p_key.integerValue;
            if((index >= 0) && (index < Elements.Count))
                return Elements[(int)index];
            Unit this_unit;
            if(Table.TryGetValue(p_key, out this_unit))
                    return this_unit;
            throw new Exception("Table does not contain index: " + p_key.ToString());
        }
        void ElementSet(Unit p_key, Unit value)
        {
            Integer index = p_key.integerValue;
            if ((index >= 0) && (index <= Elements.Count - 1))
                Elements[(int)index] = value;
            else if ((index < 0) || (index > Elements.Count))
                TableSet(p_key, value);
            else if (index == Elements.Count){
                Unit this_unit;
                if (Table.TryGetValue(p_key, out this_unit))
                    MoveToList(this_unit, p_key);
                else
                    Elements.Add(value);
            }
        }

        void MoveToList(Unit value, Unit p_key){
            Elements.Add(value);
            Table.Remove(p_key);
        }

        void ElementAdd(Unit value)
        {
            Elements.Add(value);
        }

        void TableSet(Unit index, Unit value)
        {
            Table[index] = value;
        }

        public override string ToString()
        {
            string this_string = "table: ";
            int counter = 0;
            foreach (Unit v in Elements)
            {
                if (counter == 0)
                {
                    this_string += counter + ":" + v.ToString();
                    counter++;
                }
                else
                {
                    this_string += ", " + counter + ":" + v.ToString();
                    counter++;
                }
            }
            if (counter > 0)
                this_string += " ";
            bool first = true;
            foreach (KeyValuePair<Unit, Unit> entry in Table)
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
            return this_string;
        }

        public override bool ToBool()
        {
            throw new Exception("Can not convert List to Bool.");
        }

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.Table)
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
            return Elements.GetHashCode() + Table.GetHashCode();
        }

        public override int CompareTo(object compareTo){
            if(compareTo.GetType() != typeof(Unit))
                throw new Exception("Trying to compare a TableUnit to non Unit type");
            Unit other = (Unit)compareTo;
            UnitType other_type = other.Type;
            switch(other_type){
                case UnitType.Table:
                    return 0;
                case UnitType.Float:
                case UnitType.Integer:
                case UnitType.Char:
                case UnitType.Null:
                case UnitType.Boolean:
                case UnitType.String:
                    return 1;
                case UnitType.Function:
                case UnitType.Intrinsic:
                case UnitType.Closure:
                case UnitType.UpValue:
                case UnitType.Module:
                case UnitType.Wrapper:
                    return -1;
                default:
                    throw new Exception("Trying to compare a TableUnit to unkown UnitType.");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit superTable = new TableUnit(null, null);
        static TableUnit(){
            initSuperTable();
        }
        private static void initSuperTable(){
            {
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
                superTable.Set("clone", new IntrinsicUnit("table_clone", Clone, 1));

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
                superTable.Set("list_copy", new IntrinsicUnit("list_copy", ListCopy, 1));

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
                superTable.Set("map_copy", new IntrinsicUnit("map_copy", MapCopy, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.Count;
                    return new Unit(count);
                }
                superTable.Set("count", new IntrinsicUnit("table_count", Count, 1));

                ////////////////////////////////////////////////////
                Unit ListCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.ECount;
                    return new Unit(count);
                }
                superTable.Set("list_count", new IntrinsicUnit("list_count", ListCount, 1));

                //////////////////////////////////////////////////////
                Unit MapCount(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.TCount;
                    return new Unit(count);
                }
                superTable.Set("map_count", new IntrinsicUnit("map_count", MapCount, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.Elements.Clear();
                    this_table.Table.Clear();
                    return new Unit(UnitType.Null);
                }
                superTable.Set("clear", new IntrinsicUnit("table_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit ListClear(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Clear();
                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_clear", new IntrinsicUnit("list_clear", ListClear, 1));

                //////////////////////////////////////////////////////
                Unit MapClear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.Table.Clear();
                    return new Unit(UnitType.Null);
                }
                superTable.Set("map_clear", new IntrinsicUnit("map_clear", MapClear, 1));

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
                superTable.Set("list_to_string", new IntrinsicUnit("list_to_string", ListToString, 1));

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
                superTable.Set("map_to_string", new IntrinsicUnit("map_to_string", MapToString, 1));

                //////////////////////////////////////////////////////
                Unit ListInit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Integer new_end = vm.GetInteger(1);
                    int size = list.Count;

                    for(int i=size; i<new_end; i++)
                        list.Elements.Add(new Unit(UnitType.Null));

                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_init", new IntrinsicUnit("list_init", ListInit, 2));

                //////////////////////////////////////////////////////
                Unit ListPush(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Unit value = vm.GetUnit(1);
                    list.Elements.Add(value);

                    return new Unit(UnitType.Null);
                }
                superTable.Set("push", new IntrinsicUnit("push", ListPush, 2));

                //////////////////////////////////////////////////////
                Unit ListPop(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    Float value = list.Elements[^1].floatValue;
                    list.Elements.RemoveRange(list.Elements.Count - 1, 1);

                    return new Unit(value);
                }
                superTable.Set("pop", new IntrinsicUnit("pop", ListPop, 1));

                //////////////////////////////////////////////////////
                Unit ListRemoveRange(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);
                    list.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_remove", new IntrinsicUnit("list_remove", ListRemoveRange, 3));

                //////////////////////////////////////////////////////
                Unit ListSplit(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    List<Unit> new_list_elements = list.Elements.GetRange(range_init, list.Elements.Count - range_init);
                    list.Elements.RemoveRange(range_init, list.Elements.Count - range_init);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                superTable.Set("list_split", new IntrinsicUnit("list_split", ListSplit, 2));

                //////////////////////////////////////////////////////
                Unit ListSlice(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);

                    List<Unit> new_list_elements = list.Elements.GetRange(range_init, range_end - range_init + 1);
                    list.Elements.RemoveRange(range_init, range_end - range_init + 1);
                    TableUnit new_list = new TableUnit(new_list_elements, null);

                    return new Unit(new_list);
                }
                superTable.Set("list_slice", new IntrinsicUnit("list_slice", ListSlice, 3));
                //////////////////////////////////////////////////////
                Unit ListReverse(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Reverse();

                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_reverse", new IntrinsicUnit("list_reverse", ListReverse, 1));

                //////////////////////////////////////////////////////
                Unit ListSort(VM vm)
                {
                    TableUnit list = vm.GetTable(0);
                    list.Elements.Sort();

                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_sort", new IntrinsicUnit("list_sort", ListSort, 1));

                //////////////////////////////////////////////////////
                var rng = new Random();
                Unit ListShuffle(VM vm)
                {
                    TableUnit listUnit = vm.GetTable(0);
                    List<Unit> list = listUnit.Elements;
                    int n = list.Count;
                    for(int i=0; i<list.Count; i++){
                        int rand_position = rng.Next(n);
                        Unit swap = list[rand_position];
                        list[rand_position] = list[i];
                        list[i] = swap;
                    }

                    return new Unit(UnitType.Null);
                }
                superTable.Set("list_shuffle", new IntrinsicUnit("list_shuffle", ListShuffle, 1));

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
                superTable.Set("list_index_iterator", new IntrinsicUnit("list_index_iterator", ListMakeIndexesIterator, 1));

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
                superTable.Set("list_iterator", new IntrinsicUnit("list_iterator", ListMakeIterator, 1));

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
                superTable.Set("map_indexes", new IntrinsicUnit("map_indexes", MapIndexes, 1));

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
                superTable.Set("map_numeric_indexes", new IntrinsicUnit("map_numeric_indexes", MapNumericIndexes, 1));

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
                superTable.Set("map_iterator", new IntrinsicUnit("map_iterator", MapMakeIterator, 1));

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
                superTable.Set("map_numeric_iterator", new IntrinsicUnit("map_numeric_iterator", MapMakeNumericIterator, 1));

                //////////////////////////////////////////////////////
                Unit SetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit super_table = vm.GetTable(1);
                    this_table.SuperTable = super_table;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("set_super_table", new IntrinsicUnit("set_super_table", SetSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit UnsetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.SuperTable = null;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("unset_super_table", new IntrinsicUnit("unset_super_table", UnsetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit GetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);

                    return new Unit(this_table.SuperTable);
                }
                superTable.Set("get_super_table", new IntrinsicUnit("get_super_table", GetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit Map(VM vm)
                {
                    TableUnit table = vm.GetTable(0);
                    Unit func = vm.GetUnit(1);

                    for(int index=0; index<table.ECount; index++){
                        List<Unit> args = new List<Unit>();
                        args.Add(new Unit(index));
                        args.Add(new Unit(table));
                        vm.ProtectedCallFunction(func, args);
                    }

                    return new Unit(UnitType.Null);
                }

                superTable.Set("list_map", new IntrinsicUnit("list_map", Map, 2));

                //////////////////////////////////////////////////////
                Unit ParallelMap(VM vm)
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
                        vms[index].ProtectedCallFunction(func, args);
                    });
                    for (int i = init; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(UnitType.Null);
                }

                superTable.Set("list_pmap", new IntrinsicUnit("list_pmap", ParallelMap, 2));

                //////////////////////////////////////////////////////
                Unit RangeMap(VM vm)
                {
                    TableUnit table = vm.GetTable(0);
                    Integer n_tasks = vm.GetInteger(1);
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
                        int range_end = range_start + step;
                        if (range_end > count) range_end = count;

                        for(int i = range_start; i<range_end; i++){
                            args.Clear();
                            args.Add(new Unit((Integer)i));
                            args.Add(new Unit(table));
                            vms[index].ProtectedCallFunction(func, args);
                        }
                    });
                    for (int i = 0; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(UnitType.Null);
                }

                superTable.Set("list_rmap", new IntrinsicUnit("list_rmap", RangeMap, 3));
                //////////////////////////////////////////////////////
                Unit Reduce(VM vm)
                {
                    TableUnit table = vm.GetTable(0);
                    Unit func = vm.GetUnit(1);
                    Unit accumulator = vm.GetUnit(2);

                    for(int index=0; index<table.ECount; index++){
                        List<Unit> args = new List<Unit>();
                        args.Add(new Unit(index));
                        args.Add(new Unit(table));
                        args.Add(accumulator);
                        vm.ProtectedCallFunction(func, args);
                    }

                    return accumulator;
                }

                superTable.Set("list_reduce", new IntrinsicUnit("list_reduce", Reduce, 2));
            }
        }
    }
}