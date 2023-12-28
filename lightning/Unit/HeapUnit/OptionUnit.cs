using System;
using System.Collections.Generic;

using lightningVM;

namespace lightningUnit
{
    public class OptionUnit : HeapUnit
    {
        public Unit Value { get; private set; }
        public bool IsOK {
            get
            {
                if (Value.Type == UnitType.Empty)
                    return false;
                return true;
            }
        }
        public bool IsEmpty {
            get
            {
                if (Value.Type == UnitType.Empty)
                    return true;
                return false;
            }
        }

        public override UnitType Type
        {
            get
            {
                return UnitType.Option;
            }
        }

        public OptionUnit()
        {
            Value = new Unit(UnitType.Empty);
        }

        public OptionUnit(Unit p_value)
        {
            if (p_value.Type == UnitType.Option)
                Value = ((OptionUnit)p_value.heapUnitValue).Value;
            else
                Value = p_value;
        }

        public override String ToString()
        {
            return "Option: " +  Value.ToString();
        }

        public override bool Equals(object other)
        {
            throw new Exception("Trying to check equality of OptionUnit!");
        }

        public override int CompareTo(object p_compareTo)
        {
            throw new Exception("Trying to compare with OptionUnit!");
        }

        public override int GetHashCode()
        {
            return "Option".GetHashCode() + Value.GetHashCode();
        }

        public override Unit Get(Unit p_key)
        {
            return methodTable.Get(p_key);
        }

        public Unit UnWrap()
        {
            if (Value.Type == UnitType.Empty)
                throw new Exception("Option is empty!");
            else
                return Value;
        }

        public static Unit UnWrap(Unit p_value)
        {
            if (p_value.Type == UnitType.Option)
            {
                return ((OptionUnit)p_value.heapUnitValue).UnWrap();
            }
            return p_value;
        }

        public Unit Expect(string p_msg)
        {
            if (Value.Type == UnitType.Empty)
                throw new Exception("Option is empty! " + p_msg);
            else
                return Value;
        }

        public static Unit Expect(Unit p_value, string p_msg)
        {
            if (p_value.Type == UnitType.Option)
            {
                return ((OptionUnit)p_value.heapUnitValue).Expect(p_msg);
            }
            return p_value;
        }

        public Unit Default(Unit p_default)
        {
            if (Value.Type == UnitType.Empty)
                return p_default;
            else
                return Value;
        }

        public static Unit Default(Unit p_value, Unit p_default)
        {
            if (p_value.Type == UnitType.Option)
            {
                return ((OptionUnit)p_value.heapUnitValue).Default(p_default);
            }
            return p_value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit methodTable = new TableUnit(null);
        public static TableUnit ClassMethodTable { get { return methodTable; } }
        static OptionUnit()
        {
            initMethodTable();
        }
        private static void initMethodTable()
        {
            {
                Unit OK (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);

                    return new Unit(this_option.IsOK);
                }
                methodTable.Set("is_ok", new IntrinsicUnit("option_is_ok", OK, 1));

                //////////////////////////////////////////////////////
                Unit Empty (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);

                    return new Unit(!this_option.IsOK);
                }
                methodTable.Set("is_empty", new IntrinsicUnit("option_is_empty", Empty, 1));

                //////////////////////////////////////////////////////
                Unit Unwrap (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);

                    return this_option.UnWrap();
                }
                methodTable.Set("unwrap", new IntrinsicUnit("option_unwrap", Unwrap, 1));

                //////////////////////////////////////////////////////
                Unit Expect (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);
                    StringUnit expect_string = vm.GetStringUnit(1);

                    return this_option.Expect(expect_string.content);
                }
                methodTable.Set("expect", new IntrinsicUnit("option_expect", Expect, 2));

                //////////////////////////////////////////////////////
                Unit Default (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);
                    Unit default_unit = vm.GetUnit(1);

                    return this_option.Default(default_unit);
                }
                methodTable.Set("default", new IntrinsicUnit("option_default", Default, 2));

                //////////////////////////////////////////////////////
            }
        }
    }
}