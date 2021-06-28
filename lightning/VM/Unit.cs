﻿using System;
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
    public enum UnitType{
        Number,
        Null,
        Boolean,

        HeapUnit
    }

    public struct Unit{
        public Number unitValue;
        public HeapUnit heapUnitValue;

        public UnitType type;

        public Unit(HeapUnit p_value) : this()
        {
            heapUnitValue = p_value;
            type = UnitType.HeapUnit;
        }
        public Unit(Number p_number) : this()
        {
            unitValue = p_number;
            type = UnitType.Number;
        }

        public Unit(bool p_value) : this(){
            if(p_value == true){
                unitValue = 1;
            }else{
                unitValue = 0;
            }
            type = UnitType.Boolean;
        }

        public Unit(String p_string) : this()
        {
            if(p_string == "null")
            {
                type = UnitType.Null;
            }
            else
            {
                throw new Exception("Trying to create a Unit with invalid string: " + p_string);
            }
        }

        public Type HeapUnitType(){
            if(type == UnitType.HeapUnit)
                return heapUnitValue.GetType();
            return null;
        }

        public override string ToString()
        {
            if (type == UnitType.Number){
                return unitValue.ToString();
            }else if(type == UnitType.Null){
                return "null";
            }else if(type == UnitType.Boolean){
                if(unitValue == 0)
                    return "false";
                if(unitValue == 1)
                    return "true";
                throw new Exception("Trying to get String of Invalid Boolean");
            }else{
                return heapUnitValue.ToString();
            }
        }

        public bool ToBool()
        {
            if (type == UnitType.Number){
                Console.WriteLine("ERROR: Can not convert number to Bool.");
                throw new NotImplementedException();
            }else if(type == UnitType.Null){
                return false;
            }else if(type == UnitType.Boolean){
                if(unitValue == 0)
                    return false;
                if(unitValue == 1)
                    return true;
                throw new Exception("Trying to get Value of Invalid Boolean");
            }else{
                return heapUnitValue.ToBool();
            }
        }

        public override bool Equals(object other){
            if(other.GetType() != typeof(Unit))
                throw new Exception("Trying to compare Unit to non Unit type");
            if (type == UnitType.Number){
                Type other_type = other.GetType();
                if (((Unit)other).type == UnitType.Number){
                    return ((Unit)other).unitValue == unitValue;
                }
                return false;
            }else if(type == UnitType.Null){
                Type other_type = other.GetType();
                if (((Unit)other).type == UnitType.Null){
                    return true;
                }
                return false;
            }else if(type == UnitType.Boolean){
                return ToBool() == ((Unit)other).ToBool();
            }else{
                return heapUnitValue.Equals(other);
            }
        }

        public override int GetHashCode(){
            if (type == UnitType.Number){
                return unitValue.GetHashCode();
            }else if(type == UnitType.Null){
                return UnitType.Null.GetHashCode();
            }else if(type == UnitType.Boolean){
                return ToBool().GetHashCode();
            }else{
                return heapUnitValue.GetHashCode();
            }
        }
    }
}