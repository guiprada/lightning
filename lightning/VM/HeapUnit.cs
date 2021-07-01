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
    public abstract class HeapUnit
    {
        public abstract override string ToString();
        public abstract bool ToBool();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }

    public class StringUnit : HeapUnit
    {
        public string content;

        public StringUnit(string value)
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
                if(((Unit)other).HeapUnitType() == typeof(StringUnit))
                    if(content == ((StringUnit)((Unit)(other)).heapUnitValue).content)
                        return true;
            }
            if(other_type == typeof(StringUnit))
            {
                if (((StringUnit)other).content == content)
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
    }

    public class FunctionUnit : HeapUnit
    {
        public string name;
        public Operand arity;
        public List<Instruction> body;
        public LineCounter lineCounter;
        public string module;
        public Operand originalPosition;

        public FunctionUnit(string p_name, string p_module)
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
                if(((Unit)other).HeapUnitType() == typeof(FunctionUnit))
                {
                    FunctionUnit other_val_func = (FunctionUnit)((Unit)(other)).heapUnitValue;
                    if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
                }
            }
            if (other_type == typeof(FunctionUnit))
            {
                FunctionUnit other_val_func = other as FunctionUnit;
                if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + module.GetHashCode();
        }
    }

    public class IntrinsicUnit : HeapUnit
    {
        public string name;
        public Func<VM, Unit> function;
        public int arity;

        public IntrinsicUnit(string p_name, Func<VM, Unit> p_function, int p_arity)
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
                if(((Unit)other).HeapUnitType() == typeof(IntrinsicUnit))
                {
                    if (function == (((Unit)other).heapUnitValue as IntrinsicUnit).function) return true;
                }
            }
            if (other_type == typeof(IntrinsicUnit))
            {
                if (function == (other as IntrinsicUnit).function) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    public class ClosureUnit : HeapUnit
    {
        public FunctionUnit function;
        public List<UpValueUnit> upValues;

        public ClosureUnit(FunctionUnit p_function, List<UpValueUnit> p_upValues)
        {
            function = p_function;
            upValues = p_upValues;
        }

        public void Register(Memory<Unit> p_variables)
        {
            foreach (UpValueUnit u in upValues)
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
                if(((Unit)other).HeapUnitType() == typeof(ClosureUnit))
                {
                    if (this == ((Unit)other).heapUnitValue as ClosureUnit) return true;
                }
            }
            if (other_type == typeof(ClosureUnit))
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

    public class UpValueUnit : HeapUnit
    {
        public Operand address;
        public Operand env;
        bool isCaptured;
        Memory<Unit> variables;
        Unit value;
        public Unit UpValue
        {
            get
            {
                if (isCaptured)
                    return value;
                else
                    return variables.GetAt(address, env);
            }
            set
            {
                if (isCaptured)
                    this.value = value;
                else
                    variables.SetAt(value, address, env);
            }
        }

        public UpValueUnit(Operand p_address, Operand p_env)
        {
            address = p_address;
            env = p_env;
            isCaptured = false;
            variables = null;
            value = new Unit("null");
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
                value = variables.GetAt(address, env);
            }
        }
        public override string ToString()
        {
            return new string("upvalue " + address + " on env " + env + " HeapUnit: " + UpValue.ToString());
        }

        public override bool ToBool()
        {
            return UpValue.ToBool();
        }

        public override bool Equals(object other)
        {
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).HeapUnitType() == typeof(UpValueUnit))
                {
                    if (UpValue.Equals(((Unit)other).heapUnitValue)) return true;
                }
            }
            if (other_type == typeof(UpValueUnit))
            {
                if (UpValue.Equals(other)) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return UpValue.GetHashCode();
        }

    }

    public class TableUnit : HeapUnit
    {
        public List<Unit> elements;
        public Dictionary<StringUnit, Unit> table;


        public int ECount { get { return elements.Count; } }
        public int TCount { get { return table.Count; } }
        public int Count { get { return ECount + TCount; } }
        public TableUnit(List<Unit> p_elements, Dictionary<StringUnit, Unit> p_table)
        {
            elements = p_elements ??= new List<Unit>();
            table = p_table ??= new Dictionary<StringUnit, Unit>();
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

        public void TableSet(StringUnit index, Unit value)
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
            foreach (KeyValuePair<StringUnit, Unit> entry in table)
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
                if(((Unit)other).HeapUnitType() == typeof(TableUnit))
                {
                    if (this == ((Unit)other).heapUnitValue as TableUnit) return true;
                }
            }
            if (other_type == typeof(TableUnit))
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

    public class ModuleUnit : TableUnit
    {
        public string name;
        public List<Unit> globals;
        public List<Unit> constants;
        public Operand importIndex;
        public ModuleUnit(string p_name, List<Unit> p_elements, Dictionary<StringUnit, Unit> p_table, List<Unit> p_globals, List<Unit> p_constants)
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
                if (this.name == (((Unit)other).heapUnitValue as ModuleUnit).name) return true;
            }
            if (other_type == typeof(ModuleUnit))
            {
                if ((other as ModuleUnit).name == this.name) return true;
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

    public class WrapperUnit<T> : HeapUnit
    {
        public object content;

        public WrapperUnit(object content)
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
                if(((Unit)other).HeapUnitType() == typeof(WrapperUnit<T>))
                {
                    if (this.content == (((Unit)other).heapUnitValue as WrapperUnit<T>).content) return true;
                }
            }
            if(other_type == typeof(WrapperUnit<T>))
            {
                if(((WrapperUnit<T>)other).content == this.content)
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
