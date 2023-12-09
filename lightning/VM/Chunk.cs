using System;
using System.Collections.Generic;
using System.IO;

using Operand = System.UInt16;

namespace lightning
{
	public struct ChunkPosition{
		public List<PositionData> positions;

		public ChunkPosition(List<PositionData> p_positions){
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
				case OpCode.DUP:
				case OpCode.PUSH_STASH:
				case OpCode.POP_STASH:
				case OpCode.CALL:
				case OpCode.CLOSE_CLOSURE:
				case OpCode.CLOSE_FUNCTION:
				case OpCode.LOAD_FALSE:
				case OpCode.LOAD_TRUE:
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
	public enum OpCode: Operand
	{
		LOAD_DATA,
		LOAD_VARIABLE,
		LOAD_GLOBAL,
		LOAD_IMPORTED_GLOBAL,
		LOAD_IMPORTED_DATA,
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
		public List<Unit> GetData { get{ return data; } }
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

		public void PrintToFile(string p_path, bool p_append = false){
			using (System.IO.StreamWriter file = new System.IO.StreamWriter(p_path, p_append)){
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
				if(v.Type == UnitType.String)
					Console.WriteLine("Data: " + data_literal_count.ToString() + " \"" + v.ToString() + "\"");
				else
					Console.WriteLine("Data: "+ data_literal_count.ToString() + " " + v.ToString());
				data_literal_count++;
			}

			Console.WriteLine(Environment.NewLine + "------------------- CHUNK:");
			for(int i = 0; i< program.Count; i++)
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
			return (ushort)(data.Count -1);
		}

		public void AddGlobalVariableAddress(string p_name, Operand p_address){
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

		public Nullable<Operand> GetGlobalVariableAddress(string p_name){
			Operand maybe_address;
			if(globalVariablesAddresses.TryGetValue(p_name, out maybe_address))
				return maybe_address;
			else
				return null;
		}

		public Unit GetFunction(string p_name)
		{
			foreach(Unit v in data)
			{
				if(v.Type == UnitType.Function)
					if( ((FunctionUnit)(v.heapUnitValue)).Name == p_name)
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
			return new Unit(UnitType.Null);
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
			return new Unit(UnitType.Null);
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
	}
}
