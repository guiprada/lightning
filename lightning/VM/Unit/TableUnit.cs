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
        public Dictionary<Unit, Unit> Map{get; private set;}
        public TableUnit SuperTable{get; set;}

        public override UnitType Type{
            get{
                return UnitType.Table;
            }
        }
        public int Count {
            get{
                return Map.Count;
            }
        }
        public TableUnit(Dictionary<Unit, Unit> p_map)
        {
            Map = p_map ??= new Dictionary<Unit, Unit>();
            SuperTable = superTable;
        }
        public TableUnit(Dictionary<Unit, Unit> p_map, TableUnit p_superTable)
        {
            Map = p_map ??= new Dictionary<Unit, Unit>();
            SuperTable = p_superTable;
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
            Map[p_key] = p_value;
        }
        public override Unit Get(Unit p_key){
            return GetTable(p_key);
        }
        public Unit GetTable(Unit p_key){
            Unit this_unit;
            if(Map.TryGetValue(p_key, out this_unit))
                return this_unit;
            else if(SuperTable != null)
                return SuperTable.GetTable(p_key);
            throw new Exception("Table or Super Table does not contain index: " + p_key.ToString());
        }

        public override string ToString()
        {
            string this_string = "table: ";
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
            return this_string;
        }

        public override bool ToBool()
        {
            throw new Exception("Can not convert Table to Bool.");
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
            return Map.GetHashCode();
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
                case UnitType.List:
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
                    foreach (KeyValuePair<Unit, Unit> entry in this_table.Map)
                    {
                        table_copy.Add(entry.Key, entry.Value);
                    }

                    TableUnit copy = new TableUnit(table_copy, this_table.SuperTable);

                    return new Unit(copy);
                }
                superTable.Set("clone", new IntrinsicUnit("table_clone", Clone, 1));

                //////////////////////////////////////////////////////
                Unit Count(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    int count = this_table.Count;
                    return new Unit(count);
                }
                superTable.Set("count", new IntrinsicUnit("table_count", Count, 1));

                //////////////////////////////////////////////////////
                Unit Clear(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.Map.Clear();
                    return new Unit(UnitType.Null);
                }
                superTable.Set("clear", new IntrinsicUnit("table_clear", Clear, 1));

                //////////////////////////////////////////////////////
                Unit ToString(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    string value = "";
                    bool first = true;
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
                superTable.Set("to_string", new IntrinsicUnit("table_to_string", ToString, 1));

                //////////////////////////////////////////////////////
                Unit MakeIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Map.GetEnumerator();

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

                    iterator.Set("next", new IntrinsicUnit("table_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                superTable.Set("iterator", new IntrinsicUnit("table_iterator", MakeIterator, 1));

                //////////////////////////////////////////////////////
                Unit MakeNumericIterator(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    System.Collections.IDictionaryEnumerator enumerator = this_table.Map.GetEnumerator();

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

                    iterator.Set("next", new IntrinsicUnit("table_iterator_next", next, 0));
                    return new Unit(iterator);
                }
                superTable.Set("numeric_iterator", new IntrinsicUnit("table_numeric_iterator", MakeNumericIterator, 1));

                //////////////////////////////////////////////////////
                Unit Indexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    ListUnit indexes = new ListUnit(null);

                    foreach (Unit v in this_table.Map.Keys)
                    {
                        indexes.Elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                superTable.Set("indexes", new IntrinsicUnit("table_indexes", Indexes, 1));

                //////////////////////////////////////////////////////
                Unit NumericIndexes(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    ListUnit indexes = new ListUnit(null);

                    foreach (Unit v in this_table.Map.Keys)
                    {
                        if(Unit.IsNumeric(v))
                            indexes.Elements.Add(v);
                    }

                    return new Unit(indexes);
                }
                superTable.Set("numeric_indexes", new IntrinsicUnit("table_numeric_indexes", NumericIndexes, 1));

                //////////////////////////////////////////////////////
                Unit SetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    TableUnit super_table = vm.GetTable(1);
                    this_table.SuperTable = super_table;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("set_super_table", new IntrinsicUnit("table_set_super_table", SetSuperTable, 2));

                //////////////////////////////////////////////////////
                Unit UnsetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);
                    this_table.SuperTable = null;

                    return new Unit(UnitType.Null);
                }
                superTable.Set("unset_super_table", new IntrinsicUnit("table_unset_super_table", UnsetSuperTable, 1));

                //////////////////////////////////////////////////////
                Unit GetSuperTable(VM vm)
                {
                    TableUnit this_table = vm.GetTable(0);

                    return new Unit(this_table.SuperTable);
                }
                superTable.Set("get_super_table", new IntrinsicUnit("table_get_super_table", GetSuperTable, 1));
            }
        }
    }
}