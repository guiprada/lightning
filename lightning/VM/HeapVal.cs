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
    public abstract class HeapValue
    {
        public abstract override string ToString();
        public abstract bool ToBool();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }

    public class ValString : HeapValue
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValString))
                    if(content == ((ValString)((Unit)(other)).heapValue).content)
                        return true;
            }
            if(other_type == typeof(ValString))
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

    public class ValFunction : HeapValue
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValFunction))
                {
                    ValFunction other_val_func = (ValFunction)((Unit)(other)).heapValue;
                    if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
                }
            }
            if (other_type == typeof(ValFunction))
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

    public class ValIntrinsic : HeapValue
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValIntrinsic))
                {
                    if (function == (((Unit)other).heapValue as ValIntrinsic).function) return true;
                }
            }
            if (other_type == typeof(ValIntrinsic))
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

    public class ValClosure : HeapValue
    {
        public ValFunction function;
        public List<ValUpValue> upValues;

        public ValClosure(ValFunction p_function, List<ValUpValue> p_upValues)
        {
            function = p_function;
            upValues = p_upValues;
        }

        public void Register(Memory<Unit> p_variables)
        {
            foreach (ValUpValue u in upValues)
                u.Attach(p_variables);
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValClosure))
                {
                    if (this == ((Unit)other).heapValue as ValClosure) return true;
                }
            }
            if (other_type == typeof(ValClosure))
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

    public class ValUpValue : HeapValue
    {
        public Operand address;
        public Operand env;
        bool isCaptured;
        Memory<Unit> variables;
        Unit val;
        public Unit Val
        {
            get
            {
                if (isCaptured)
                    return val;
                else
                    return variables.GetAt(address, env);
            }
            set
            {
                if (isCaptured)
                    val = value;
                else
                    variables.SetAt(value, address, env);
            }
        }

        public ValUpValue(Operand p_address, Operand p_env)
        {
            address = p_address;
            env = p_env;
            isCaptured = false;
            variables = null;
            val = new Unit("null");
        }

        public void Attach(Memory<Unit> p_variables)
        {
            variables = p_variables;
        }

        public void Capture()
        {
            if (isCaptured == false)
            {
                isCaptured = true;
                val = variables.GetAt(address, env);
            }
        }
        public override string ToString()
        {
            return new string("upvalue " + address + " on env " + env + " HeapValue: " + Val.ToString());
        }

        public override bool ToBool()
        {
            return Val.ToBool();
        }

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValUpValue))
                {
                    if (Val.Equals(((Unit)other).heapValue)) return true;
                }
            }
            if (other_type == typeof(ValUpValue))
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

    public class ValTable : HeapValue
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
                elements.Add(new Unit("null"));
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValTable))
                {
                    if (this == ((Unit)other).heapValue as ValTable) return true;
                }
            }
            if (other_type == typeof(ValTable))
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if (this.name == (((Unit)other).heapValue as ValModule).name) return true;
            }
            if (other_type == typeof(ValModule))
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

    public class ValWrapper<T> : HeapValue
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapValueType() == typeof(ValWrapper<T>))
                {
                    if (this.content == (((Unit)other).heapValue as ValWrapper<T>).content) return true;
                }
            }
            if(other_type == typeof(ValWrapper<T>))
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
