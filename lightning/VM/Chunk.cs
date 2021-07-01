using System;
using System.Collections.Generic;
using System.Text;

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
                case OpCode.VARDCL:
                case OpCode.GLOBALDCL:
                case OpCode.NENV:
                case OpCode.CENV:
                case OpCode.RET:
                case OpCode.ADD:
                case OpCode.APP:
                case OpCode.SUB:
                case OpCode.MUL:
                case OpCode.DIV:
                case OpCode.NEG:
                case OpCode.NOT:
                case OpCode.INC:
                case OpCode.DEC:
                case OpCode.AND:
                case OpCode.OR:
                case OpCode.XOR:
                case OpCode.NAND:
                case OpCode.NOR:
                case OpCode.XNOR:
                case OpCode.EQ:
                case OpCode.NEQ:
                case OpCode.GTQ:
                case OpCode.LTQ:
                case OpCode.GT:
                case OpCode.LT:
                case OpCode.EXIT:
                case OpCode.LOADNIL:
                case OpCode.POP:
                case OpCode.PUSHSTASH:
                case OpCode.POPSTASH:
                case OpCode.CALL:
                case OpCode.CLOSURECLOSE:
                case OpCode.FUNCLOSE:
                case OpCode.LOADFALSE:
                case OpCode.LOADTRUE:
                    return op.ToString();

                // 1 op
                case OpCode.LOADC:
                case OpCode.LOADG:
                case OpCode.LOADUPVAL:
                case OpCode.LOADINTR:
                case OpCode.JMP:
                case OpCode.JNT:
                case OpCode.JMPB:
                case OpCode.SETRET:
                case OpCode.TABLEGET:
                    return op.ToString() + " " + this.opA;
                // 2 op
                case OpCode.ASSIGNG:
                case OpCode.ASSIGNUPVAL:
                case OpCode.LOADV:
                case OpCode.LOADGI:
                case OpCode.LOADCI:
                case OpCode.NTABLE:
                case OpCode.TABLESET:
                    return op.ToString() + " " + this.opA + " " + this.opB;
                // 3 op
                case OpCode.ASSIGN:
                case OpCode.FUNDCL:
                    return op.ToString() + " " + this.opA + " " + this.opB + " " + this.opC;
                default:
                    return "Unkown Opcode: " + op.ToString();
            }
        }
    }
    public enum OpCode: Operand
    {
        LOADC,// Loads a constant to stack
        LOADV,// Loads o variable to stack
        LOADG,
        LOADGI,
        LOADCI,
        LOADUPVAL,
        LOADINTR,
        LOADNIL,
        LOADTRUE,
        LOADFALSE,

        GLOBALDCL,
        VARDCL,
        FUNDCL,
        ASSIGN,
        ASSIGNG,
        ASSIGNUPVAL,
        TABLEGET,
        TABLESET,

        POP,
        PUSHSTASH,
        POPSTASH,

        JMP,    // Jumps n instructions
        JNT,    // Jumps if not true
        JMPB,     // Jumps back n instructions

        NENV,   // Open new env
        CENV,   // Close env
        RET,
        SETRET,

        ADD, // Float
        APP, // String
        SUB, //
        MUL,
        DIV,
        NEG,
        INC,
        DEC,

        NOT,
        AND,
        OR,
        XOR,
        NAND,
        NOR,
        XNOR,

        EQ,
        NEQ,
        GTQ,
        LTQ,
        GT,
        LT,

        NTABLE, // Creates a new table

        CALL,
        CLOSURECLOSE,
        FUNCLOSE,

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
                if(v.HeapUnitType() == typeof(StringUnit))
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
                if(v.HeapUnitType() == typeof(FunctionUnit))
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
