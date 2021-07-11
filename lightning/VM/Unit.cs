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

        public Unit(char p_char){
            unitValue = p_char;
            heapUnitValue = TypeUnit.Char;
        }

        public Unit(UnitType p_type){
            unitValue = 0;
            switch(p_type){
                case UnitType.Null:
                    heapUnitValue = TypeUnit.Null;
                    break;
                case UnitType.Boolean:
                    heapUnitValue = TypeUnit.Boolean;
                    break;
                case UnitType.Number:
                    heapUnitValue = TypeUnit.Number;
                    break;
                case UnitType.Char:
                    heapUnitValue = TypeUnit.Char;
                    break;
                default:
                    throw new Exception("Trying to create a Unit of unknown type.");
            }
        }

        public override string ToString()
        {
            UnitType this_type = this.Type;
            switch(this_type){
                case UnitType.Number:
                    return unitValue.ToString();
                case UnitType.Char:
                    return ((char)unitValue).ToString();
                case UnitType.Null:
                    return "null";
                case UnitType.Boolean:
                    if(unitValue == 0)
                        return "false";
                    if(unitValue == 1)
                        return "true";
                    throw new Exception("Trying to get String of Invalid Boolean.");
                default:
                    return heapUnitValue.ToString();
            }
        }

        public bool ToBool()
        {
            UnitType this_type = this.Type;
            switch(this_type){
                case UnitType.Number:
                    throw new Exception("Can not convert Number to Bool.");
                case UnitType.Char:
                    throw new Exception("Can not convert Char to Bool.");
                case UnitType.Null:
                    return false;
                case UnitType.Boolean:
                    if(unitValue == 0)
                        return false;
                    if(unitValue == 1)
                        return true;
                    throw new Exception("Trying to get Value of Invalid Boolean.");
                default:
                    return heapUnitValue.ToBool();
            }
        }

        public override bool Equals(object other){
            if(other.GetType() != typeof(Unit))
                throw new Exception("Trying to compare Unit to non Unit type.");

            UnitType this_type = this.Type;
            UnitType other_type = ((Unit)other).Type;
            switch(this_type){
                case UnitType.Number:
                    if (other_type == UnitType.Number){
                        return ((Unit)other).unitValue == unitValue;
                    }
                    return false;
                case UnitType.Char:
                    if (other_type == UnitType.Char){
                        return this.unitValue == ((Unit)other).unitValue;
                    }
                    return false;
                case UnitType.Null:
                    if (other_type == UnitType.Null){
                        return true;
                    }
                    return false;
                case UnitType.Boolean:
                    return ToBool() == ((Unit)other).ToBool();
                default:
                    return heapUnitValue.Equals(other);
            }
        }

        public override int GetHashCode(){
            UnitType this_type = this.Type;
            switch(this_type){
                case UnitType.Number:
                    return unitValue.GetHashCode();
                case UnitType.Char:
                    return ((char)unitValue).GetHashCode();
                case UnitType.Null:
                    return UnitType.Null.GetHashCode();
                case UnitType.Boolean:
                    return ToBool().GetHashCode();
                default:
                    return heapUnitValue.GetHashCode();
            }
        }
    }
}
