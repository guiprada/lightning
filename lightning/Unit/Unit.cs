using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using lightningExceptions;
using lightningTools;

namespace lightningUnit
{
    public enum UnitType
    {
        Float
        , Integer
        , Boolean
        , String
        , Char
        , UpValue
        , Table
        , List
        , Function
        , Intrinsic
        , ExternalFunction
        , Closure
        , Module
        , Wrapper
        , Option
        , Result
        , Void
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Unit : IComparable
    {
        [FieldOffset(0)]
        public Float floatValue;
        [FieldOffset(0)]
        public Integer integerValue;
        [FieldOffset(0)]
        public char charValue;
        [FieldOffset(0)]
        public bool boolValue;
        [FieldOffset(0)]
        public bool isHeapUnit;

#if DOUBLE
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public HeapUnit heapUnitValue;

        public UnitType Type
        {
            get
            {
                return heapUnitValue.Type;
            }
        }

        public Unit(HeapUnit p_value) : this()
        {
            isHeapUnit = true;
            heapUnitValue = p_value;
        }
        public Unit(Float p_number) : this()
        {
            floatValue = p_number;
            heapUnitValue = TypeUnit.Float;
        }

        public Unit(Integer p_number) : this()
        {
            integerValue = p_number;
            heapUnitValue = TypeUnit.Integer;
        }

        public Unit(bool p_value) : this()
        {
            boolValue = p_value;
            heapUnitValue = TypeUnit.Boolean;
        }

        public Unit(String p_string) : this()
        {
            isHeapUnit = true;
            heapUnitValue = new StringUnit(p_string);
        }

        public Unit(char p_char) : this()
        {
            charValue = p_char;
            heapUnitValue = TypeUnit.Char;
        }

        public Unit(UnitType p_type) : this()
        {
            switch (p_type)
            {
                case UnitType.Void:
                    heapUnitValue = TypeUnit.Void;
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
                    Logger.Log("Trying to create a Unit of unknown type.", Defaults.Config.VMLogFile);
                    throw Exceptions.unknown_type;
            }
        }

        public static Unit FromObject(object p_obj)
        {
            if (p_obj == null)
                return new Unit(new OptionUnit());

            Type this_type = p_obj.GetType();

            if (this_type == typeof(Unit)) // Returns the same object - it should work :)
                return new Unit(new OptionUnit((Unit)p_obj));

            if (this_type == typeof(char))
                return new Unit(new OptionUnit(new Unit((char)p_obj)));

            if (this_type == typeof(String))
                return new Unit(new OptionUnit(new Unit((String)p_obj)));

            if (this_type == typeof(bool))
                return new Unit(new OptionUnit(new Unit((bool)p_obj)));
#if DOUBLE

            if ( this_type == typeof(System.Int16) || this_type == typeof(System.Int32) )
                return new Unit(new OptionUnit(new Unit(Convert.ToInt64(p_obj))));
            else if (this_type == typeof(Integer))
                return new Unit(new OptionUnit(new Unit((Integer)p_obj)));
#else
            if (this_type == typeof(System.Int16))
                return new Unit(new OptionUnit(new Unit(Convert.ToInt64(p_obj))));
            else if (this_type == typeof(Integer))
                return new Unit(new OptionUnit(new Unit((Integer)p_obj)));
#endif
            return new Unit(new OptionUnit(new Unit(new WrapperUnit<object>(p_obj))));
        }

        public static object ToObject(Unit p_value)
        {
            UnitType this_type = p_value.Type;

            if (this_type == UnitType.Float)
                return p_value.floatValue;

            if (this_type == UnitType.Integer)
                return p_value.integerValue;

            if (this_type == UnitType.Char)
                return p_value.charValue;

            if (this_type == UnitType.String)
                return ((StringUnit)p_value.heapUnitValue).content;

            if (this_type == UnitType.Boolean)
                return p_value.boolValue;

            if (this_type == UnitType.Wrapper)
            {
                return ((WrapperUnit<object>)p_value.heapUnitValue).UnWrap();
            }

            if (this_type == UnitType.Option)
            {
                OptionUnit opt_value = (OptionUnit)p_value.heapUnitValue;
                if (opt_value.IsOK)
                    return ToObject(opt_value.Value);
                else
                    return null;
            }

            Logger.Log("Unit.ToObject - Could not convert to object!", Defaults.Config.VMLogFile);
            throw Exceptions.can_not_convert;
        }

        public override string ToString()
        {
            UnitType this_type = this.Type;
            switch (this_type)
            {
                case UnitType.Float:
                    return floatValue.ToString();
                case UnitType.Integer:
                    return integerValue.ToString();
                case UnitType.Char:
                    return charValue.ToString();
                case UnitType.Boolean:
                    return boolValue.ToString();
                case UnitType.Void:
                    return "VOID";
                default:
                    return heapUnitValue.ToString();
            }
        }

        public bool ToBool()
        {
            UnitType this_type = this.Type;
            switch (this_type)
            {
                case UnitType.Boolean:
                    return boolValue;
                default:
                    Logger.Log("Can not convert UnitType: " + this_type + " to Bool.", Defaults.Config.VMLogFile);
                    throw Exceptions.can_not_convert;
            }
        }

        public override bool Equals(object p_other)
        {
            if (p_other.GetType() != typeof(Unit))
            {
                Logger.Log("Trying to compare Unit to non Unit type.", Defaults.Config.VMLogFile);
                throw Exceptions.can_not_compare;
            }

            UnitType this_type = this.Type;
            UnitType other_type = ((Unit)p_other).Type;
            if (this_type == UnitType.Void || other_type == UnitType.Void)
            {
                Logger.Log("Trying to compare VOID Values", Defaults.Config.VMLogFile);
                throw Exceptions.can_not_compare;
            }

            switch (this_type)
            {
                case UnitType.Float:
                    if (other_type == UnitType.Float)
                    {
                        return ((Unit)p_other).floatValue == floatValue;
                    }
                    else if (other_type == UnitType.Integer)
                    {
                        Logger.Log("Trying to compare Float to Integer!", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    return false;
                case UnitType.Integer:
                    if (other_type == UnitType.Integer)
                    {
                        return ((Unit)p_other).integerValue == integerValue;
                    }
                    else
                    if (other_type == UnitType.Float)
                    {
                        Logger.Log("Trying to compare Integer to Float!", Defaults.Config.VMLogFile);
                        throw Exceptions.not_supported;
                    }
                    return false;
                case UnitType.Char:
                    if (other_type == UnitType.Char)
                    {
                        return this.charValue == ((Unit)p_other).charValue;
                    }
                    return false;
                case UnitType.Boolean:
                    if (other_type == UnitType.Boolean)
                    {
                        return boolValue == ((Unit)p_other).boolValue;
                    }
                    return false;
                default:
                    return heapUnitValue.Equals(p_other);
            }
        }

        public override int GetHashCode()
        {
            UnitType this_type = this.Type;
            switch (this_type)
            {
                case UnitType.Float:
                    return floatValue.GetHashCode();
                case UnitType.Integer:
                    return integerValue.GetHashCode();
                case UnitType.Char:
                    return charValue.GetHashCode();
                case UnitType.Void:
                    return "VOID".GetHashCode();
                case UnitType.Boolean:
                    return boolValue.GetHashCode();
                default:
                    return heapUnitValue.GetHashCode();
            }
        }

        public static Unit operator -(Unit p_op)
        {
            UnitType op_type = p_op.Type;
            if (op_type == UnitType.Float)
            {
                return new Unit(-p_op.floatValue);
            }
            else if (op_type == UnitType.Integer)
            {
                return new Unit(-p_op.integerValue);
            }

            Logger.Log("Trying to negate non numeric UnitType.", Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public static Unit operator +(Unit p_opA, Unit p_opB)
        {
            UnitType opA_type = p_opA.Type;
            UnitType opB_type = p_opB.Type;

            if (opA_type == UnitType.Float)
            {
                if (opB_type == UnitType.Float)
                    return new Unit(p_opA.floatValue + p_opB.floatValue);
            }
            else if (opA_type == UnitType.Integer)
            {
                if (opB_type == UnitType.Integer)
                    return new Unit(p_opA.integerValue + p_opB.integerValue);
            }

            Logger.Log("Adition between type: " + opA_type + " and type: " + opB_type + " is not supported!", Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public static Unit operator -(Unit p_opA, Unit p_opB)
        {
            UnitType opA_type = p_opA.Type;
            UnitType opB_type = p_opB.Type;

            if (opA_type == UnitType.Float)
            {
                if (opB_type == UnitType.Float)
                    return new Unit(p_opA.floatValue - p_opB.floatValue);
            }
            else if (opA_type == UnitType.Integer)
            {
                if (opB_type == UnitType.Integer)
                    return new Unit(p_opA.integerValue - p_opB.integerValue);
            }

            Logger.Log("Subtraction between type: " + opA_type + " and type: " + opB_type + " is not supported!", Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public static Unit operator *(Unit p_opA, Unit p_opB)
        {
            UnitType opA_type = p_opA.Type;
            UnitType opB_type = p_opB.Type;

            if (opA_type == UnitType.Float)
            {
                if (opB_type == UnitType.Float)
                    return new Unit(p_opA.floatValue * p_opB.floatValue);
            }
            else if (opA_type == UnitType.Integer)
            {
                if (opB_type == UnitType.Integer)
                    return new Unit(p_opA.integerValue * p_opB.integerValue);
            }

            Logger.Log("Multiplication between type: " + opA_type + " and type: " + opB_type + " is not supported!", Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public static Unit operator /(Unit p_opA, Unit p_opB)
        {
            UnitType opA_type = p_opA.Type;
            UnitType opB_type = p_opB.Type;

            if (opA_type == UnitType.Float)
            {
                if (opB_type == UnitType.Float)
                    return new Unit(p_opA.floatValue / p_opB.floatValue);
            }
            else if (opA_type == UnitType.Integer)
            {
                if (opB_type == UnitType.Integer)
                    return new Unit(p_opA.integerValue / p_opB.integerValue);
            }

            Logger.Log("Division between type: " + opA_type + " and type: " + opB_type + " is not supported!", Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public static Unit increment (Unit p_opA)
        {
            UnitType opA_type = p_opA.Type;

            if (opA_type == UnitType.Float)
            {
                return new Unit(p_opA.floatValue + 1.0);
            }
            else if (opA_type == UnitType.Integer)
            {
                return new Unit(p_opA.integerValue + 1);
            }

            Logger.Log("Can not incrementn type: " + opA_type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public static Unit decrement(Unit p_opA)
        {
            UnitType opA_type = p_opA.Type;

            if (opA_type == UnitType.Float)
            {
                return new Unit(p_opA.floatValue - 1.0);
            }
            else if (opA_type == UnitType.Integer)
            {
                return new Unit(p_opA.integerValue - 1);
            }

            Logger.Log("Can not decrementn type: " + opA_type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }

        public int CompareTo(object p_compareTo)
        {
            if (p_compareTo.GetType() != typeof(Unit))
                throw Exceptions.can_not_compare;

            Unit lhs = this;
            UnitType lhs_type = lhs.Type;
            Unit rhs = (Unit)p_compareTo;
            UnitType rhs_type = rhs.Type;

            // Resolve UpValue
            if (lhs_type == UnitType.UpValue)
            {
                lhs = ((UpValueUnit)lhs.heapUnitValue).UpValue;
                lhs_type = lhs.Type;
            }
            if (rhs_type == UnitType.UpValue)
            {
                rhs = ((UpValueUnit)rhs.heapUnitValue).UpValue;
                rhs_type = rhs.Type;
            }

            switch (lhs_type)
            {
                case UnitType.Float:
                    if (rhs_type == UnitType.Float)
                        return lhs.floatValue.CompareTo(rhs.floatValue);
                    else
                    {
                        Logger.Log("Trying to compare a: " + lhs_type + " to UnitType: " + rhs_type, Defaults.Config.VMLogFile);
                        throw Exceptions.can_not_compare;
                    }
                case UnitType.Integer:
                    if (rhs_type == UnitType.Integer)
                        return lhs.integerValue.CompareTo(rhs.integerValue);
                    else
                    {
                        Logger.Log("Trying to compare a: " + lhs_type + " to UnitType: " + rhs_type, Defaults.Config.VMLogFile);
                        throw Exceptions.can_not_compare;
                    }
                case UnitType.Char:
                    if (rhs_type == UnitType.Char)
                        return lhs.charValue.CompareTo(rhs.charValue);
                    else
                    {
                        Logger.Log("Trying to compare a: " + lhs_type + " to UnitType: " + rhs_type, Defaults.Config.VMLogFile);
                        throw Exceptions.can_not_compare;
                    }
                case UnitType.String:
                    if (rhs_type == UnitType.String)
                        return ((StringUnit)lhs.heapUnitValue).content.CompareTo(((StringUnit)rhs.heapUnitValue).content);
                    else
                    {
                        Logger.Log("Trying to compare a: " + lhs_type + " to UnitType: " + rhs_type, Defaults.Config.VMLogFile);
                        throw Exceptions.can_not_compare;
                    }
                default:
                    Logger.Log("Trying to compare a: " + lhs_type + " to UnitType: " + rhs_type, Defaults.Config.VMLogFile);
                    throw Exceptions.can_not_compare;
            }
        }
        public static bool IsNumeric(Unit p_value)
        {
            UnitType type = p_value.Type;
            return type == UnitType.Float || type == UnitType.Integer;
        }

        public static bool IsCallable(Unit p_value)
        {
            UnitType this_callable_type = p_value.Type;
            return  this_callable_type == UnitType.Function
                ||  this_callable_type == UnitType.Intrinsic
                ||  this_callable_type == UnitType.ExternalFunction
                ||  this_callable_type == UnitType.Closure;
        }

        public static bool IsEmpty(Unit p_value)
        {
            UnitType this_callable_type = p_value.Type;
            return this_callable_type == UnitType.Void;
        }
    }
}