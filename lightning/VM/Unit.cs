using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

using Operand = System.UInt16;

#if DOUBLE
    using Number = System.Double;
#else
    using Number = System.Single;
#endif

namespace lightning
{
    public struct Unit{
        public Number unitValue;
        public HeapUnit heapUnitValue;

        public UnitType Type{
            get{
                return heapUnitValue.Type;
            }
        }

        public Unit(HeapUnit p_value)
        {
            unitValue = 0;
            heapUnitValue = p_value;
        }
        public Unit(Number p_number)
        {
            unitValue = p_number;
            heapUnitValue = TypeUnit.Number;
        }

        public Unit(bool p_value){
            if(p_value == true){
                unitValue = 1;
            }else{
                unitValue = 0;
            }
            heapUnitValue = TypeUnit.Boolean;
        }

        public Unit(String p_string){
            unitValue = 0;
            heapUnitValue = new StringUnit(p_string);
        }

        public Unit(UnitType p_type){
            unitValue = 0;
            if(p_type == UnitType.Null)
                heapUnitValue = TypeUnit.Null;
            else if(p_type == UnitType.Boolean)
                heapUnitValue = TypeUnit.Boolean;
            else if(p_type == UnitType.Number)
                heapUnitValue = TypeUnit.Number;
            else
                throw new Exception("Trying to create a Unit of unknown type.");
        }

        public override string ToString()
        {
            UnitType type = this.Type;
            if (type == UnitType.Number){
                return unitValue.ToString();
            }else if(type == UnitType.Null){
                return "null";
            }else if(type == UnitType.Boolean){
                if(unitValue == 0)
                    return "false";
                if(unitValue == 1)
                    return "true";
                throw new Exception("Trying to get String of Invalid Boolean.");
            }

            return heapUnitValue.ToString();
        }

        public bool ToBool()
        {
            UnitType this_type = this.Type;
            if (this_type == UnitType.Number){
                throw new Exception("Can not convert Number to Bool.");
            }else if(this_type == UnitType.Null){
                return false;
            }else if(this_type == UnitType.Boolean){
                if(unitValue == 0)
                    return false;
                if(unitValue == 1)
                    return true;
                throw new Exception("Trying to get Value of Invalid Boolean.");
            }else{
                return heapUnitValue.ToBool();
            }
        }

        public override bool Equals(object other){
            UnitType this_type = this.Type;
            if(other.GetType() != typeof(Unit))
                throw new Exception("Trying to compare Unit to non Unit type.");

            UnitType other_type = ((Unit)other).Type;
            if (this_type == UnitType.Number){
                if (other_type == UnitType.Number){
                    return ((Unit)other).unitValue == unitValue;
                }
                return false;
            }else if(this_type == UnitType.Null){
                if (other_type == UnitType.Null){
                    return true;
                }
                return false;
            }else if(this_type == UnitType.Boolean){
                return ToBool() == ((Unit)other).ToBool();
            }else{
                return heapUnitValue.Equals(other);
            }
        }

        public override int GetHashCode(){
            UnitType this_type = this.Type;
            if (this_type == UnitType.Number){
                return unitValue.GetHashCode();
            }else if(this_type == UnitType.Null){
                return UnitType.Null.GetHashCode();
            }else if(this_type == UnitType.Boolean){
                return ToBool().GetHashCode();
            }else{
                return heapUnitValue.GetHashCode();
            }
        }
    }
}
