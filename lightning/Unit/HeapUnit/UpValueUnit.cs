using System;

namespace lightningUnit
{
	public class UpValueUnit : HeapUnit
    {
        public Operand Address { get; private set; }
        public Operand Env { get; private set; }
        private bool isCaptured;
        public bool IsCaptured { get { return isCaptured; } }
        private lightningVM.Memory<Unit> variables;
        private Unit value;
        public override UnitType Type
        {
            get
            {
                return UnitType.UpValue;
            }
        }
        public Unit UpValue
        {
            get
            {
                if (isCaptured)
                    return value;
                else
                    return variables.GetAt(Address, Env);
            }
            set
            {
                if (isCaptured)
                    this.value = value;
                else
                    variables.SetAt(value, Address, Env);
            }
        }

        public UpValueUnit(Operand p_Address, Operand p_Env)
        {
            Address = p_Address;
            Env = p_Env;
            isCaptured = false;
            variables = null;
            value = new Unit(UnitType.Null);
        }

        public void Attach(lightningVM.Memory<Unit> p_variables)
        {
            variables = p_variables;
        }

        public void Capture()
        {
            if (isCaptured == false)
            {
                isCaptured = true;
                value = variables.GetAt(Address, Env);
            }
        }
        public override string ToString()
        {
            return new string("upvalue " + Address + " on env " + Env + " HeapUnit: " + UpValue.ToString());
        }

        public override bool ToBool()
        {
            return UpValue.ToBool();
        }

        public override bool Equals(object p_other)
        {
            Type other_type = p_other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)p_other).Type == UnitType.UpValue)
                {
                    if (UpValue.Equals(((Unit)p_other).heapUnitValue)) return true;
                }
            }
            if (other_type == typeof(UpValueUnit))
            {
                if (UpValue.Equals(p_other)) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return UpValue.GetHashCode();
        }
    }
}