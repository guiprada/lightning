using System;
using System.Collections.Generic;

using lightningExceptions;
using lightningTools;
using lightningVM;

namespace lightningUnit
{
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
                    Logger.LogLine("Unknown assignment operator: " + op, Defaults.Config.VMLogFile);
                    throw Exceptions.unknown_operator;
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
                return this_unit;
            else
            {
                Logger.LogLine("Module Table does not contain index: " + p_key.ToString(), Defaults.Config.VMLogFile);
                throw Exceptions.not_found;
            }
        }
    }
}