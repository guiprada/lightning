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
        Value
    }

    //[StructLayout(LayoutKind.Explicit)]
    public struct Unit{
        //[FieldOffset(0)]
        public Value value;
        //[FieldOffset(0)]
        public Number number;
        //[FieldOffset(8)]
        public UnitType type;

        public Unit(Value p_value) : this()
        {
            value = p_value;
            type = UnitType.Value;
        }
        public Unit(Number p_number) : this()
        {
            number = p_number;
            type = UnitType.Number;
        }

        public Value GetValue(){
            return value;
        }

        public override string ToString()
        {
            if (type == UnitType.Number){
                return number.ToString();
            }else{
                return value.ToString();
            }
        }

        public bool ToBool()
        {
            if (type == UnitType.Number){
                Console.WriteLine("ERROR: Can not convert number to Bool.");
                throw new NotImplementedException();
            }else{
                return value.ToBool();
            }
        }

        public override bool Equals(object other){
            if (type == UnitType.Number){
                if (((Unit)other).type == UnitType.Number){
                    return ((Unit)other).number == number;
                }
                return false;
            }else{
                return value.Equals(other);
            }
        }

        public override int GetHashCode(){
            if (type == UnitType.Number){
                return number.GetHashCode();
            }else{
                return value.GetHashCode();
            }
        }
    }
    public abstract class Value
    {
        static ValNil global_nil = new ValNil();
        public static ValNil Nil { get { return global_nil; } }

        static ValBool global_false = new ValBool(false);
        public static ValBool False { get { return global_false; } }

        static ValBool global_true = new ValBool(true);
        public static ValBool True { get { return global_true; } }

        public abstract override string ToString();
        public abstract bool ToBool();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }

    public class ValString : Value
    {
        public string content;

        public ValString(string value)
        {
            content = value;
        }
        public override string ToString()
        {
            return content;
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert string to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValString))
                    if(content == ((ValString)((Unit)(other)).value).content)
                        return true;
            }
            if(other.GetType() == typeof(ValString))
            {
                if (((ValString)other).content == content)
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
    }

    public class ValBool : Value
    {
        bool content;

        public ValBool(bool value)
        {
            content = value;
        }

        public override string ToString()
        {
            return content.ToString();
        }

        public override bool ToBool()
        {
            return content;
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValBool))
                    if(content == ((ValBool)((Unit)(other)).value).content)
                        return true;
            }
            if (other.GetType() == typeof(ValBool))
            {
                if (((ValBool)other).content == this.content) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
    }

    public class ValNil : Value
    {
        public override string ToString()
        {
            return "nil";
        }

        public override bool ToBool()
        {
            return false;
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValNil))
                    return true;
            }
            if (other.GetType() == typeof(ValNil))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }
    }

    public class ValFunction : Value
    {
        public string name;
        public Operand arity;
        public List<Instruction> body;
        public string module;
        public Operand originalPosition;

        public ValFunction(string p_name, string p_module)
        {
            name = p_name;
            arity = 0;
            body = new List<Instruction>();
            module = p_module;
        }
        public override string ToString()
        {
            string str = new string("fun" + " " + name + ":" + module +  " " + " (" + arity + ")");
            foreach (Instruction i in body)
            {
                str += i.ToString() + "\n";
            }
            return str;
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert function to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValFunction))
                {
                    ValFunction other_val_func = (ValFunction)((Unit)(other)).value;
                    if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
                }
            }
            if (other.GetType() == typeof(ValFunction))
            {
                ValFunction other_val_func = other as ValFunction;
                if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + module.GetHashCode();
        }
    }

    public class ValIntrinsic : Value
    {
        public string name;
        public Func<VM, Unit> function;
        public int arity;

        public ValIntrinsic(string p_name, Func<VM, Unit> p_function, int p_arity)
        {
            name = p_name;
            function = p_function;
            arity = p_arity;
        }

        public override string ToString()
        {
            return new string("Intrinsic " + name + "(" + arity + ")");
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert intrinsic to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValIntrinsic))
                {
                    if (function == (((Unit)other).value as ValIntrinsic).function) return true;
                }
            }
            if (other.GetType() == typeof(ValIntrinsic))
            {
                if (function == (other as ValIntrinsic).function) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    public class ValClosure : Value
    {
        public ValFunction function;
        public List<ValUpValue> upValues;

        public ValClosure(ValFunction p_function, List<ValUpValue> p_upValues)
        {
            function = p_function;
            upValues = p_upValues;
        }

        public void Register(List<Unit> p_variables, int[] p_variablesBases)
        {
            foreach (ValUpValue u in upValues)
                u.Attach(p_variables, p_variablesBases);
        }

        public override string ToString()
        {
            return new string("Closure of " + function.ToString());
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert clojure to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValClosure))
                {
                    if (this == ((Unit)other).value as ValClosure) return true;
                }
            }
            if (other.GetType() == typeof(ValClosure))
            {
                if (other == this) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return upValues.GetHashCode() + function.GetHashCode();
        }
    }

    public class ValUpValue : Value
    {
        public Operand address;
        public Operand env;
        bool isCaptured;
        List<Unit> variables;
        int[] variablesBases;
        Unit val;
        public Unit Val
        {
            get
            {
                if (isCaptured)
                {
                    //Console.WriteLine("get " + val);
                    return val;
                }
                else
                {
                    int this_BP = variablesBases[env];
                    return variables[this_BP + address];
                }
            }
            set
            {
                if (isCaptured)
                {
                    //Console.WriteLine("captured received " + value);
                    val = value;
                    //Console.WriteLine("set to " +  val);
                }
                else
                {
                    //Console.WriteLine("not captured received " + value);
                    int this_BP = variablesBases[env];
                    variables[this_BP + address] = value;
                    //Console.WriteLine("set to " + Val);
                }
            }
        }

        public ValUpValue(Operand p_address, Operand p_env)
        {
            address = p_address;
            env = p_env;
            isCaptured = false;
            variables = null;
            variablesBases = null;
            val = new Unit(Value.Nil);
        }

        public void Attach(List<Unit> p_variables, int[] p_variablesBases)
        {
            variables = p_variables;
            variablesBases = p_variablesBases;
        }

        public void Capture()
        {
            if (isCaptured == false)
            {
                isCaptured = true;
                int this_BP = variablesBases[env];
                val = variables[this_BP + address];
            }
        }
        public override string ToString()
        {
            return new string("upvalue " + address + " on env " + env + " Value: " + Val.ToString());
        }

        public override bool ToBool()
        {
            return Val.ToBool();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValUpValue))
                {
                    if (Val.Equals(((Unit)other).value)) return true;
                }
            }
            if (other.GetType() == typeof(ValUpValue))
            {
                if (Val.Equals(other)) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Val.GetHashCode();
        }

    }

    public class ValTable : Value
    {
        public List<Unit> elements;
        public Dictionary<ValString, Unit> table;


        public int ECount { get { return elements.Count; } }
        public int TCount { get { return table.Count; } }
        public int Count { get { return ECount + TCount; } }
        public ValTable(List<Unit> p_elements, Dictionary<ValString, Unit> p_table)
        {
            elements = p_elements ??= new List<Unit>();
            table = p_table ??= new Dictionary<ValString, Unit>();
        }

        public void ElementSet(int index, Unit value)
        {
            if (index > (ECount - 1))
                ElementsStretch(index - (ECount - 1));
            elements[index] = value;
        }

        public void ElementAdd(Unit value)
        {            
            elements.Add(value);
        }

        public void TableSet(ValString index, Unit value)
        {
            table[index] = value;
        }

        void ElementsStretch(int n)
        {
            for (int i = 0; i < n; i++)
            {
                elements.Add(new Unit(Value.Nil));
            }
        }

        public override string ToString()
        {
            string this_string = "table: ";
            int counter = 0;
            foreach (Unit v in elements)
            {
                if (counter == 0)
                {
                    this_string += counter + ":" + v.ToString();
                    counter++;
                }
                else
                {
                    this_string += ", " + counter + ":" + v.ToString();
                    counter++;
                }
            }
            if (counter > 0)
                this_string += " ";
            bool first = true;
            foreach (KeyValuePair<ValString, Unit> entry in table)
            {
                if (first)
                {
                    this_string += '{' + entry.Key.ToString() + ':' + entry.Value + '}';
                    first = false;
                }
                else
                {
                    this_string += ", {" + entry.Key.ToString() + ':' + entry.Value + '}';
                }
            }
            return this_string;
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert list to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValTable))
                {
                    if (this == ((Unit)other).value as ValTable) return true;
                }
            }
            if (other.GetType() == typeof(ValTable))
            {
                if (other == this) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return elements.GetHashCode() + table.GetHashCode();
        }
    }

    public class ValModule : ValTable
    {
        public string name;
        public List<Unit> globals;
        public List<Unit> constants;
        public Operand importIndex;
        public ValModule(string p_name, List<Unit> p_elements, Dictionary<ValString, Unit> p_table, List<Unit> p_globals, List<Unit> p_constants)
            : base(p_elements, p_table)
        {
            name = p_name;
            globals = p_globals ??= new List<Unit>();
            constants = p_constants ??= new List<Unit>();
            importIndex = 0;
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValModule))
                {
                    if (this.name == (((Unit)other).value as ValModule).name) return true;
                }
            }
            if (other.GetType() == typeof(ValModule))
            {
                if ((other as ValModule).name == this.name) return true;
            }
            return false;
        }
        public override string ToString()
        {
            return "module" + name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    public class ValWrapper<T> : Value
    {
        public object content;

        public ValWrapper(object content)
        {
            this.content = content;
        }

        public override string ToString()
        {
            return content.ToString();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(Unit))
            {
                if(((Unit)other).value.GetType() == typeof(ValWrapper<T>))
                {
                    if (this.content == (((Unit)other).value as ValWrapper<T>).content) return true;
                }
            }
            if(other.GetType() == typeof(ValWrapper<T>))
            {
                if(((ValWrapper<T>)other).content == this.content)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert a referenc Wrapper to Bool.");
            throw new NotImplementedException();
        }

        public T UnWrapp()
        {
            if (this.content.GetType() == typeof(T))
            {
                return (T)this.content;
            }
            else
            {
                throw new Exception("UnWrapp<>() type Error!");
            }
        }
    }
}
