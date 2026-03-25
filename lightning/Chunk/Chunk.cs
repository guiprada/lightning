using System;
using System.Collections.Generic;
using System.IO;


// using lightningPrelude;
using lightningUnit;
using lightningPrelude;
using lightningExceptions;
using lightningTools;

namespace lightningChunk
{
    public struct PositionData
    {
        int line;
        int column;
        public PositionData(int p_line, int p_column)
        {
            line = p_line;
            column = p_column;
        }
        public override string ToString()
        {
            return "(" + line + "," + column + ")";
        }
        public void Write(BinaryWriter p_w)
        {
            p_w.Write(line);
            p_w.Write(column);
        }
        public static PositionData Read(BinaryReader p_r)
        {
            int line = p_r.ReadInt32();
            int column = p_r.ReadInt32();
            return new PositionData(line, column);
        }
    }

    public struct ChunkPosition
    {
        public List<PositionData> positions;

        public ChunkPosition(List<PositionData> p_positions)
        {
            positions = p_positions;
        }
        public void AddPosition(PositionData p_positionData)
        {
            positions.Add(p_positionData);
        }
        public PositionData GetPosition(int p_instructionAddress)
        {
            return positions[p_instructionAddress];
        }
        public ChunkPosition Slice(int p_start, int p_end)
        {
            int range = p_end - p_start;
            List<PositionData> positionSlice = positions.GetRange(p_start, range);
            positions.RemoveRange(p_start, range);

            return new ChunkPosition(positionSlice);
        }
    }

    public enum AssignmentOperatorType
    {
        ASSIGN,
        ADDITION_ASSIGN,
        SUBTRACTION_ASSIGN,
        MULTIPLICATION_ASSIGN,
        DIVISION_ASSIGN,
    }

    public struct Instruction
    {
        public OpCode opCode;
        public Operand opA;
        public Operand opB;
        public Operand opC;
        public Instruction(OpCode p_opCode, Operand p_opA, Operand p_opB, Operand p_opC)
        {
            opCode = p_opCode;
            opA = p_opA;
            opB = p_opB;
            opC = p_opC;
        }

        public override string ToString()
        {
            OpCode op = this.opCode;
            switch (op)
            {
                // 0 op
                case OpCode.DECLARE_VARIABLE:
                case OpCode.DECLARE_GLOBAL:
                case OpCode.OPEN_ENV:
                case OpCode.CLOSE_ENV:
                case OpCode.RETURN:
                case OpCode.ADD:
                case OpCode.APPEND:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.NEGATE:
                case OpCode.NOT:
                case OpCode.AND:
                case OpCode.OR:
                case OpCode.XOR:
                case OpCode.EQUALS:
                case OpCode.NOT_EQUALS:
                case OpCode.GREATER_EQUALS:
                case OpCode.LESS_EQUALS:
                case OpCode.GREATER:
                case OpCode.LESS:
                case OpCode.EXIT:
                case OpCode.LOAD_VOID:
                case OpCode.POP:
                case OpCode.DUP:
                case OpCode.PUSH_STASH:
                case OpCode.POP_STASH:
                case OpCode.CALL:
                case OpCode.CLOSE_CLOSURE:
                case OpCode.CLOSE_FUNCTION:
                case OpCode.LOAD_FALSE:
                case OpCode.LOAD_TRUE:
                case OpCode.DECLARE_CONST_VARIABLE:
                case OpCode.MAKE_CONST:
                case OpCode.MAKE_MOVE:
                    return op.ToString();

                // 1 op
                case OpCode.LOAD_DATA:
                case OpCode.LOAD_GLOBAL:
                case OpCode.LOAD_UPVALUE:
                case OpCode.LOAD_INTRINSIC:
                case OpCode.JUMP:
                case OpCode.JUMP_IF_NOT_TRUE:
                case OpCode.JUMP_BACK:
                case OpCode.RETURN_SET:
                case OpCode.GET:
                case OpCode.NEW_TABLE:
                case OpCode.NEW_LIST:
                    return op.ToString() + " " + this.opA;
                // 2 op
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.ASSIGN_UPVALUE:
                case OpCode.LOAD_VARIABLE:
                case OpCode.LOAD_IMPORTED_GLOBAL:
                case OpCode.LOAD_IMPORTED_DATA:
                case OpCode.SET:
                    return op.ToString() + " " + this.opA + " " + this.opB;
                // 3 op
                case OpCode.ASSIGN_VARIABLE:
                case OpCode.DECLARE_FUNCTION:
                case OpCode.ASSIGN_IMPORTED_GLOBAL:
                    return op.ToString() + " " + this.opA + " " + this.opB + " " + this.opC;
                default:
                    return "Unkown Opcode: " + op.ToString();
            }
        }
    }
    public enum OpCode : Operand
    {
        LOAD_DATA,
        LOAD_VARIABLE,
        LOAD_GLOBAL,
        LOAD_IMPORTED_GLOBAL,
        LOAD_IMPORTED_DATA,
        LOAD_UPVALUE,
        LOAD_INTRINSIC,
        LOAD_VOID,
        LOAD_TRUE,
        LOAD_FALSE,

