using System;
using System.Collections.Generic;

using Operand = System.UInt16;

namespace lightning
{
    public struct LineCounter{
        public List<uint> lines;

        public LineCounter(List<uint> p_lines){
            lines = p_lines;
        }
        public void AddLine(uint line)
        {
            lines.Add(line);
        }
        public uint GetLine(int instruction_address)
        {
            return lines[instruction_address];
        }
        public LineCounter Slice(int start, int end)
        {
            int range = end - start;
            List<uint> linesSlice = lines.GetRange(start, range);
            lines.RemoveRange(start, range);

            return new LineCounter(linesSlice);
        }
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
                case OpCode.INCREMENT:
                case OpCode.DECREMENT:
                case OpCode.AND:
                case OpCode.OR:
                case OpCode.XOR:
                case OpCode.NAND:
                case OpCode.NOR:
                case OpCode.XNOR:
                case OpCode.EQUALS:
                case OpCode.NOT_EQUALS:
                case OpCode.GREATER_EQUALS:
                case OpCode.LESS_EQUALS:
                case OpCode.GREATER:
                case OpCode.LESS:
                case OpCode.EXIT:
                case OpCode.LOAD_NIL:
                case OpCode.POP:
                case OpCode.PUSH_STASH:
                case OpCode.POP_STASH:
                case OpCode.CALL:
                case OpCode.CLOSE_CLOSURE:
                case OpCode.CLOSE_FUNCTION:
                case OpCode.LOAD_FALSE:
                case OpCode.LOAD_TRUE:
                    return op.ToString();

                // 1 op
                case OpCode.LOAD_CONSTANT:
                case OpCode.LOAD_GLOBAL:
                case OpCode.LOAD_UPVALUE:
                case OpCode.LOAD_INTRINSIC:
                case OpCode.JUMP:
                case OpCode.JUMP_IF_NOT_TRUE:
                case OpCode.JUMP_BACK:
                case OpCode.RETURN_SET:
                case OpCode.TABLE_GET:
                    return op.ToString() + " " + this.opA;
                // 2 op
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.ASSIGN_UPVALUE:
                case OpCode.LOAD_VARIABLE:
                case OpCode.LOAD_IMPORTED_GLOBAL:
                case OpCode.LOAD_IMPORTED_CONSTANT:
                case OpCode.NEW_TABLE:
                case OpCode.TABLE_SET:
                    return op.ToString() + " " + this.opA + " " + this.opB;
                // 3 op
                case OpCode.ASSIGN_VARIABLE:
                case OpCode.DECLARE_FUNCTION:
                    return op.ToString() + " " + this.opA + " " + this.opB + " " + this.opC;
                default:
                    return "Unkown Opcode: " + op.ToString();
            }
        }
    }
    public enum OpCode: Operand
    {
        LOAD_CONSTANT,// Loads a constant to stack
        LOAD_VARIABLE,// Loads o variable to stack
        LOAD_GLOBAL,
        LOAD_IMPORTED_GLOBAL,
        LOAD_IMPORTED_CONSTANT,
        LOAD_UPVALUE,
        LOAD_INTRINSIC,
        LOAD_NIL,
        LOAD_TRUE,
        LOAD_FALSE,

        DECLARE_GLOBAL,
        DECLARE_VARIABLE,
        DECLARE_FUNCTION,
        ASSIGN_VARIABLE,
        ASSIGN_GLOBAL,
        ASSIGN_UPVALUE,
        TABLE_GET,
        TABLE_SET,

        POP,
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
        INCREMENT,
        DECREMENT,

        NOT,
        AND,
        OR,
        XOR,
        NAND,
        NOR,
        XNOR,

        EQUALS,
        NOT_EQUALS,
        GREATER_EQUALS,
        LESS_EQUALS,
        GREATER,
        LESS,

        NEW_TABLE, // Creates a new table

        CALL,
        CLOSE_CLOSURE,
        CLOSE_FUNCTION,

        EXIT// EXIT ;)
    }

    public class Chunk
    {
        List<Instruction> program;
        List<Unit> constants;
        public LineCounter lineCounter;
        public Library Prelude { get; private set; }

        public Operand ProgramSize
        {
            get
            {
                return (Operand)program.Count;
            }
        }
        public List<Instruction> Program { get { return program; } }
        public Operand ConstantsCount()
        {
            return (Operand)constants.Count;
        }

        public Chunk(Library p_prelude)
        {
            program = new List<Instruction>();
            lineCounter = new LineCounter(new List<uint>());
            constants = new List<Unit>();
            Prelude = p_prelude;
        }

        public void Print()
        {

            int constant_counter = 0;
            foreach (Unit v in constants)
            {
                if(v.Type == UnitType.String)
                    Console.WriteLine("Constant: " + constant_counter.ToString() + " \"" + v.ToString() + "\"");
                else
                    Console.WriteLine("Constant: "+ constant_counter.ToString() + " " + v.ToString());
                constant_counter++;
            }

            for(int i = 0; i< program.Count; i++)
            {
                Console.Write(i + ": ");
                PrintInstruction(program[i]);
                Console.Write(" on line: " + " " + lineCounter.GetLine(i) + '\n');
            }
            Console.WriteLine();
        }

        public void PrintLastInstruction()
        {
            PrintInstruction(program[^1]);
        }

        static public void PrintInstruction(Instruction instruction)
        {
            Console.Write(instruction.ToString());
        }

        public ushort AddConstant(Unit value)
        {
            constants.Add(value);
            return (ushort)(constants.Count -1);
        }

        public void WriteInstruction(OpCode opCode, Operand opA, Operand opB, Operand opC, uint line)
        {
            Instruction this_instruction = new Instruction(opCode, opA, opB, opC);
            program.Add(this_instruction);
            lineCounter.AddLine(line);
        }

        public void WriteInstruction(Instruction instruction)
        {
            program.Add(instruction);
        }

        public Instruction ReadInstruction(Operand address)
        {
           return program[address];
        }

        public Unit GetConstant(Operand address)
        {
            return constants[address];
        }

        public List<Unit> GetConstants()
        {
            return constants;
        }

        public HeapUnit GetFunction(string name)
        {
            foreach(Unit v in constants)
            {
                if(v.Type == UnitType.Function)
                    if( ((FunctionUnit)(v.heapUnitValue)).name == name)
                    {
                        return (FunctionUnit)(v.heapUnitValue);
                    }
            }
            foreach (IntrinsicUnit v in Prelude.intrinsics)
            {
                if (v.name == name)
                {
                    return v;
                }

            }
            return null;
        }

        public List<Instruction> Slice(int start, int end)
        {
            int range = end - start;
            List<Instruction> programSlice = program.GetRange(start, range);
            program.RemoveRange(start, range);

            return programSlice;
        }

        public void FixInstruction(int address, OpCode? opCode, Operand? opA, Operand? opB, Operand? opC)
        {
            Instruction old_instruction = program[address];
            program[address] = new Instruction(
                opCode ??= old_instruction.opCode,
                opA ??= old_instruction.opA,
                opB ??= old_instruction.opB,
                opC ??= old_instruction.opC);
        }

        public void SwapConstant(int address, Unit new_value)
        {
            constants[address] = new_value;
        }
    }
}
