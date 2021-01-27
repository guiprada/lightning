using System;
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

    public class ValNumber : Value
    {
        public Number content;
        public ValNumber(Number value)
        {
            content = value;
        }

        public override string ToString()
        {
            return content.ToString();
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert number to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(ValNumber))
            {
                if (((ValNumber)other).content == content) return true;
                else return false;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
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
            if (other.GetType() == typeof(ValString))
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
            return "Nil";
        }

        public override bool ToBool()
        {
            return false;
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(ValNil))
            {
                return true;
            }
            else return false;
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
            return new string("fun" + (name != null ? " " + name : "") + " (" + arity + ")");
        }

        public override bool ToBool()
        {
            Console.WriteLine("ERROR: Can not convert function to Bool.");
            throw new NotImplementedException();
        }

        public override bool Equals(object other)
        {
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
        public Func<VM, Value> function;
        public int arity;

        public ValIntrinsic(string p_name, Func<VM, Value> p_function, int p_arity)
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

        public void Register(List<Value> p_variables, int[] p_variablesBases)
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
        List<Value> variables;
        int[] variablesBases;
        Value val;
        public Value Val
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
            val = null;
        }

        public void Attach(List<Value> p_variables, int[] p_variablesBases)
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
            if (other.GetType() == typeof(ValUpValue))
            {
                if ((other as ValUpValue).Val == Val) return true;
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
        public List<Value> elements;
        public Dictionary<ValString, Value> table;


        public int ECount { get { return elements.Count; } }
        public int TCount { get { return table.Count; } }
        public int Count { get { return ECount + TCount; } }
        public ValTable(List<Value> p_elements, Dictionary<ValString, Value> p_table)
        {
            elements = p_elements ??= new List<Value>();
            table = p_table ??= new Dictionary<ValString, Value>();
        }

        public void ElementSet(int index, Value value)
        {
            if (index > (ECount - 1))
                ElementsStretch(index - (ECount - 1));
            elements[index] = value;
        }

        public void ElementAdd(Value value)
        {            
            elements.Add(value);
        }

        public void TableSet(ValString index, Value value)
        {
            table[index] = value;
        }

        void ElementsStretch(int n)
        {
            for (int i = 0; i < n; i++)
            {
                elements.Add(Value.Nil);
            }
        }

        public override string ToString()
        {
            string this_string = "table: ";
            int counter = 0;
            foreach (Value v in elements)
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
            foreach (KeyValuePair<ValString, Value> entry in table)
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
        public List<Value> globals;
        public Operand importIndex;
        public ValModule(string p_name, List<Value> p_elements, Dictionary<ValString, Value> p_table, List<Value> p_globals)
            : base(p_elements, p_table)
        {
            name = p_name;
            globals = p_globals ??= new List<Value>();
            importIndex = 0;
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(ValModule))
            {
                if ((other as ValModule).name == this.name) return true;
            }
            return false;
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
