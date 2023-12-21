using System;
using System.Collections.Generic;

namespace lightningUnit
{
	public class ClosureUnit : HeapUnit
    {
        public FunctionUnit Function { get; private set; }
        public List<UpValueUnit> UpValues { get; private set; }
        public override UnitType Type
        {
            get
            {
                return UnitType.Closure;
            }
        }

        public ClosureUnit(FunctionUnit p_Function, List<UpValueUnit> p_UpValues)
        {
            Function = p_Function;
            UpValues = p_UpValues;
        }

        public override string ToString()
        {
            return new string("Closure of " + Function.ToString());
        }

        public override bool Equals(object p_other)
        {
            Type other_type = p_other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)p_other).Type == UnitType.Closure)
                {
                    if (this == ((Unit)p_other).heapUnitValue as ClosureUnit) return true;
                }
            }
            if (other_type == typeof(ClosureUnit))
            {
                if (p_other == this) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return UpValues.GetHashCode() + Function.GetHashCode();
        }
    }
}