using System;

using lightningVM;

namespace lightningUnit
{
	public class IntrinsicUnit : HeapUnit
    {
        public string Name { get; private set; }
        public Func<VM, Unit> Function { get; private set; }
        public Operand Arity { get; private set; }

        public override UnitType Type
        {
            get
            {
                return UnitType.Intrinsic;
            }
        }

        public IntrinsicUnit(string p_Name, Func<VM, Unit> p_Function, Operand p_Arity)
        {
            Name = p_Name;
            Function = p_Function;
            Arity = p_Arity;
        }

        public override string ToString()
        {
            return new string("Intrinsic " + Name + "(" + Arity + ")");
        }

        public override bool Equals(object p_other)
        {
            Type other_type = p_other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)p_other).Type == UnitType.Intrinsic)
                {
                    if (Function == (((Unit)p_other).heapUnitValue as IntrinsicUnit).Function) return true;
                }
            }
            if (other_type == typeof(IntrinsicUnit))
            {
                if (Function == (p_other as IntrinsicUnit).Function) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}