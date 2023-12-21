using System;

namespace lightningUnit
{
	public class ExternalFunctionUnit : HeapUnit
    {
        public string Name { get; private set; }
        public System.Reflection.MethodInfo Function { get; private set; }
        public Operand Arity { get; private set; }

        public override UnitType Type
        {
            get
            {
                return UnitType.ExternalFunction;
            }
        }

        public ExternalFunctionUnit(string p_Name, System.Reflection.MethodInfo p_Function, Operand p_Arity)
        {
            if (p_Function != null)
            {
                Name = p_Name;
                Function = p_Function;
                Arity = p_Arity;
            }
            else
                throw new Exception("Cannot create ExternalFunctionUnit from null!");
        }

        public override string ToString()
        {
            return new string("ExternalFunctionUnit " + Name + "(" + Arity + ")");
        }

        public override bool Equals(object p_other)
        {
            Type other_type = p_other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)p_other).Type == UnitType.ExternalFunction)
                {
                    if (Function == (((Unit)p_other).heapUnitValue as ExternalFunctionUnit).Function) return true;
                }
            }
            if (other_type == typeof(ExternalFunctionUnit))
            {
                if (Function == (p_other as ExternalFunctionUnit).Function) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}