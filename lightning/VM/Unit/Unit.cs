﻿using System;
using System.Runtime.InteropServices;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Unit : IComparable{

        [FieldOffset(0)]
        public Float floatValue;
        [FieldOffset(0)]
        public Integer integerValue;
        [FieldOffset(0)]
        public char charValue;
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
            floatValue = 0;
            heapUnitValue = p_value;
        }
        public Unit(Float p_number):this()
        {
            floatValue = p_number;
            heapUnitValue = TypeUnit.Float;
        }

        public Unit(Integer p_number):this()
        {
            integerValue = p_number;
            heapUnitValue = TypeUnit.Integer;
        }

        public Unit(bool p_value):this()
        {
            boolValue = p_value;
            heapUnitValue = TypeUnit.Boolean;
        }

        public Unit(String p_string):this()
        {
            floatValue = 0;
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
                case UnitType.Float:
                    heapUnitValue = TypeUnit.Float;
                    break;
                case UnitType.Integer:
                    heapUnitValue = TypeUnit.Integer;
                    break;
                case UnitType.Char:
                    heapUnitValue = TypeUnit.Char;
                    break;
                default:
                    throw new Exception("Trying to create a Unit of unknown type. " + VM.ErrorString(null));
            }
        }

        public override string ToString()
        {
            UnitType this_type = this.Type;
            switch(this_type){
                case UnitType.Float:
                    return floatValue.ToString();
                case UnitType.Integer:
                    return integerValue.ToString();
                case UnitType.Char:
                    return charValue.ToString();
                case UnitType.Null:
                    return "Null";
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
                case UnitType.Float:
                    throw new Exception("Can not convert Float to Bool. " + VM.ErrorString(null));
                case UnitType.Integer:
                    throw new Exception("Can not convert Integer to Bool. " + VM.ErrorString(null));
                case UnitType.Char:
                    throw new Exception("Can not convert Char to Bool. " + VM.ErrorString(null));
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
                throw new Exception("Trying to compare Unit to non Unit type. " + VM.ErrorString(null));

            UnitType this_type = this.Type;
            UnitType other_type = ((Unit)other).Type;
            switch(this_type){
                case UnitType.Float:
                    if (other_type == UnitType.Float){
                        return ((Unit)other).floatValue == floatValue;
                    }else if(other_type == UnitType.Integer) {
                        return ((Unit)other).integerValue == floatValue;
                    }
                    return false;
                case UnitType.Integer:
                    if (other_type == UnitType.Float){
                        return (((Unit)other).floatValue) == integerValue;
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
                    if (other_type == UnitType.Float){
                        return false;
                    }else if(other_type == UnitType.Integer) {
                        return false;
                    }else if(other_type == UnitType.Char) {
                        return false;
                    }
                    return ToBool() == ((Unit)other).ToBool();
                default:
                    return heapUnitValue.Equals(other);
            }
        }

        public override int GetHashCode(){
            UnitType this_type = this.Type;
            switch(this_type){
                case UnitType.Float:
                    return floatValue.GetHashCode();
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

        // public static Unit operator +(Unit op) => op;
        public static Unit operator -(Unit op){
            UnitType op_type = op.Type;
            if(op_type == UnitType.Float){
                return new Unit(- op.floatValue);
            }
            if(op_type == UnitType.Integer){
                return new Unit(- op.integerValue);
            }
            throw new Exception("Trying to negate non numeric UnitType. " + VM.ErrorString(null));
        }
        public static Unit operator +(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type != opB_type)
                throw new Exception("Trying to add different UnitType. " + VM.ErrorString(null));

            if(opA_type == UnitType.Float){
                if(opB_type == UnitType.Float)
                    return new Unit(opA.floatValue + opB.floatValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue + opB.integerValue);
            }
            throw new Exception("Trying to add non alphanumeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator +(Unit opA, Float opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Float){
                return new Unit(opA.floatValue + opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue + (Integer)opB);
            }
            throw new Exception("Trying to increment non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator +(Unit opA, Integer opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Float){
                return new Unit(opA.floatValue + (Float)opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue + opB);
            }
            throw new Exception("Trying to increment non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator -(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type != opB_type)
                throw new Exception("Trying to subtract different UnitType. " + VM.ErrorString(null));

            if(opA_type == UnitType.Float){
                if(opB_type == UnitType.Float)
                    return new Unit(opA.floatValue - opB.floatValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue - opB.integerValue);
            }
            throw new Exception("Trying to subtract non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator -(Unit opA, Float opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Float){
                return new Unit(opA.floatValue - opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue - (Integer)opB);
            }
            throw new Exception("Trying to decrement non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator -(Unit opA, Integer opB){
            UnitType opA_type = opA.Type;

            if(opA_type == UnitType.Float){
                return new Unit(opA.floatValue - (Float)opB);
            }
            if(opA_type == UnitType.Integer){
                return new Unit(opA.integerValue - opB);
            }
            throw new Exception("Trying to decrement non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator *(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type != opB_type)
                throw new Exception("Trying to multiply different UnitType. " + VM.ErrorString(null));

            if(opA_type == UnitType.Float){
                if(opB_type == UnitType.Float)
                    return new Unit(opA.floatValue * opB.floatValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue * opB.integerValue);
            }
            throw new Exception("Trying to multiply non numeric UnitType. " + VM.ErrorString(null));
        }

        public static Unit operator /(Unit opA, Unit opB){
            UnitType opA_type = opA.Type;
            UnitType opB_type = opB.Type;

            if(opA_type != opB_type)
                throw new Exception("Trying to divide different UnitType. " + VM.ErrorString(null));

            if(opA_type == UnitType.Float){
                if(opB_type == UnitType.Float)
                    return new Unit(opA.floatValue / opB.floatValue);
            }
            if(opA_type == UnitType.Integer){
                if(opB_type == UnitType.Integer)
                    return new Unit(opA.integerValue / opB.integerValue);
            }
            throw new Exception("Trying to divide non numeric UnitType. " + VM.ErrorString(null));
        }

        public int CompareTo(object compareTo){
            UnitType this_type = this.Type;
            if(compareTo.GetType() != typeof(Unit)) return -1;
            Unit other = (Unit)compareTo;
            UnitType other_type = ((Unit)other).Type;
            switch(this_type){
                case UnitType.Float:
                    switch(other_type){
                        case UnitType.Float:
                            return floatValue.CompareTo(other.floatValue);
                        case UnitType.Integer:
                            return floatValue.CompareTo(other.integerValue);
                        case UnitType.Char:
                            return 1;
                        case UnitType.Null:
                            return 1;
                        case UnitType.Boolean:
                            return 1;
                        default:
                            return -1;
                    }
                case UnitType.Integer:
                    switch(other_type){
                        case UnitType.Float:
                            if(this.integerValue > other.floatValue)
                                return 1;
                            else if(this.integerValue < other.floatValue)
                                return -1;
                            else
                                return 0;
                        case UnitType.Integer:
                            return integerValue.CompareTo(other.integerValue);
                        case UnitType.Char:
                        case UnitType.Null:
                        case UnitType.Boolean:
                            return 1;
                        default:
                            return -1;
                    }
                case UnitType.Char:
                    switch(other_type){
                        case UnitType.Float:
                        case UnitType.Integer:
                            return -1;
                        case UnitType.Char:
                            return charValue.CompareTo(other.charValue);
                        case UnitType.Null:
                        case UnitType.Boolean:
                            return 1;
                        default:
                            return -1;
                    }
                case UnitType.Null:
                    switch(other_type){
                        case UnitType.Null:
                            return 0;
                        default:
                            return -1;
                    }
                case UnitType.Boolean:
                    switch(other_type){
                        case UnitType.Float:
                        case UnitType.Integer:
                        case UnitType.Char:
                            return -1;
                        case UnitType.Null:
                            return 1;
                        case UnitType.Boolean:
                            return boolValue.CompareTo(other.boolValue);
                        default:
                            return -1;
                    }
                case UnitType.String:
                case UnitType.Table:
                case UnitType.Function:
                case UnitType.Intrinsic:
                case UnitType.Closure:
                case UnitType.UpValue:
                case UnitType.Module:
                case UnitType.Wrapper:
                    return (this.heapUnitValue).CompareTo(compareTo);
                default:
                    throw new Exception("Trying to compare a Unit to unkown UnitType.");
            }
        }
        public static bool IsNumeric(Unit p_value){
            UnitType type = p_value.Type;
            return (type == UnitType.Float || type  == UnitType.Integer);
        }
    }
}