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
    public abstract class Unit
    {
        static NullUnit global_null = new NullUnit();
        public static NullUnit Null { get { return global_null; } }

        static BoolUnit global_false = new BoolUnit(false);
        public static BoolUnit False { get { return global_false; } }

        static BoolUnit global_true = new BoolUnit(true);
        public static BoolUnit True { get { return global_true; } }

        public abstract override string ToString();
        public abstract bool ToBool();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }

    public class BoolUnit : Unit
    {
        bool content;

        public BoolUnit(bool value)
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
            if (other.GetType() == typeof(BoolUnit))
            {
                if (((BoolUnit)other).content == this.content) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
    }

    public class NumberUnit : Unit
    {
        public Number content;
        public bool referenced;
        public bool stacked;
        public NumberUnit(Number value)
        {
            content = value;
            referenced = false;
            stacked = false;
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
            if (other.GetType() == typeof(NumberUnit))
            {
                if (((NumberUnit)other).content == content) return true;
                else return false;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
    }

    public class NullUnit : Unit
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
            if (other.GetType() == typeof(NullUnit))
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

    public class StringUnit : Unit
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
            throw new Exception("Can not convert String to Bool.");
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(StringUnit))
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

    public class FunctionUnit : Unit
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
            throw new Exception("Can not convert Function to Bool.");
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(FunctionUnit))
            {
                FunctionUnit other_val_func = (FunctionUnit)other;
                if (other_val_func.name == this.name && other_val_func.module == this.module) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() + module.GetHashCode();
        }
    }

    public class IntrinsicUnit : Unit
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
            throw new Exception("Can not convert Intrinsic to Bool.");
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(IntrinsicUnit))
            {
                if (function == ((IntrinsicUnit)other).function)
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    public class ClosureUnit : Unit
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
            throw new Exception("Can not convert Clojure to Bool.");
        }


        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(ClosureUnit))
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

    public class UpValueUnit : Unit
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
            value = Unit.Null;
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
                if(value.GetType() == typeof(NumberUnit))
                    ((NumberUnit)value).referenced = true;
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
            if (other.GetType() == typeof(UpValueUnit))
            {
                if (((UpValueUnit)other).UpValue == UpValue) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return UpValue.GetHashCode();
        }

    }

    public class TableUnit : Unit
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

            if(value.GetType() == typeof(NumberUnit))
                ((NumberUnit)value).referenced = true;
        }

        public void ElementAdd(Unit value)
        {
            elements.Add(value);
            if(value.GetType() == typeof(NumberUnit))
                ((NumberUnit)value).referenced = true;
        }

        public void TableSet(StringUnit index, Unit value)
        {
            table[index] = value;
            if(value.GetType() == typeof(NumberUnit))
                ((NumberUnit)value).referenced = true;
        }

        public void TableSet(string index, Unit value)
        {
            table[new StringUnit(index)] = value;
            if(value.GetType() == typeof(NumberUnit))
                ((NumberUnit)value).referenced = true;
        }

        public void TableSet(StringUnit index, Number value)
        {
            table[index] = new NumberUnit(value);
            if(table[index].GetType() == typeof(NumberUnit))
                ((NumberUnit)table[index]).referenced = true;
        }

        public void TableSet(string index, Number value)
        {
            StringUnit string_unit_index = new StringUnit(index);
            table[string_unit_index] = new NumberUnit(value);
            if(table[string_unit_index].GetType() == typeof(NumberUnit))
                ((NumberUnit)table[string_unit_index]).referenced = true;
        }

        void ElementsStretch(int n)
        {
            for (int i = 0; i < n; i++)
            {
                elements.Add(Unit.Null);
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
            throw new Exception("Can not convert List to Bool.");
        }

        public override bool Equals(object other)
        {
            if (other.GetType() == typeof(TableUnit))
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
            if (other.GetType() == typeof(ModuleUnit))
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

    public class WrapperUnit<T> : Unit
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
            if(other.GetType() == typeof(WrapperUnit<T>))
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
            throw new Exception("Can not convert a Wrapper to Bool.");
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
