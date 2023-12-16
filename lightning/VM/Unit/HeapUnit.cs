using System;
using System.Collections.Generic;

#if DOUBLE
using Float = System.Double;
using Integer = System.Int64;
using Operand = System.UInt16;
#else
	using Float = System.Single;
	using Integer = System.Int32;
	using Operand = System.UInt16;
#endif

namespace lightning
{
	public enum UnitType
	{
		Float
		, Integer
		, Null
		, Boolean
		, String
		, Char
		, Function
		, Intrinsic
		, ExternalFunction
		, Closure
		, UpValue
		, Table
		, List
		, Module
		, Wrapper
	}

	public abstract class HeapUnit
	{
		public abstract UnitType Type { get; }
		public abstract override string ToString();
		public virtual bool ToBool()
		{
			throw new Exception("Can not convert " + Type + " to Bool.");
		}
		public abstract override bool Equals(object other);
		public abstract override int GetHashCode();
		public virtual Unit Get(Unit p_key)
		{
			throw new Exception("Trying to Get a Table value of " + Type);
		}
		public virtual void Set(Unit index, Unit value)
		{
			throw new Exception("Trying to Set a Table value of " + Type);
		}
		public virtual void SetExtensionTable(TableUnit p_ExtensionTable)
		{
			throw new Exception("Trying to set a Extension Table of a " + Type);
		}
		public virtual void UnsetExtensionTable()
		{
			throw new Exception("Trying to unset a Extension Table of a " + Type);
		}
		public virtual TableUnit GetExtensionTable()
		{
			throw new Exception("Trying to get a Extension Table of a " + Type);
		}
		public virtual int CompareTo(object p_compareTo)
		{
			throw new Exception("Trying to compare a " + Type);
		}
	}

	public class TypeUnit : HeapUnit
	{
		public static TypeUnit Float = new TypeUnit(UnitType.Float);
		public static TypeUnit Integer = new TypeUnit(UnitType.Integer);
		public static TypeUnit Null = new TypeUnit(UnitType.Null);
		public static TypeUnit Boolean = new TypeUnit(UnitType.Boolean);
		public static TypeUnit Char = new TypeUnit(UnitType.Char);

		UnitType type;

		public override UnitType Type
		{
			get
			{
				return type;
			}
		}

		private TypeUnit(UnitType p_type)
		{
			type = p_type;
		}

		public override string ToString()
		{
			if (this.type == UnitType.Float)
				return "UnitType.Float";
			else if (this.type == UnitType.Integer)
				return "UnitType.Integer";
			else if (this.type == UnitType.Null)
				return "UnitType.Null";
			else if (this.type == UnitType.Boolean)
				return "UnitType.Boolean";
			else if (this.type == UnitType.Char)
				return "UnitType.Char";
			else
				return "Unknown UnitType";
		}

		public override bool Equals(object p_other)
		{
			if (p_other.GetType() == typeof(TypeUnit))
				return this.type == ((TypeUnit)p_other).type;
			return false;
		}
		public override int GetHashCode()
		{
			return this.type.GetHashCode();
		}
	}

	public class FunctionUnit : HeapUnit
	{
		public string Name { get; private set; }
		public Operand Arity { get; private set; }
		public List<Instruction> Body { get; private set; }
		public ChunkPosition ChunkPosition { get; private set; }
		public string Module { get; private set; }
		public Operand OriginalPosition { get; private set; }

		public override UnitType Type
		{
			get
			{
				return UnitType.Function;
			}
		}

		public FunctionUnit(string p_Name, string p_Module)
		{
			Name = p_Name;
			Module = p_Module;
		}

