using System;
using System.Collections.Generic;

using lightningVM;
namespace lightningUnit
{
    public class ListUnit : HeapUnit
    {
        public List<Unit> Elements { get; private set; }
        public TableUnit MethodTable { get; set; }

        public override UnitType Type
        {
            get
            {
                return UnitType.List;
            }
        }
        public int Count
        {
            get
            {
                return Elements.Count;
            }
        }
        public ListUnit(List<Unit> p_elements)
        {
            Elements = p_elements ??= new List<Unit>();
            MethodTable = methodTable;
        }

        void ElementAdd(Unit value)
        {
            Elements.Add(value);
        }

        public override void Set(Unit p_key, Unit p_value)
        {
            if (p_key.Type != UnitType.Integer)
                throw new Exception("List only supports Integer indexes: " + p_key.ToString());
            Integer index = p_key.integerValue;
            if ((index > Count) || (index < 0))
                throw new Exception("List index is out of bounds: " + p_key.ToString());
            ElementSet(p_key, p_value);
        }
        public override Unit Get(Unit p_key)
        {
            UnitType key_type = p_key.Type;
            Integer index = p_key.integerValue;
            switch (key_type)
            {
                case UnitType.Integer:
                    return GetElement(p_key);
                default:
                    return GetTable(p_key);
            }
        }
        Unit GetTable(Unit p_key)
        {
            if (MethodTable != null)
                return MethodTable.Get(p_key);
            throw new Exception("List does not contain a Method Table: " + p_key.ToString());
        }

        Unit GetElement(Unit p_key)
        {
            Integer index = p_key.integerValue;
            if ((index >= 0) && (index < Elements.Count))
                return Elements[(int)index];
            throw new Exception("List does not contain index: " + p_key.ToString());
        }

        void ElementSet(Unit p_key, Unit value)
        {
            Integer index = p_key.integerValue;
            if ((index >= 0) && (index <= Elements.Count - 1))
                Elements[(int)index] = value;
            else if (index == Elements.Count)
            {
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

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)other).Type == UnitType.List)
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

        public override void SetExtensionTable(TableUnit p_ExtensionTable)
        {
            MethodTable.ExtensionTable = p_ExtensionTable;
        }

        public override void UnsetExtensionTable()
        {
            MethodTable.ExtensionTable = null;
        }

        public override TableUnit GetExtensionTable()
        {
            return MethodTable.ExtensionTable;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit methodTable = new TableUnit(null);
        public static TableUnit ClassMethodTable { get { return methodTable; } }
        static ListUnit()
        {
            initMethodTable();
        }
        private static void initMethodTable()
        {
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
                    new_list.MethodTable = this_list.MethodTable;

                    return new Unit(new_list);
                }
                methodTable.Set("clone", new IntrinsicUnit("list_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int count = this_list.Count;
                    return new Unit(count);
                }
                methodTable.Set("count", new IntrinsicUnit("list_count", Count, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Clear();
                    return new Unit(true);
                }
                methodTable.Set("clear", new IntrinsicUnit("list_clear", Clear, 1));

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
                methodTable.Set("to_string", new IntrinsicUnit("list_to_string", ToString, 1));

                //////////////////////////////////////////////////////
                Unit MakeIndexesIterator(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null);
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
                methodTable.Set("index_iterator", new IntrinsicUnit("list_index_iterator", MakeIndexesIterator, 1));

                //////////////////////////////////////////////////////
                Unit MakeIterator(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int i = -1;

                    TableUnit iterator = new TableUnit(null);
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
                methodTable.Set("iterator", new IntrinsicUnit("list_iterator", MakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit Init(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Integer new_end = vm.GetInteger(1);
                    int size = this_list.Count;

                    for (int i = size; i < new_end; i++)
                        this_list.Elements.Add(new Unit(new OptionUnit()));

                    return new Unit(true);
                }
                methodTable.Set("init", new IntrinsicUnit("list_init", Init, 2));

                //////////////////////////////////////////////////////
                Unit Push(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Unit value = vm.GetUnit(1);
                    this_list.Elements.Add(value);

                    return new Unit(true);
                }
                methodTable.Set("push", new IntrinsicUnit("list_push", Push, 2));

                //////////////////////////////////////////////////////
                Unit Pop(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    Float value = this_list.Elements[^1].floatValue;
                    this_list.Elements.RemoveAt(this_list.Elements.Count - 1);

                    return new Unit(value);
                }
                methodTable.Set("pop", new IntrinsicUnit("list_pop", Pop, 1));

                //////////////////////////////////////////////////////
                Unit RemoveRange(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    int range_init = (int)vm.GetInteger(1);
                    int range_end = (int)vm.GetInteger(2);
                    this_list.Elements.RemoveRange(range_init, range_end - range_init + 1);

                    return new Unit(true);
                }
                methodTable.Set("remove", new IntrinsicUnit("list_remove", RemoveRange, 3));

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
                methodTable.Set("split", new IntrinsicUnit("list_split", Split, 2));

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
                methodTable.Set("slice", new IntrinsicUnit("list_slice", Slice, 3));
                //////////////////////////////////////////////////////
                Unit Reverse(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Reverse();

                    return new Unit(true);
                }
                methodTable.Set("reverse", new IntrinsicUnit("list_reverse", Reverse, 1));

                //////////////////////////////////////////////////////
                Unit Sort(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    this_list.Elements.Sort();

                    return new Unit(true);
                }
                methodTable.Set("sort", new IntrinsicUnit("list_sort", Sort, 1));

                //////////////////////////////////////////////////////
                var rng = new Random();
                Unit Shuffle(VM vm)
                {
                    ListUnit this_list = vm.GetList(0);
                    List<Unit> list = this_list.Elements;
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
                    ListUnit this_list = vm.GetList(0);
                    Unit func = vm.GetUnit(1);

                    for (int index = 0; index < this_list.Count; index++)
                    {
                        List<Unit> args = new List<Unit>();

                        Unit index_unit = new Unit(index);
                        args.Add(this_list.GetElement(index_unit));
                        args.Add(index_unit);
                        Unit result = vm.ProtectedCallFunction(func, args);
                        this_list.ElementSet(index_unit, result);
                    }

                    return new Unit(true);
                }

                methodTable.Set("map", new IntrinsicUnit("list_map", Map, 2));

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
                        Unit index_unit = new Unit(index);
                        args.Add(this_list.GetElement(index_unit));
                        args.Add(index_unit);
                        Unit result = vms[index].ProtectedCallFunction(func, args);
                        this_list.ElementSet(index_unit, result);
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

                        for (int i = range_start; i < range_end; i++)
                        {
                            args.Clear();
                            Unit index_unit = new Unit((Integer)i);
                            args.Add(this_list.GetElement(index_unit));
                            args.Add(index_unit);
                            Unit result = vms[index].ProtectedCallFunction(func, args);
                            this_list.ElementSet(index_unit, result);
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
                    ListUnit this_list = vm.GetList(0);
                    Unit func = vm.GetUnit(1);
                    Unit accumulator = vm.GetUnit(2);

                    for (int index = 0; index < this_list.Count; index++)
                    {
                        List<Unit> args = new List<Unit>();
                        Unit index_unit = new Unit(index);
                        args.Add(this_list.GetElement(index_unit));
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