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
    public enum UnitType{
        Number,
        Null,
        Boolean,
        String,
        Char,
        Function,
        Intrinsic,
        Closure,
        UpValue,
        Table,
        Module,
        Wrapper
    }

    public abstract class HeapUnit
    {
        public abstract UnitType Type{get;}
        public abstract override string ToString();
        public abstract bool ToBool();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }

    public class TypeUnit : HeapUnit{
        public static TypeUnit Number = new TypeUnit(UnitType.Number);
        public static TypeUnit Null = new TypeUnit(UnitType.Null);
        public static TypeUnit Boolean = new TypeUnit(UnitType.Boolean);
        public static TypeUnit Char = new TypeUnit(UnitType.Char);

        UnitType type;

        public override UnitType Type{
            get{
                return type;
            }
        }

        private TypeUnit(UnitType p_type){
            type = p_type;
        }

        public override string ToString(){
            if(this.type == UnitType.Number)
                return "UnitType.Number";
            else if(this.type == UnitType.Null)
                return "UnitType.Null";
            else if(this.type == UnitType.Boolean)
                return "UnitType.Boolean";
            else if(this.type == UnitType.Char)
                return "UnitType.Char";
            else
                return "Unknown UnitType";
        }
        public override bool ToBool(){
            throw new Exception("Trying to get a boolean value of TypeUnit");
        }

        public override bool Equals(object other){
            if(other.GetType() == typeof(TypeUnit))
                return this.type == ((TypeUnit)other).type;
            return false;
        }
        public override int GetHashCode(){
            return this.type.GetHashCode();
        }
    }

    public class StringUnit : HeapUnit
    {
        public string content;

        public override UnitType Type{
            get{
                return UnitType.String;
            }
        }

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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.String)
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

        public override UnitType Type{
            get{
                return UnitType.Function;
            }
        }

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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.Function)
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

        public override UnitType Type{
            get{
                return UnitType.Intrinsic;
            }
        }

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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.Intrinsic)
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
        public override UnitType Type{
            get{
                return UnitType.Closure;
            }
        }

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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.Closure)
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
        public override UnitType Type{
            get{
                return UnitType.UpValue;
            }
        }
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
            value = new Unit(UnitType.Null);
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
                if(((Unit)other).Type == UnitType.UpValue)
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

        public TableUnit superTable;

        public override UnitType Type{
            get{
                return UnitType.Table;
            }
        }
        public int ECount {
            get{
                return elements.Count;
            }
        }
        public int TCount {
            get{
                return table.Count;
            }
        }
        public int Count {
            get{
                return ECount + TCount;
            }
        }
        public TableUnit(List<Unit> p_elements, Dictionary<StringUnit, Unit> p_table)
        {
            elements = p_elements ??= new List<Unit>();
            table = p_table ??= new Dictionary<StringUnit, Unit>();
        }

        public Unit Get(Unit p_key){
            if (p_key.Type == UnitType.String)
                return GetTable((StringUnit)(p_key.heapUnitValue));
            if (p_key.Type == UnitType.Number)
                return elements[(int)p_key.unitValue];
            else
                throw new Exception("Table can not be indexed by: " + p_key.Type);
        }
        public Unit GetTable(StringUnit p_string){
            if(table.ContainsKey(p_string)){
                return table[p_string];
            }else if(superTable != null){
                return superTable.GetTable(p_string);
            }else{
                throw new Exception("Table or Super Table does not contain index: " + p_string.ToString());
            }
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

        public void TableSet(string index, Unit value)
        {
            table[new StringUnit(index)] = value;
        }

        public void TableSet(StringUnit index, Number value)
        {
            table[index] = new Unit(value);
        }

        public void TableSet(string index, Number value)
        {
            table[new StringUnit(index)] = new Unit(value);
        }

        public void TableSet(StringUnit index, HeapUnit value)
        {
            table[index] = new Unit(value);
        }

        public void TableSet(string index, HeapUnit value)
        {
            table[new StringUnit(index)] = new Unit(value);
        }

        public void TableSet(StringUnit index, bool value)
        {
            table[index] = new Unit(value);
        }

        public void TableSet(string index, bool value)
        {
            table[new StringUnit(index)] = new Unit(value);
        }

        void ElementsStretch(int n)
        {
            for (int i = 0; i < n; i++)
            {
                elements.Add(new Unit(UnitType.Null));
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
            Type other_type = other.GetType();
            if (other_type == typeof(Unit))
            {
                if(((Unit)other).Type == UnitType.Table)
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
        public override UnitType Type{
            get{
                return UnitType.Module;
            }
        }
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
        public override UnitType Type{
            get{
                return UnitType.Wrapper;
            }
        }
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
                if(((Unit)other).Type == UnitType.Wrapper)
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
