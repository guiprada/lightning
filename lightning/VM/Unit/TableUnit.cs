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
        public TableUnit(List<Unit> p_elements, Dictionary<Unit, Unit> p_table, TableUnit p_superTable = null)
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
                    if(p_key.integerValue >= 0)
                        ElementSet(p_key, p_value);
                    else
                        TableSet(p_key, p_value);
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
                    if(p_key.integerValue >= 0)
                        return GetElement(p_key);
                    else
                        return GetTable(p_key);
                default:
                    return GetTable(p_key);
            }
        }
        Unit GetTable(Unit p_key){
            if(Table.ContainsKey(p_key))
                return Table[p_key];
            else if(SuperTable != null)
                return SuperTable.GetTable(p_key);
            throw new Exception("Table or Super Table does not contain index: " + p_key.ToString());
        }

        Unit GetElement(Unit p_key){
            if(p_key.integerValue < Elements.Count)
                return Elements[(int)p_key.integerValue];
            if(Table.ContainsKey(p_key))
                    return Table[p_key];
            throw new Exception("List does not contain index: " + p_key.ToString());
        }
        void ElementSet(Unit p_key, Unit value)
        {
            Integer index = p_key.integerValue;
            if (index <= Elements.Count - 1)
                Elements[(int)index] = value;
            else if (index > Elements.Count)
                TableSet(p_key, value);
            else if (index == Elements.Count){
                if (Table.ContainsKey(p_key))
                    MoveToList(p_key);
                else
                    Elements.Add(value);
            }
        }

        void MoveToList(Unit p_key){
            Elements.Add(Table[p_key]);
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
    }
}