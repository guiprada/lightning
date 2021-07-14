using System;
using System.Collections.Generic;

using Operand = System.UInt16;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    public enum UnitType{
        Float,
        Integer,
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
        public abstract Unit Get(Unit p_key);
        public abstract void Set(Unit p_key, Unit p_value);
    }

    public class TypeUnit : HeapUnit{
        public static TypeUnit Float = new TypeUnit(UnitType.Float);
        public static TypeUnit Integer = new TypeUnit(UnitType.Integer);
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
            if(this.type == UnitType.Float)
                return "UnitType.Float";
            else if(this.type == UnitType.Integer)
                return "UnitType.Integer";
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
            throw new Exception("Trying to get a boolean value of TypeUnit. " + VM.ErrorString(null));
        }

        public override bool Equals(object other){
            if(other.GetType() == typeof(TypeUnit))
                return this.type == ((TypeUnit)other).type;
            return false;
        }
        public override int GetHashCode(){
            return this.type.GetHashCode();
        }

        public override Unit Get(Unit p_key){
            throw new Exception("Trying to Get a Table value of TypeUnit. " + VM.ErrorString(null));
        }

        public override void Set(Unit p_key, Unit p_value){
            throw new Exception("Trying to Set a Table value of TypeUnit. " + VM.ErrorString(null));
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
            throw new Exception("Can not convert Function to Bool. " + VM.ErrorString(null));
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

        public override Unit Get(Unit p_key){
            throw new Exception("Trying to Get a Table value of FunctionUnit. " + VM.ErrorString(null));
        }

        public override void Set(Unit p_key, Unit p_value){
            throw new Exception("Trying to Set a Table value of FunctionUnit. " + VM.ErrorString(null));
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
            throw new Exception("Can not convert Intrinsic to Bool. " + VM.ErrorString(null));
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

        public override Unit Get(Unit p_key){
            throw new Exception("Trying to Get a Table value of IntrinsicUnit. " + VM.ErrorString(null));
        }

        public override void Set(Unit p_key, Unit p_value){
            throw new Exception("Trying to Set a Table value of IntrinsicUnit. " + VM.ErrorString(null));
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
            throw new Exception("Can not convert Clojure to Bool. " + VM.ErrorString(null));
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

        public override Unit Get(Unit p_key){
            throw new Exception("Trying to Get a Table value of ClosureUnit. " + VM.ErrorString(null));
        }

        public override void Set(Unit p_key, Unit p_value){
            throw new Exception("Trying to Set a Table value of ClosureUnit. " + VM.ErrorString(null));
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

        public override Unit Get(Unit p_key){
            throw new Exception("Trying to Get a Table value of UpValueUnit. " + VM.ErrorString(null));
        }

        public override void Set(Unit p_key, Unit p_value){
            throw new Exception("Trying to Set a Table value of UpValueUnit. " + VM.ErrorString(null));
        }
    }

    public class ModuleUnit : HeapUnit
    {
        public string name;
        public List<Unit> elements;
        public Dictionary<Unit, Unit> table;
        public List<Unit> globals;
        public List<Unit> constants;
        public Operand importIndex;
        public override UnitType Type{
            get{
                return UnitType.Module;
            }
        }
        public ModuleUnit(string p_name, List<Unit> p_elements, Dictionary<Unit, Unit> p_table, List<Unit> p_globals, List<Unit> p_constants)
        {
            name = p_name;
            elements = p_elements ?? new List<Unit>();
            table = p_table ?? new Dictionary<Unit, Unit>();
            globals = p_globals ??= new List<Unit>();
            constants = p_constants ??= new List<Unit>();
            importIndex = 0;
        }
        public override bool ToBool()
        {
            throw new Exception("Can not convert Module to Bool. " + VM.ErrorString(null));
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

        public override void Set(Unit index, Unit value)
        {
            table[index] = value;
        }

        public override Unit Get(Unit p_key){
            if(table.ContainsKey(p_key)){
                return table[p_key];
            }else{
                throw new Exception("Module Table does not contain index: " + p_key.ToString());
            }
        }
    }

    public class WrapperUnit<T> : HeapUnit
    {
        public object content;
        Dictionary<Unit, Unit> table;
        private TableUnit superTable;
        public override UnitType Type{
            get{
                return UnitType.Wrapper;
            }
        }
        public WrapperUnit(object p_content, TableUnit p_superTable = null)
        {
            content = p_content;
            table = new Dictionary<Unit, Unit>();
            superTable = p_superTable;
        }

        public override void Set(Unit index, Unit value)
        {
            table[index] = value;
        }

        public override Unit Get(Unit p_key){
            if(table.ContainsKey(p_key))
                return table[p_key];
            else if(superTable != null)
                return superTable.Get(p_key);
            throw new Exception("Wrapper<" + typeof(T) +"> Super Table does not contain index: " + p_key.ToString());
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
            throw new Exception("Can not convert a Wrapper<" + typeof(T) +"> to Bool. " + VM.ErrorString(null));
        }

        public T UnWrapp()
        {
            if (this.content.GetType() == typeof(T))
            {
                return (T)this.content;
            }
            else
            {
                throw new Exception("UnWrapp<" + typeof(T) +">() type Error! " + VM.ErrorString(null));
            }
        }
    }
}
