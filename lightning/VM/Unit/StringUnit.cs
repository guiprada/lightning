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
	public class StringUnit : HeapUnit
    {
        public string content;
        private Dictionary<Unit, Unit> table;
        public override UnitType Type{
            get{
                return UnitType.String;
            }
        }
        public StringUnit(string value)
        {
            content = value;
            table = new Dictionary<Unit, Unit>();
        }
        public override string ToString()
        {
            return content;
        }

        public override bool ToBool()
        {
            throw new Exception("Can not convert String to Bool." + VM.ErrorString(null));
        }

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.String)
                    if(content == ((StringUnit)((Unit)(other)).heapUnitValue).content)
                        return true;
            }
            if(other_type == typeof(StringUnit))
            {
                if (((StringUnit)other).content == content)
                    return true;
            }

            return false;
        }

        public override void Set(Unit index, Unit value)
        {
            table[index] = value;
        }

        public override Unit Get(Unit p_key){
            if(table.ContainsKey(p_key))
                return table[p_key];
            else if(superTable != null)
                return superTable.Get(p_key);
            throw new Exception("String Table or Super Table does not contain index: " + p_key.ToString());
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }

        public override int CompareTo(object compareTo){
            if(compareTo.GetType() != typeof(Unit))
                throw new Exception("Trying to compare StringUnit to non Unit type");
            Unit other = (Unit)compareTo;
            UnitType other_type = other.Type;
            switch(other_type){
                case UnitType.String:
                    return this.content.CompareTo(((StringUnit)(other.heapUnitValue)).content);
                case UnitType.Boolean:
                case UnitType.Char:
                case UnitType.Float:
                case UnitType.Integer:
                    return 1;
                default:
                    return -1;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////// String
        private static TableUnit superTable = new TableUnit(null, null);
        static StringUnit(){
            initSuperTable();
        }
        private static void initSuperTable(){
            Unit StringSlice(VM vm)
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
            superTable.Set("slice", new IntrinsicUnit("string_slice", StringSlice, 3));

            //////////////////////////////////////////////////////

            Unit StringSplit(VM vm)
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
            superTable.Set("split", new IntrinsicUnit("string_split", StringSplit, 2));

            //////////////////////////////////////////////////////

            Unit StringLength(VM vm)
            {
                StringUnit val_input_string = vm.GetStringUnit(0);
                return new Unit(val_input_string.content.Length);
            }
            superTable.Set("length", new IntrinsicUnit("string_length", StringLength, 1));

            //////////////////////////////////////////////////////

            Unit StringCopy(VM vm)
            {
                Unit val_input_string = vm.GetUnit(0);
                if (val_input_string.Type == UnitType.String)
                    return new Unit(val_input_string.ToString());
                else
                    return new Unit(UnitType.Null);
            }
            superTable.Set("copy", new IntrinsicUnit("string_copy", StringCopy, 1));

            //////////////////////////////////////////////////////
            Unit ToList(VM vm)
            {
                string val_input_string = vm.GetString(0);
                List<Unit> string_list = new List<Unit>();
                foreach(char c in val_input_string.ToCharArray()){
                    string_list.Add(new Unit(c));
                }
                return new Unit(new TableUnit(string_list, null));
            }
            superTable.Set("to_list", new IntrinsicUnit("string_to_list", ToList, 1));

            //////////////////////////////////////////////////////
            Unit CharAt(VM vm)
            {
                string input_string = vm.GetString(0);
                Integer index = vm.GetInteger(1);
                if (index < input_string.Length)
                {
                    char result = input_string[(int)index];
                    return new Unit(result);
                }
                return new Unit(UnitType.Null);
            }
            superTable.Set("char_at", new IntrinsicUnit("string_char_at", CharAt, 2));

            //////////////////////////////////////////////////////
            Unit Contains(VM vm)
            {
                string input_string = vm.GetString(0);
                string contained_string = vm.GetString(1);

                return new Unit(input_string.Contains(contained_string));
            }
            superTable.Set("contains", new IntrinsicUnit("string_contains", Contains, 2));

            //////////////////////////////////////////////////////
            Unit ContainsChar(VM vm)
            {
                string input_string = vm.GetString(0);
                char contained_char = vm.GetChar(1);

                return new Unit(input_string.Contains(contained_char));
            }
            superTable.Set("contains_char", new IntrinsicUnit("string_contains_char", ContainsChar, 2));

            //////////////////////////////////////////////////////
            Unit Escape(VM vm)
            {
                string input_string = vm.GetString(0);
                return new Unit(System.Text.RegularExpressions.Regex.Escape(input_string));
            }
            superTable.Set("escape", new IntrinsicUnit("escape", Escape, 1));

            //////////////////////////////////////////////////////
            Unit Unescape(VM vm)
            {
                string input_string = vm.GetString(0);
                return new Unit(System.Text.RegularExpressions.Regex.Unescape(input_string));
            }
            superTable.Set("unescape", new IntrinsicUnit("unescape", Unescape, 1));

        }
    }
}