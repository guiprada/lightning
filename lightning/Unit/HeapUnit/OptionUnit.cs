using System;
using System.Collections.Generic;

using lightningVM;

namespace lightningUnit
{
    public class OptionUnit : HeapUnit
    {
        public Unit Value { get; private set; }

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
            Value = p_value;
        }

        public override String ToString()
        {
            return "Otion: " +  Value.ToString();

        }

        public override bool Equals(object other)
        {
            throw new Exception("Trying to check equality of OptionUnit!");
        }

        // public override int CompareTo(object p_compareTo)
        // {
        //     throw new Exception("Trying to compare with OptionUnit!");
        // }

        public override int GetHashCode()
        {
            return "Option".GetHashCode() + Value.GetHashCode();
        }

        public override Unit Get(Unit p_key)
        {
            return methodTable.Get(p_key);
        }

        public bool OK()
        {
            if (Value.Type == UnitType.Empty)
                return false;
            return true;
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

                    return new Unit(this_option.OK());
                }
                methodTable.Set("ok", new IntrinsicUnit("option_ok", OK, 1));

                //////////////////////////////////////////////////////
                Unit Unwrap (VM vm)
                {
                    OptionUnit this_option = vm.GetOptionUnit(0);

                    return this_option.UnWrap();
                }
                methodTable.Set("unwrap", new IntrinsicUnit("option_unwrap", Unwrap, 1));

                //////////////////////////////////////////////////////
            }
        }
    }
}