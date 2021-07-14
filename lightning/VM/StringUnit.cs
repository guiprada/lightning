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

        public override UnitType Type{
            get{
                return UnitType.String;
            }
        }

        static StringUnit(){
            initSuperTable();
        }
        public StringUnit(string value)
        {
            content = value;
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

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }

        public override Unit Get(Unit p_key){
            if(superTable.table.ContainsKey(p_key))
                return superTable.table[p_key];
            else
            throw new Exception("StringUnit Super Table does not contain index: " + p_key.ToString());
        }

        public void Set(Unit p_key, Unit p_value){
            superTable.table.Add(p_key, p_value);
        }

        private static TableUnit superTable = new TableUnit(null, null);
        private static void initSuperTable(){
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
            superTable.Set("slice", new IntrinsicUnit("string_slice", stringSlice, 3));

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
            superTable.Set("split", new IntrinsicUnit("string_split", stringSplit, 2));

            //////////////////////////////////////////////////////

            Unit stringLength(VM vm)
            {
                StringUnit val_input_string = vm.GetStringUnit(0);
                return new Unit(val_input_string.content.Length);
            }
            superTable.Set("length", new IntrinsicUnit("string_length", stringLength, 1));

            //////////////////////////////////////////////////////

            Unit stringCopy(VM vm)
            {
                Unit val_input_string = vm.GetUnit(0);
                if (val_input_string.Type == UnitType.String)
                    return new Unit(val_input_string.ToString());
                else
                    return new Unit(UnitType.Null);
            }
            superTable.Set("copy", new IntrinsicUnit("string_copy", stringCopy, 1));

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
            superTable.Set("to_list", new IntrinsicUnit("string_to_list", toList, 1));

            //////////////////////////////////////////////////////
            Unit charAt(VM vm)
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
            superTable.Set("char_at", new IntrinsicUnit("string_char_at", charAt, 2));

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
        }
    }
}