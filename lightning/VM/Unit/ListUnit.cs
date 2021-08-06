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
    public class ListUnit : HeapUnit
    {
        public List<Unit> Elements{get; private set;}
        public TableUnit SuperTable{get; set;}

        public override UnitType Type{
            get{
                return UnitType.List;
            }
        }
        public int Count {
            get{
                return Elements.Count;
            }
        }
        public ListUnit(List<Unit> p_elements)
        {
            Elements = p_elements ??= new List<Unit>();
            SuperTable = superTable;
        }

        void ElementAdd(Unit value)
        {
            Elements.Add(value);
        }

        public override void Set(Unit p_key, Unit p_value){
            if(p_key.Type != UnitType.Integer)
                throw new Exception("List only supports Integer indexes: " + p_key.ToString());
            Integer index = p_key.integerValue;
            if((index > Count) || (index < 0))
                throw new Exception("List index is out of bounds: " + p_key.ToString());
            ElementSet(p_key, p_value);
        }
        public override Unit Get(Unit p_key){
            UnitType key_type = p_key.Type;
            Integer index = p_key.integerValue;
            switch(key_type){
                case UnitType.Integer:
                    return GetElement(p_key);
                default:
                    return GetTable(p_key);
            }
        }
        Unit GetTable(Unit p_key){
            if(SuperTable != null)
                return SuperTable.GetTable(p_key);
            throw new Exception("List or Super Table does not contain index: " + p_key.ToString());
        }

        Unit GetElement(Unit p_key){
            Integer index = p_key.integerValue;
            if((index >= 0) && (index < Elements.Count))
                return Elements[(int)index];
            throw new Exception("List does not contain index: " + p_key.ToString());
        }

        void ElementSet(Unit p_key, Unit value)
        {
            Integer index = p_key.integerValue;
            if ((index >= 0) && (index <= Elements.Count - 1))
                Elements[(int)index] = value;
            else if (index == Elements.Count){
                ElementAdd(value);
            }
        }

        public override string ToString()
        {
            string this_string = "List: ";
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
                if(((Unit)other).Type == UnitType.List)
                {
                    if (this == ((Unit)other).heapUnitValue as ListUnit) return true;
                }
            }
            if (other_type == typeof(ListUnit))
            {
                if (other == this) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Elements.GetHashCode() + Elements.GetHashCode();
        }

        public override int CompareTo(object compareTo){
            if(compareTo.GetType() != typeof(Unit))
                throw new Exception("Trying to compare a ListUnit to non Unit type");
            Unit other = (Unit)compareTo;
            UnitType other_type = other.Type;
            switch(other_type){
                case UnitType.List:
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
                case UnitType.Table:
                    return -1;
                default:
                    throw new Exception("Trying to compare a ListUnit to unkown UnitType.");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit superTable = new TableUnit(null, null);
        static ListUnit(){
            initSuperTable();
        }
        private TableUnit ExtensionSuperTable {
            get
            {
                if(superTable.SuperTable == null)
                    superTable.SuperTable = new TableUnit(null, null);
                return superTable.SuperTable;
            }
        }
        private static void initSuperTable(){
            {
                //////////////////////////////////////////////////////
                Unit Clone(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    List<Unit> new_list_elements = new List<Unit>();
                    foreach (Unit v in this_list.Elements)
                    {
                        new_list_elements.Add(v);
                    }
                    ListUnit new_list = new ListUnit(new_list_elements);
                    new_list.SuperTable = this_list.SuperTable;

                    return new Unit(new_list);
                }
                superTable.Set("clone", new IntrinsicUnit("list_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int count = this_list.Count;
                    return new Unit(count);
                }
                superTable.Set("count", new IntrinsicUnit("list_count", Count, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Clear();
                    return new Unit(UnitType.Null);
                }
                superTable.Set("clear", new IntrinsicUnit("list_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit ToString(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    bool first = true;
                    string value = "";
                    foreach (Unit v in this_list.Elements)
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
                superTable.Set("to_string", new IntrinsicUnit("list_to_string", ToString, 1));

                //////////////////////////////////////////////////////
                Unit MakeIndexesIterator(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_list.Count - 1))
                        {
                            i++;
                            iterator.Set("key", i);
                            iterator.Set("value", this_list.Elements[i]);
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.Set("next", new IntrinsicUnit("list_index_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                superTable.Set("index_iterator", new IntrinsicUnit("list_index_iterator", MakeIndexesIterator, 1));

                //////////////////////////////////////////////////////
                Unit MakeIterator(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int i = -1;
                    Unit value = new Unit(UnitType.Null);

                    TableUnit iterator = new TableUnit(null, null);
                    Unit next(VM vm)
                    {
                        if (i < (this_list.Count - 1))
                        {
                            i++;
                            iterator.Set("value", this_list.Elements[i]);
                            return new Unit(true);
                        }
                        return new Unit(false);
                    };
                    iterator.Set("next", new IntrinsicUnit("list_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                superTable.Set("iterator", new IntrinsicUnit("list_iterator", MakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit Init(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Integer new_end = vm.GetInteger(1);
                    int size = this_list.Count;

                    for(int i=size; i<new_end; i++)
                        this_list.Elements.Add(new Unit(UnitType.Null));

                    return new Unit(UnitType.Null);
                }
                superTable.Set("init", new IntrinsicUnit("list_init", Init, 2));

                //////////////////////////////////////////////////////
                Unit Push(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Unit value = vm.GetUnit(1);
                    this_list.Elements.Add(value);

                    return new Unit(UnitType.Null);
                }
                superTable.Set("push", new IntrinsicUnit("list_push", Push, 2));

                //////////////////////////////////////////////////////
                Unit Pop(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Float value = this_list.Elements[^1].floatValue;
                    this_list.Elements.RemoveAt(this_list.Elements.Count - 1);

                    return new Unit(value);
                }
                superTable.Set("pop", new IntrinsicUnit("list_pop", Pop, 1));

                //////////////////////////////////////////////////////
                Unit RemoveRange(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);
                    this_list.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(UnitType.Null);
                }
                superTable.Set("remove", new IntrinsicUnit("list_remove", RemoveRange, 3));

                //////////////////////////////////////////////////////
                Unit Split(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int range_init = (int)vm.GetInteger(1);
                    List<Unit> new_list_elements = this_list.Elements.GetRange(range_init, this_list.Elements.Count - range_init);
                    this_list.Elements.RemoveRange(range_init, this_list.Elements.Count - range_init);
                    ListUnit new_list = new ListUnit(new_list_elements);

                    return new Unit(new_list);
                }
                superTable.Set("split", new IntrinsicUnit("list_split", Split, 2));

                //////////////////////////////////////////////////////
                Unit Slice(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);

                    List<Unit> new_list_elements = this_list.Elements.GetRange(range_init, range_end - range_init + 1);
                    this_list.Elements.RemoveRange(range_init, range_end - range_init + 1);
                    ListUnit new_list = new ListUnit(new_list_elements);

                    return new Unit(new_list);
                }
                superTable.Set("slice", new IntrinsicUnit("list_slice", Slice, 3));
                //////////////////////////////////////////////////////
                Unit Reverse(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Reverse();

                    return new Unit(UnitType.Null);
                }
                superTable.Set("reverse", new IntrinsicUnit("list_reverse", Reverse, 1));

                //////////////////////////////////////////////////////
                Unit Sort(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Sort();

                    return new Unit(UnitType.Null);
                }
                superTable.Set("sort", new IntrinsicUnit("list_sort", Sort, 1));

                //////////////////////////////////////////////////////
                var rng = new Random();
                Unit Shuffle(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    List<Unit> list = this_list.Elements;
                    int n = list.Count;
                    for(int i=0; i<list.Count; i++){
                        int rand_position = rng.Next(n);
                        Unit swap = list[rand_position];
                        list[rand_position] = list[i];
                        list[i] = swap;
                    }

                    return new Unit(UnitType.Null);
                }
                superTable.Set("shuffle", new IntrinsicUnit("list_shuffle", Shuffle, 1));

                //////////////////////////////////////////////////////
                Unit SetSuperTable(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    TableUnit super_table = vm.GetTable(1);
                    this_list.SuperTable = super_table;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("set_super_table", new IntrinsicUnit("list_set_super_table", SetSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit GetExtensionSuperTable(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);

                    return new Unit(this_list.ExtensionSuperTable);
                }
                superTable.Set("get_extension_super_table", new IntrinsicUnit("list_get_extension_super_table", GetExtensionSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit UnsetSuperTable(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.SuperTable = null;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("unset_super_table", new IntrinsicUnit("list_unset_super_table", UnsetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit GetSuperTable(VM vm)
                {
                    Unit this_unit = vm.GetUnit(0);
                    if(this_unit.Type == UnitType.List)
                        return new Unit(((ListUnit)this_unit.heapUnitValue).SuperTable);
                    else if (this_unit.Type == UnitType.Table)
                        return new Unit(((TableUnit)this_unit.heapUnitValue).SuperTable);
                    else
                        return new Unit(UnitType.Null);
                }
                superTable.Set("get_super_table", new IntrinsicUnit("list_get_super_table", GetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit Map(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Unit func = vm.GetUnit(1);

                    for(int index=0; index<this_list.Count; index++){
                        List<Unit> args = new List<Unit>();
                        args.Add(new Unit(index));
                        args.Add(new Unit(this_list));
                        vm.ProtectedCallFunction(func, args);
                    }

                    return new Unit(UnitType.Null);
                }

                superTable.Set("map", new IntrinsicUnit("list_map", Map, 2));

                //////////////////////////////////////////////////////
                Unit ParallelMap(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Unit func = vm.GetUnit(1);

                    int init = 0;
                    int end = this_list.Count;
                    VM[] vms = new VM[end];
                    for (int i = init; i < end; i++)
                    {
                        vms[i] = vm.GetParallelVM();
                    }
                    System.Threading.Tasks.Parallel.For(init, end, (index) =>
                    {
                        List<Unit> args = new List<Unit>();
                        args.Add(new Unit(index));
                        args.Add(new Unit(this_list));
                        vms[index].ProtectedCallFunction(func, args);
                    });
                    for (int i = init; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(UnitType.Null);
                }

                superTable.Set("pmap", new IntrinsicUnit("list_pmap", ParallelMap, 2));

                //////////////////////////////////////////////////////
                Unit RangeMap(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Integer n_tasks = vm.GetInteger(1);
                    Unit func = vm.GetUnit(2);


                    int init = 0;
                    int end = (int)n_tasks;
                    VM[] vms = new VM[end];
                    for (int i = 0; i < end; i++)
                    {
                        vms[i] = vm.GetParallelVM();
                    }

                    int count = this_list.Count;
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
                            args.Add(new Unit(this_list));
                            vms[index].ProtectedCallFunction(func, args);
                        }
                    });
                    for (int i = 0; i < end; i++)
                    {
                        vm.RecycleVM(vms[i]);
                    }
                    return new Unit(UnitType.Null);
                }

                superTable.Set("rmap", new IntrinsicUnit("list_rmap", RangeMap, 3));

                //////////////////////////////////////////////////////
                Unit Reduce(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Unit func = vm.GetUnit(1);
                    Unit accumulator = vm.GetUnit(2);

                    for(int index=0; index<this_list.Count; index++){
                        List<Unit> args = new List<Unit>();
                        args.Add(new Unit(index));
                        args.Add(new Unit(this_list));
                        args.Add(accumulator);
                        vm.ProtectedCallFunction(func, args);
                    }

                    return accumulator;
                }

                superTable.Set("reduce", new IntrinsicUnit("list_reduce", Reduce, 2));
            }
        }
    }
}