        DECLARE_GLOBAL,
        DECLARE_VARIABLE,
        DECLARE_CONST_VARIABLE,  // like DECLARE_VARIABLE but checks PROTECTION_CONST first
        MAKE_CONST,              // stamps PROTECTION_CONST on TOS Unit (for var const)
        MAKE_MOVE,               // stamps PROTECTION_MOVE on TOS heap Unit (for &arg call-site)
        DECLARE_FUNCTION,
        ASSIGN_VARIABLE,
        ASSIGN_GLOBAL,
        ASSIGN_IMPORTED_GLOBAL,
        ASSIGN_UPVALUE,
        GET,
        SET,

        POP,
        DUP,
        PUSH_STASH,
        POP_STASH,

        JUMP,    // Jumps n instructions
        JUMP_IF_NOT_TRUE,    // Jumps if not true
        JUMP_BACK,     // Jumps back n instructions

        OPEN_ENV,   // Open new env
        CLOSE_ENV,   // Close env
        RETURN,
        RETURN_SET,

        ADD, // Float
        APPEND, // String
        SUBTRACT, //
        MULTIPLY,
        DIVIDE,
        NEGATE,

        NOT,
        AND,
        OR,
        XOR,

        EQUALS,
        NOT_EQUALS,
        GREATER_EQUALS,
        LESS_EQUALS,
        GREATER,
        LESS,

        NEW_TABLE, // Creates a new table
        NEW_LIST,

        CALL,
        CLOSE_CLOSURE,
        CLOSE_FUNCTION,

        EXIT// EXIT ;)
    }

    public class Chunk
    {
        private List<Instruction> program;
        private List<Unit> data;
        private Dictionary<string, Operand> globalVariablesAddresses;
        private ChunkPosition chunkPosition;
        public Library Prelude { get; private set; }
        public Operand ExitAddress { get { return (Operand)program.Count; } }
        public List<Instruction> Body { get { return program; } }
        public Operand DataCount { get { return (Operand)data.Count; } }
        public List<Unit> GetData { get { return data; } }
        public ChunkPosition ChunkPosition { get { return chunkPosition; } }
        public string ModuleName { get; private set; }
        public FunctionUnit MainFunctionUnit(string p_name)
        {
            FunctionUnit this_function_unit = new FunctionUnit(p_name, ModuleName);
            this_function_unit.Set(0, Body, ChunkPosition, 0);
            return this_function_unit;
        }

        public Chunk(string p_moduleName, Library p_prelude)
        {
            program = new List<Instruction>();
            data = new List<Unit>();
            globalVariablesAddresses = new Dictionary<string, Operand>();
            chunkPosition = new ChunkPosition(new List<PositionData>());
            Prelude = p_prelude;
            ModuleName = p_moduleName;
        }