		public void Set(Operand p_Arity, List<Instruction> p_Body, ChunkPosition p_ChunkPosition, Operand p_OriginalPosition)
		{
			Arity = p_Arity;
			Body = p_Body;
			ChunkPosition = p_ChunkPosition;
			OriginalPosition = p_OriginalPosition;
		}
		public override string ToString()
		{
			string str = new string("fun" + " " + Name + ":" + Module + " " + " (" + Arity + ")\n");
			foreach (Instruction i in Body)
			{
				str += i.ToString() + "\n";
			}
			return str;
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.Function)
				{
					FunctionUnit other_val_func = (FunctionUnit)((Unit)(p_other)).heapUnitValue;
					if (other_val_func.Name == this.Name && other_val_func.Module == this.Module) return true;
				}
			}
			if (other_type == typeof(FunctionUnit))
			{
				FunctionUnit other_val_func = p_other as FunctionUnit;
				if (other_val_func.Name == this.Name && other_val_func.Module == this.Module) return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() + Module.GetHashCode();
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare a FunctionUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.Function:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
					return 1;
				case UnitType.Intrinsic:
				case UnitType.ExternalFunction:
				case UnitType.Closure:
				case UnitType.UpValue:
				case UnitType.Module:
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare a FunctionUnit to unkown UnitType.");
			}
		}
	}

	public class IntrinsicUnit : HeapUnit
	{
		public string Name { get; private set; }
		public Func<VM, Unit> Function { get; private set; }
		public Operand Arity { get; private set; }

		public override UnitType Type
		{
			get
			{
				return UnitType.Intrinsic;
			}
		}

		public IntrinsicUnit(string p_Name, Func<VM, Unit> p_Function, Operand p_Arity)
		{
			Name = p_Name;
			Function = p_Function;
			Arity = p_Arity;
		}

		public override string ToString()
		{
			return new string("Intrinsic " + Name + "(" + Arity + ")");
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.Intrinsic)
				{
					if (Function == (((Unit)p_other).heapUnitValue as IntrinsicUnit).Function) return true;
				}
			}
			if (other_type == typeof(IntrinsicUnit))
			{
				if (Function == (p_other as IntrinsicUnit).Function) return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare an IntrinsicUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.Intrinsic:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
					return 1;
				case UnitType.ExternalFunction:
				case UnitType.Closure:
				case UnitType.UpValue:
				case UnitType.Module:
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare an IntrinsicUnit to unkown UnitType.");
			}
		}
	}

