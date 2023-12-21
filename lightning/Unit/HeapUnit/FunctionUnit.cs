using System;
using System.Collections.Generic;

using lightningChunk;

namespace lightningUnit
{
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
    }
}