        public void PrintToFile(string p_path, bool p_append = false)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(p_path, p_append))
            {
                Console.SetOut(file);
                Print();
                var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }
        }

        public void Print()
        {

            int data_literal_count = 0;
            Console.WriteLine("------------------- DATA LITERALS:");
            foreach (Unit v in data)
            {
                if (v.Type == UnitType.String)
                    Console.WriteLine("Data: " + data_literal_count.ToString() + " \"" + v.ToString() + "\"");
                else
                    Console.WriteLine("Data: " + data_literal_count.ToString() + " " + v.ToString());
                data_literal_count++;
            }

            Console.WriteLine(Environment.NewLine + "------------------- CHUNK:");
            for (int i = 0; i < program.Count; i++)
            {
                Console.Write(i + ": ");
                PrintInstruction(program[i]);
                Console.Write(" on position: " + " " + chunkPosition.GetPosition(i) + '\n');
            }
            Console.WriteLine();
        }

        public void PrintLastInstruction()
        {
            PrintInstruction(program[^1]);
        }

        static public void PrintInstruction(Instruction p_instruction)
        {
            Console.Write(p_instruction.ToString());
        }

        public ushort AddData(Unit p_value)
        {
            data.Add(p_value);
            return (ushort)(data.Count - 1);
        }

        public void AddGlobalVariableAddress(string p_name, Operand p_address)
        {
            globalVariablesAddresses.Add(p_name, p_address);
        }

        public void WriteInstruction(OpCode p_opCode, Operand p_opA, Operand p_opB, Operand p_opC, PositionData p_positionData)
        {
            Instruction this_instruction = new Instruction(p_opCode, p_opA, p_opB, p_opC);
            program.Add(this_instruction);
            chunkPosition.AddPosition(p_positionData);
        }

        public void WriteInstruction(Instruction p_instruction)
        {
            program.Add(p_instruction);
        }

        public Instruction ReadInstruction(Operand p_address)
        {
            return program[p_address];
        }

        public Unit GetDataLiteral(Operand p_address)
        {
            return data[p_address];
        }

        public Nullable<Operand> GetGlobalVariableAddress(string p_name)
        {
            Operand maybe_address;
            if (globalVariablesAddresses.TryGetValue(p_name, out maybe_address))
                return maybe_address;
            else
                return null;
        }

        public Unit GetFunction(string p_name)
        {
            foreach (Unit v in data)
            {
                if (v.Type == UnitType.Function)
                    if (((FunctionUnit)(v.heapUnitValue)).Name == p_name)
                    {
                        return new Unit(v.heapUnitValue);
                    }
            }
            foreach (IntrinsicUnit v in Prelude.intrinsics)
            {
                if (v.Name == p_name)
                {
                    return new Unit(v);
                }

            }
            Logger.LogLine(("Could not find Function: " + p_name), Defaults.Config.VMLogFile);
            throw Exceptions.not_found;
        }

        public Unit GetUnitFromTable(string p_table, string p_name)
        {
            foreach (KeyValuePair<string, TableUnit> item in Prelude.tables)
            {
                if (item.Key == p_table)
                {
                    return item.Value.Get(new Unit(p_name));
                }

            }
            Logger.LogLine("Could not find Unit: " + p_name + " in table: " + p_table, Defaults.Config.VMLogFile);
            throw Exceptions.not_found;
        }

        public List<Instruction> Slice(int p_start, int p_end)
        {
            int range = p_end - p_start;
            List<Instruction> programSlice = program.GetRange(p_start, range);
            program.RemoveRange(p_start, range);

            return programSlice;
        }

        public void FixInstruction(int p_address, OpCode? p_opCode, Operand? p_opA, Operand? p_opB, Operand? p_opC)
        {
            Instruction old_instruction = program[p_address];
            program[p_address] = new Instruction(
                p_opCode ??= old_instruction.opCode,
                p_opA ??= old_instruction.opA,
                p_opB ??= old_instruction.opB,
                p_opC ??= old_instruction.opC);
        }

        public void SwapDataLiteral(int p_address, Unit p_new_value)
        {
            data[p_address] = p_new_value;
        }

        ///////////////////////////////////////////////////////////////////////////////
        // .ltnc binary serialization
        ///////////////////////////////////////////////////////////////////////////////

        private static readonly byte[] LtncMagic = { (byte)'L', (byte)'T', (byte)'N', (byte)'C' };
        private const ushort LtncVersion = 1;
#if DOUBLE
        private const ushort LtncFlags = 1; // float64 / int64
#else
        private const ushort LtncFlags = 0; // float32 / int32