public class ExternalFunctionUnit : HeapUnit
	{
		public string Name { get; private set; }
		public System.Reflection.MethodInfo Function { get; private set; }
		public Operand Arity { get; private set; }

		public override UnitType Type
		{
			get
			{
				return UnitType.ExternalFunction;
			}
		}

		public ExternalFunctionUnit(string p_Name, System.Reflection.MethodInfo p_Function, Operand p_Arity)
		{
			if (p_Function != null)
			{
				Name = p_Name;
				Function = p_Function;
				Arity = p_Arity;
			}
			else
				throw new Exception("Cannot create ExternalFunctionUnit from null!");
		}

		public override string ToString()
		{
			return new string("ExternalFunctionUnit " + Name + "(" + Arity + ")");
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.ExternalFunction)
				{
					if (Function == (((Unit)p_other).heapUnitValue as ExternalFunctionUnit).Function) return true;
				}
			}
			if (other_type == typeof(ExternalFunctionUnit))
			{
				if (Function == (p_other as ExternalFunctionUnit).Function) return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare an ExternalFunctionUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.ExternalFunction:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
				case UnitType.Intrinsic:
					return 1;
				case UnitType.Closure:
				case UnitType.UpValue:
				case UnitType.Module:
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare an ExternalFunctionUnit to unkown UnitType.");
			}
		}
	}

	public class ClosureUnit : HeapUnit
	{
		public FunctionUnit Function { get; private set; }
		public List<UpValueUnit> UpValues { get; private set; }
		public override UnitType Type
		{
			get
			{
				return UnitType.Closure;
			}
		}

		public ClosureUnit(FunctionUnit p_Function, List<UpValueUnit> p_UpValues)
		{
			Function = p_Function;
			UpValues = p_UpValues;
		}

		public override string ToString()
		{
			return new string("Closure of " + Function.ToString());
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.Closure)
				{
					if (this == ((Unit)p_other).heapUnitValue as ClosureUnit) return true;
				}
			}
			if (other_type == typeof(ClosureUnit))
			{
				if (p_other == this) return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return UpValues.GetHashCode() + Function.GetHashCode();
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare a ClosureUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.Closure:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
				case UnitType.Intrinsic:
				case UnitType.ExternalFunction:
					return 1;
				case UnitType.UpValue:
				case UnitType.Module:
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare a ClosureUnit to unkown UnitType.");
			}
		}
	}

	public class UpValueUnit : HeapUnit
	{
		public Operand Address { get; private set; }
		public Operand Env { get; private set; }
		private bool isCaptured;
		public bool IsCaptured { get { return isCaptured; } }
		private Memory<Unit> variables;
		private Unit value;
		public override UnitType Type
		{
			get
			{
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
					return variables.GetAt(Address, Env);
			}
			set
			{
				if (isCaptured)
					this.value = value;
				else
					variables.SetAt(value, Address, Env);
			}
		}

		public UpValueUnit(Operand p_Address, Operand p_Env)
		{
			Address = p_Address;
			Env = p_Env;
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
				value = variables.GetAt(Address, Env);
			}
		}
		public override string ToString()
		{
			return new string("upvalue " + Address + " on env " + Env + " HeapUnit: " + UpValue.ToString());
		}

		public override bool ToBool()
		{
			return UpValue.ToBool();
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.UpValue)
				{
					if (UpValue.Equals(((Unit)p_other).heapUnitValue)) return true;
				}
			}
			if (other_type == typeof(UpValueUnit))
			{
				if (UpValue.Equals(p_other)) return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return UpValue.GetHashCode();
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare a UpValueUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.UpValue:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
				case UnitType.Intrinsic:
				case UnitType.ExternalFunction:
				case UnitType.Closure:
					return 1;
				case UnitType.Module:
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare a UpValueUnit to unkown UnitType.");
			}
		}
	}

	public class ModuleUnit : HeapUnit
	{
		public string Name { get; private set; }
		public Dictionary<Unit, Unit> Table { get; private set; }
		public List<Unit> globals;
		public List<Unit> Globals { get { return globals; } private set { globals = value; } }
		public List<Unit> Data { get; private set; }
		public Operand ImportIndex { get; set; }
		public override UnitType Type
		{
			get
			{
				return UnitType.Module;
			}
		}
		public ModuleUnit(string p_Name, Dictionary<Unit, Unit> p_Table, List<Unit> p_Globals, List<Unit> p_Data)
		{
			Name = p_Name;
			Table = p_Table ?? new Dictionary<Unit, Unit>();
			Globals = p_Globals ??= new List<Unit>();
			Data = p_Data ??= new List<Unit>();
			ImportIndex = 0;
		}

		public Unit GetGlobal(Operand p_index)
		{
			Unit value;
			lock (globals[p_index].heapUnitValue)
				value = globals[p_index];
			return value;
		}

		public void SetGlobal(Unit p_value, Operand p_index)
		{
			lock (globals[p_index].heapUnitValue)
				globals[p_index] = p_value;
		}

		public void SetOpGlobal(Unit p_value, Operand op, Operand p_index)
		{
			switch (op)
			{
				case VM.ASSIGN:
					lock (globals[p_index].heapUnitValue)
						globals[p_index] = p_value;
					break;
				case VM.ADDITION_ASSIGN:
					lock (globals[p_index].heapUnitValue)
						globals[p_index] += p_value;
					break;
				case VM.SUBTRACTION_ASSIGN:
					lock (globals[p_index].heapUnitValue)
						globals[p_index] -= p_value;
					break;
				case VM.MULTIPLICATION_ASSIGN:
					lock (globals[p_index].heapUnitValue)
						globals[p_index] *= p_value;
					break;
				case VM.DIVISION_ASSIGN:
					lock (globals[p_index].heapUnitValue)
						globals[p_index] /= p_value;
					break;
				default:
					throw new Exception("Unknown operator");
			}
		}

		public Unit GetData(Operand p_index)
		{
			return Data[p_index];
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (this.Name == (((Unit)p_other).heapUnitValue as ModuleUnit).Name) return true;
			}
			if (other_type == typeof(ModuleUnit))
			{
				if ((p_other as ModuleUnit).Name == this.Name) return true;
			}

			return false;
		}
		public override string ToString()
		{
			return "module" + Name;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override void Set(Unit index, Unit value)
		{
			Table[index] = value;
		}

		public override Unit Get(Unit p_key)
		{
			Unit this_unit;
			if (Table.TryGetValue(p_key, out this_unit))
			{
				return this_unit;
			}
			else
			{
				throw new Exception("Module Table does not contain index:" + p_key.ToString());
			}
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare a ModuleUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.Module:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
				case UnitType.Intrinsic:
				case UnitType.ExternalFunction:
				case UnitType.Closure:
				case UnitType.UpValue:
					return 1;
				case UnitType.Wrapper:
					return -1;
				default:
					throw new Exception("Trying to compare a ModuleUnit to unkown UnitType.");
			}
		}
	}

	public class WrapperUnit<T> : HeapUnit
	{
		public object content;
		public TableUnit ExtensionTable { get; private set; }
		public override UnitType Type
		{
			get
			{
				return UnitType.Wrapper;
			}
		}
		public WrapperUnit(object p_content, TableUnit p_ExtentionTable = null)
		{
			content = p_content;
			ExtensionTable = p_ExtentionTable;
		}

		public override Unit Get(Unit p_key)
		{
			return ExtensionTable.Get(p_key);
		}

		public override string ToString()
		{
			return content.ToString();
		}

		public override bool Equals(object p_other)
		{
			Type other_type = p_other.GetType();
			if (other_type == typeof(Unit))
			{
				if (((Unit)p_other).Type == UnitType.Wrapper)
				{
					if (this.content == (((Unit)p_other).heapUnitValue as WrapperUnit<T>).content) return true;
				}
			}
			if (other_type == typeof(WrapperUnit<T>))
			{
				if (((WrapperUnit<T>)p_other).content == this.content)
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
		public T UnWrap()
		{
			if (this.content.GetType() == typeof(T))
			{
				return (T)this.content;
			}
			else
			{
				throw new Exception("UnWrap<" + typeof(T) + ">() type Error!");
			}
		}

		public override void SetExtensionTable(TableUnit p_ExtensionTable)
		{
			ExtensionTable = p_ExtensionTable;
		}

		public override void UnsetExtensionTable()
		{
			ExtensionTable = null;
		}

		public override TableUnit GetExtensionTable()
		{
			return ExtensionTable;
		}

		public override int CompareTo(object p_compareTo)
		{
			if (p_compareTo.GetType() != typeof(Unit))
				throw new Exception("Trying to compare a WrapperUnit to non Unit type");
			Unit other = (Unit)p_compareTo;
			UnitType other_type = other.Type;
			switch (other_type)
			{
				case UnitType.Wrapper:
					return 0;
				case UnitType.Float:
				case UnitType.Integer:
				case UnitType.Char:
				case UnitType.Null:
				case UnitType.Boolean:
				case UnitType.String:
				case UnitType.Table:
				case UnitType.List:
				case UnitType.Function:
				case UnitType.Intrinsic:
				case UnitType.ExternalFunction:
				case UnitType.Closure:
				case UnitType.UpValue:
				case UnitType.Module:
					return 1;
				default:
					throw new Exception("Trying to compare a WrapperUnit to unkown UnitType.");
			}
		}
	}
}
