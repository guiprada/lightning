using System;
using System.Collections.Generic;
using System.Text;

using Operand = System.UInt16;
namespace lightning
{   
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
    }
    public enum OpCode: Operand
    {
        LOADC,// Loads a constant to stack
        LOADV,// Loads o variable to stack
        LOADG,
        LOADI,
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

        PUSH,
        POP,
        STASHTOP,
        POPSTASH,

        JMP,    // Jumps n instructions 
        JNT,    // Jumps if not true
        JMPB,     // Jumps back n instructions

        NENV,   // Open new env
        CENV,   // Close env
        RET,
        RETSREL,

        ADD, // Float
        APP, // String
        SUB, // 
        MUL,
        DIV,
        NEG,

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
        PFOR,
        FOREACH,

        EXIT// EXIT ;)
    }

    public class Chunk
    {        
        //class LineInfo
        //{
        //    public uint line;
        //    public uint nMembers;
        //    public LineInfo(uint p_line, uint p_nMembers)
        //    {
        //        line = p_line;
        //        nMembers = p_nMembers;
        //    }
        //}

        List<Instruction> program;
        List<Value> constants;
        Dictionary<uint, uint> lines;
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
            lines = new Dictionary<uint, uint>();
            constants = new List<Value>();
            Prelude = p_prelude;        
        }

        public void Print()
        {

            int constant_counter = 0;
            foreach (Value v in constants)
            {
                if(v.GetType() == typeof(ValString))
                    Console.WriteLine("Constant: " + constant_counter.ToString() + " \"" + v.ToString() + "\"");
                else
                    Console.WriteLine("Constant: "+ constant_counter.ToString() + " " + v.ToString());
                constant_counter++;
            }

            for(int i = 0; i< program.Count; i++)
            {
                Console.Write(i + ": ");
                PrintInstruction(program[i]);
                Console.Write(" on line: " + " " + GetLine(i) + '\n');
            }
            Console.WriteLine();
        }

        public void PrintLastInstruction()
        {
            PrintInstruction(program[^1]);
        }

        static public void PrintInstruction(Instruction instruction)
        {
            Console.Write(ToString(instruction));
        }

        static public string ToString(Instruction instruction)
        {
            OpCode op = instruction.opCode;
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
                case OpCode.STASHTOP:
                case OpCode.POPSTASH:
                case OpCode.CALL:
                case OpCode.CLOSURECLOSE:
                case OpCode.FUNCLOSE:
                case OpCode.LOADFALSE:
                case OpCode.LOADTRUE:
                case OpCode.PFOR:
                case OpCode.FOREACH:
                    return op.ToString();

                // 1 op
                case OpCode.LOADC:
                case OpCode.LOADG: 
                case OpCode.LOADUPVAL:
                case OpCode.PUSH:
                case OpCode.LOADINTR:
                case OpCode.JMP:
                case OpCode.JNT:
                case OpCode.JMPB:
                case OpCode.RETSREL:
                case OpCode.TABLEGET:
                    return op.ToString() + " " + instruction.opA;
                // 2 op                
                case OpCode.ASSIGNG:
                case OpCode.ASSIGNUPVAL:
                case OpCode.LOADV:
                case OpCode.LOADI:               
                case OpCode.NTABLE:
                case OpCode.FUNDCL:
                case OpCode.TABLESET:
                    return op.ToString() + " " + instruction.opA + " " + instruction.opB;
                // 3 op
                case OpCode.ASSIGN:
                    return op.ToString() + " " + instruction.opA + " " + instruction.opB + " " + instruction.opC;
                default:
                    return "Unkown Opcode: " + op.ToString();
            }
        }
 
        public ushort AddConstant(Value value)
        {
            constants.Add(value);
            return (ushort)(constants.Count -1);
        }

        public void WriteInstruction(OpCode opCode, Operand opA, Operand opB, Operand opC, uint line)
        {
            Instruction this_instruction = new Instruction(opCode, opA, opB, opC);
            program.Add(this_instruction);
            AddLine(line);
        }

        public void WriteInstruction(Instruction instruction)
        {            
            program.Add(instruction);            
        }

        void AddLine(uint line)
        {
            if (lines.ContainsKey(line))
            {
                lines[line]++;
            }
            else
            {
                lines.Add(line, 1);
            }
        }
        public uint GetLine(int instruction_address)
        {
            uint sum = 0;
            uint counter = 1;
            while(sum < instruction_address)
            {
                if (lines.ContainsKey(counter)) {
                    sum += lines[counter];
                }
                counter++;
            }
            return counter;
        }

        public Instruction ReadInstruction(Operand address)
        {
           return program[address];           
        }

        public Value GetConstant(Operand address)
        {
            return constants[address];
        }

        public List<Value> GetConstants()
        {
            return constants;
        }

        public Value GetFunction(string name)
        {
            foreach(Value v in constants)
            {
                if(v.GetType() == typeof(ValFunction))
                    if((v as ValFunction).name == name)
                    {
                        return v as ValFunction;
                    }
            }
            foreach (ValIntrinsic v in Prelude.intrinsics)
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
            List<Instruction> slice = program.GetRange(start, end - start);
            program.RemoveRange(start, end - start);
            return slice;
        }

        public void FixInstruction(int address, OpCode? opCode, Operand? opA, Operand? opB, Operand? opC)
        {
            //Console.WriteLine("Fixed instruction: " + address);
            //PrintInstruction(program[address]);
            Instruction old_instruction = program[address];
            program[address] = new Instruction(
                opCode ??= old_instruction.opCode,
                opA ??= old_instruction.opA,
                opB ??= old_instruction.opB,
                opC ??= old_instruction.opC);
        }

        public void SwapConstant(int address, Value new_value)
        {
            constants[address] = new_value;
        }        
    }
}