#endif

        // Each instruction is serialised as 4 u16 values followed by a position (2 × i32).
        static void WriteInstr(BinaryWriter p_w, Instruction p_i, PositionData p_pos)
        {
            p_w.Write((ushort)p_i.opCode);
            p_w.Write((ushort)p_i.opA);
            p_w.Write((ushort)p_i.opB);
            p_w.Write((ushort)p_i.opC);
            p_pos.Write(p_w);
        }

        static (Instruction instr, PositionData pos) ReadInstr(BinaryReader p_r)
        {
            OpCode opCode = (OpCode)p_r.ReadUInt16();
            Operand opA = p_r.ReadUInt16();
            Operand opB = p_r.ReadUInt16();
            Operand opC = p_r.ReadUInt16();
            PositionData pos = PositionData.Read(p_r);
            return (new Instruction(opCode, opA, opB, opC), pos);
        }

        static void WriteStr(BinaryWriter p_w, string p_s)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(p_s);
            p_w.Write((ushort)bytes.Length);
            p_w.Write(bytes);
        }

        static string ReadStr(BinaryReader p_r)
        {
            ushort len = p_r.ReadUInt16();
            byte[] bytes = p_r.ReadBytes(len);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        static void WriteUnit(BinaryWriter p_w, Unit p_unit)
        {
            switch (p_unit.Type)
            {
                case UnitType.Float:
                    p_w.Write((byte)0);
#if DOUBLE
                    p_w.Write((double)p_unit.floatValue);
#else
                    p_w.Write((float)p_unit.floatValue);
#endif
                    break;
                case UnitType.Integer:
                    p_w.Write((byte)1);
#if DOUBLE
                    p_w.Write((long)p_unit.integerValue);
#else
                    p_w.Write((int)p_unit.integerValue);
#endif
                    break;
                case UnitType.Boolean:
                    p_w.Write((byte)2);
                    p_w.Write(p_unit.boolValue);
                    break;
                case UnitType.Char:
                    p_w.Write((byte)3);
                    p_w.Write(p_unit.charValue);
                    break;
                case UnitType.String:
                    p_w.Write((byte)4);
                    WriteStr(p_w, ((StringUnit)p_unit.heapUnitValue).content);
                    break;
                case UnitType.Function:
                    p_w.Write((byte)5);
                    FunctionUnit fn = (FunctionUnit)p_unit.heapUnitValue;
                    WriteStr(p_w, fn.Name);
                    WriteStr(p_w, fn.Module);
                    p_w.Write((ushort)fn.Arity);
                    p_w.Write((ushort)fn.OriginalPosition);
                    p_w.Write((ushort)fn.Body.Count);
                    for (int i = 0; i < fn.Body.Count; i++)
                        WriteInstr(p_w, fn.Body[i], fn.ChunkPosition.GetPosition(i));
                    break;
                case UnitType.Void:
                    p_w.Write((byte)6);
                    break;
                case UnitType.Closure:
                {
                    p_w.Write((byte)7);
                    ClosureUnit cl = (ClosureUnit)p_unit.heapUnitValue;
                    WriteStr(p_w, cl.Function.Name);
                    WriteStr(p_w, cl.Function.Module);
                    p_w.Write((ushort)cl.Function.Arity);
                    p_w.Write((ushort)cl.Function.OriginalPosition);
                    p_w.Write((ushort)cl.Function.Body.Count);
                    for (int i = 0; i < cl.Function.Body.Count; i++)
                        WriteInstr(p_w, cl.Function.Body[i], cl.Function.ChunkPosition.GetPosition(i));
                    p_w.Write((ushort)cl.UpValues.Count);
                    foreach (UpValueUnit uv in cl.UpValues)
                    {
                        p_w.Write((byte)(uv.IsChained ? 1 : 0));
                        if (uv.IsChained)
                            p_w.Write((ushort)uv.ChainedIndex);
                        else
                        {
                            p_w.Write((ushort)uv.Address);
                            p_w.Write((ushort)uv.Env);
                        }
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException("Cannot serialize Unit of type " + p_unit.Type);
            }
        }

        static Unit ReadUnit(BinaryReader p_r)
        {
            byte tag = p_r.ReadByte();
            switch (tag)
            {
                case 0:
#if DOUBLE
                    return new Unit((Float)p_r.ReadDouble());
#else
                    return new Unit((Float)p_r.ReadSingle());
#endif
                case 1:
#if DOUBLE
                    return new Unit((Integer)p_r.ReadInt64());
#else
                    return new Unit((Integer)p_r.ReadInt32());
#endif
                case 2:
                    return new Unit(p_r.ReadBoolean());
                case 3:
                    return new Unit(p_r.ReadChar());
                case 4:
                    return new Unit(ReadStr(p_r));
                case 5:
                {
                    string name = ReadStr(p_r);
                    string module = ReadStr(p_r);
                    ushort arity = p_r.ReadUInt16();
                    ushort origPos = p_r.ReadUInt16();
                    ushort instrCount = p_r.ReadUInt16();
                    List<Instruction> body = new List<Instruction>(instrCount);
                    List<PositionData> posList = new List<PositionData>(instrCount);
                    for (int i = 0; i < instrCount; i++)
                    {
                        var (instr, pos) = ReadInstr(p_r);
                        body.Add(instr);
                        posList.Add(pos);
                    }
                    FunctionUnit fn = new FunctionUnit(name, module);
                    fn.Set(arity, body, new ChunkPosition(posList), origPos);
                    return new Unit(fn);
                }
                case 6:
                    return new Unit(UnitType.Void);
                case 7: // Closure
                {
                    string name = ReadStr(p_r);
                    string module = ReadStr(p_r);
                    ushort arity = p_r.ReadUInt16();
                    ushort origPos = p_r.ReadUInt16();
                    ushort instrCount = p_r.ReadUInt16();
                    List<Instruction> body = new List<Instruction>(instrCount);
                    List<PositionData> posList = new List<PositionData>(instrCount);
                    for (int i = 0; i < instrCount; i++)
                    {
                        var (instr, pos) = ReadInstr(p_r);
                        body.Add(instr);
                        posList.Add(pos);
                    }
                    FunctionUnit fn = new FunctionUnit(name, module);
                    fn.Set(arity, body, new ChunkPosition(posList), origPos);
                    ushort uvCount = p_r.ReadUInt16();
                    List<UpValueUnit> upValues = new List<UpValueUnit>(uvCount);
                    for (int i = 0; i < uvCount; i++)
                    {
                        bool isChained = p_r.ReadByte() != 0;
                        if (isChained)
                            upValues.Add(new UpValueUnit(p_r.ReadUInt16(), true));
                        else
                        {
                            ushort addr = p_r.ReadUInt16();
                            ushort env = p_r.ReadUInt16();
                            upValues.Add(new UpValueUnit(addr, env));
                        }
                    }
                    return new Unit(new ClosureUnit(fn, upValues));
                }
                default:
                    throw new InvalidOperationException("Unknown Unit tag in .ltnc: " + tag);
            }
        }

        public void Save(string p_path)
        {
            using BinaryWriter w = new BinaryWriter(File.Open(p_path, FileMode.Create));

            // Header
            w.Write(LtncMagic);
            w.Write(LtncVersion);
            w.Write(LtncFlags);

            // Module name
            WriteStr(w, ModuleName);

            // Data literals (constants + nested functions)
            w.Write((ushort)data.Count);
            foreach (Unit u in data)
                WriteUnit(w, u);

            // Global variable addresses
            w.Write((ushort)globalVariablesAddresses.Count);
            foreach (KeyValuePair<string, Operand> kv in globalVariablesAddresses)
            {
                WriteStr(w, kv.Key);
                w.Write((ushort)kv.Value);
            }

            // Main program body
            w.Write((ushort)program.Count);
            for (int i = 0; i < program.Count; i++)
                WriteInstr(w, program[i], chunkPosition.GetPosition(i));
        }

        public static Chunk Load(string p_path, Library p_prelude)
        {
            using BinaryReader r = new BinaryReader(File.OpenRead(p_path));

            // Validate header
            byte[] magic = r.ReadBytes(4);
            if (magic[0] != 'L' || magic[1] != 'T' || magic[2] != 'N' || magic[3] != 'C')
                throw new InvalidOperationException("Not a .ltnc file: " + p_path);
            ushort version = r.ReadUInt16();
            if (version != LtncVersion)
                throw new InvalidOperationException(".ltnc version mismatch: " + version);
            ushort flags = r.ReadUInt16();
            if (flags != LtncFlags)
                throw new InvalidOperationException(".ltnc flags mismatch: file=" + flags + " runtime=" + LtncFlags);

            string moduleName = ReadStr(r);
            Chunk chunk = new Chunk(moduleName, p_prelude);

            // Data literals
            ushort dataCount = r.ReadUInt16();
            for (int i = 0; i < dataCount; i++)
                chunk.AddData(ReadUnit(r));

            // Global variable addresses
            ushort globalCount = r.ReadUInt16();
            for (int i = 0; i < globalCount; i++)
            {
                string name = ReadStr(r);
                Operand address = r.ReadUInt16();
                chunk.AddGlobalVariableAddress(name, address);
            }

            // Main program body
            ushort instrCount = r.ReadUInt16();
            for (int i = 0; i < instrCount; i++)
            {
                var (instr, pos) = ReadInstr(r);
                chunk.WriteInstruction(instr.opCode, instr.opA, instr.opB, instr.opC, pos);
            }

            return chunk;
        }
    }
}
