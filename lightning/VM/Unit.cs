using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

using Operand = System.UInt16;

#if DOUBLE
    using Number = System.Double;
    using Integer = System.Int64;
#else
    using Number = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Unit{

        [FieldOffset(0)]
        public Number numberValue;
        [FieldOffset(0)]
        public char charValue;
        [FieldOffset(0)]
        public Integer integerValue;
        [FieldOffset(0)]
        public bool boolValue;

#if DOUBLE
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public HeapUnit heapUnitValue;

        public UnitType Type{
            get{
                return heapUnitValue.Type;
            }
        }

        public Unit(HeapUnit p_value):this()
        {
            numberValue = 0;
            heapUnitValue = p_value;
        }
        public Unit(Number p_number):this()
        {
            if(p_number%1 == 0){
                integerValue = (Integer)p_number;
                heapUnitValue = TypeUnit.Integer;
            }else{
                numberValue = p_number;
                heapUnitValue = TypeUnit.Number;
            }
        }

        public Unit(Integer p_number):this()
        {
            integerValue = p_number;
            heapUnitValue = TypeUnit.Integer;
        }

        public Unit(bool p_value):this()
        {
            if(p_value == true){
                boolValue = true;
            }else{
                boolValue = false;
            }
            heapUnitValue = TypeUnit.Boolean;
        }

        public Unit(String p_string):this()
        {
            numberValue = 0;
            heapUnitValue = new StringUnit(p_string);
        }

        public Unit(char p_char):this()
        {
            charValue = p_char;
            heapUnitValue = TypeUnit.Char;
        }

        public Unit(UnitType p_type):this()
        {
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
                case UnitType.Integer:
                    heapUnitValue = TypeUnit.Integer;
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
                    return numberValue.ToString();
                case UnitType.Integer:
                    return integerValue.ToString();
                case UnitType.Char:
                    return charValue.ToString();
                case UnitType.Null:
                    return "null";
                case UnitType.Boolean:
                    return boolValue.ToString();
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
                case UnitType.Integer:
                    throw new Exception("Can not convert Integer to Bool.");
                case UnitType.Char:
                    throw new Exception("Can not convert Char to Bool.");
                case UnitType.Null:
                    return false;
                case UnitType.Boolean:
                    return boolValue;
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
                        return ((Unit)other).numberValue == numberValue;
                    }else if(other_type == UnitType.Integer) {
                        return ((Unit)other).integerValue == numberValue;
                    }
                    return false;
                case UnitType.Integer:
                    if (other_type == UnitType.Number){
                        return (((Unit)other).numberValue) == integerValue;
                    }else if(other_type == UnitType.Integer) {
                        return ((Unit)other).integerValue == integerValue;
                    }
                    return false;
                case UnitType.Char:
                    if (other_type == UnitType.Char){
                        return this.charValue == ((Unit)other).charValue;
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
                    return numberValue.GetHashCode();
                case UnitType.Integer:
                    return integerValue.GetHashCode();
                case UnitType.Char:
                    return charValue.GetHashCode();
                case UnitType.Null:
                    return UnitType.Null.GetHashCode();
                case UnitType.Boolean:
                    return boolValue.GetHashCode();
                default:
                    return heapUnitValue.GetHashCode();
            }
        }

        public static Unit operator +(Unit op) => op;
        public static Unit operator -(Unit op){
            UnitType op_type = op.Type;
            if(op_type == UnitType.Number){
                return new Unit(- op.numberValue);
            }
            if(op_type == UnitType.Integer){
                return new Unit(- op.integerValue);
            }
            throw new Exception("Trying to negate non numeric UnitType");
        }
        public static Unit operator +(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type == UnitType.Number){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.numberValue + opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.numberValue + opB.integerValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.integerValue + opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue + opB.integerValue);
            }
            throw new Exception("Trying to add non numeric UnitType");
        }

        public static Unit operator +(Unit opA, Number opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Number){
                return new Unit(opA.numberValue + opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue + opB);
            }
            throw new Exception("Trying to increment non numeric UnitType");
        }

        public static Unit operator +(Unit opA, Integer opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Number){
                return new Unit(opA.numberValue + opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue + opB);
            }
            throw new Exception("Trying to increment non numeric UnitType");
        }

        public static Unit operator -(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type == UnitType.Number){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.numberValue - opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.numberValue - opB.integerValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.integerValue - opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue - opB.integerValue);
            }
            throw new Exception("Trying to subtract non numeric UnitType");
        }

        public static Unit operator -(Unit opA, Number opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Number){
                return new Unit(opA.numberValue - opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue - opB);
            }
            throw new Exception("Trying to decrement non numeric UnitType");
        }

        public static Unit operator -(Unit opA, Integer opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Number){
                return new Unit(opA.numberValue - opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue - opB);
            }
            throw new Exception("Trying to decrement non numeric UnitType");
        }

        public static Unit operator *(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type == UnitType.Number){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.numberValue * opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.numberValue * opB.integerValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.integerValue * opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue * opB.integerValue);
            }
            throw new Exception("Trying to multiply non numeric UnitType");
        }

        public static Unit operator /(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type == UnitType.Number){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.numberValue / opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.numberValue / opB.integerValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Number)
                    return new Unit(opA.integerValue / opB.numberValue);
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue / opB.integerValue);
            }
            throw new Exception("Trying to divide non numeric UnitType");
        }

        public static bool isNumeric(Unit p_value){
            UnitType type = p_value.Type;
            return (type == UnitType.Number || type  == UnitType.Integer);
        }
    }
}
