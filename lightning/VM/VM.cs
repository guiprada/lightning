using System;
using System.Collections.Generic;
using System.Linq;


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
	public enum VMResultType
	{
		OK,
		ERROR
	}

	public struct VMResult
	{
		public VMResultType status;
		public Unit value;

		public VMResult(VMResultType p_status, Unit p_value)
		{
			status = p_status;
			value = p_value;
		}
	}

	public class VM
	{
		Operand IP;
		FunctionUnit main;
		private bool parallelVM;

		InstructionStack instructions;
		List<Instruction> instructionsCache;

		Memory<Unit> globals; // used for global variables
		Memory<Unit> variables; // used for scoped variables
		List<Unit> data;

		public Stack stack;

		Memory<UpValueUnit> upValues;
		UpValueEnv registeredUpValues;

		public List<IntrinsicUnit> Intrinsics { get; private set; }
		public Library Prelude { get; private set; }
		public Dictionary<string, int> LoadedModules { get; private set; }
		public List<ModuleUnit> modules;
		public List<Unit> Data { get { return data; } }
		int Env { get { return variables.Env; } }

		static Stack<VM> vmPool;
		public const Operand ASSIGN = (Operand)AssignmentOperatorType.ASSIGN;
		public const Operand ADDITION_ASSIGN = (Operand)AssignmentOperatorType.ADDITION_ASSIGN;
		public const Operand SUBTRACTION_ASSIGN = (Operand)AssignmentOperatorType.SUBTRACTION_ASSIGN;
		public const Operand MULTIPLICATION_ASSIGN = (Operand)AssignmentOperatorType.MULTIPLICATION_ASSIGN;
		public const Operand DIVISION_ASSIGN = (Operand)AssignmentOperatorType.DIVISION_ASSIGN;

		//////////////////////////////////////////////////// Public
		// static VM()
		// {
		// }

		public VM(Chunk p_chunk)
		{
			IP = 0;
			parallelVM = false;

			main = p_chunk.MainFunctionUnit("Main");

			instructions = new InstructionStack(Defaults.Config.CallStackSize, main, out instructionsCache);

			stack = new Stack(Defaults.Config.CallStackSize);

			variables = new Memory<Unit>();
			upValues = new Memory<UpValueUnit>();
			registeredUpValues = new UpValueEnv(variables);

			data = p_chunk.GetData;
			Prelude = p_chunk.Prelude;
			globals = new Memory<Unit>();
			Intrinsics = p_chunk.Prelude.intrinsics;
			foreach (IntrinsicUnit v in Intrinsics)
			{
				// lock(globals) // not needed because this vm initialization does not run in parallel
				globals.Add(new Unit(v));
			}
			foreach (KeyValuePair<string, TableUnit> entry in p_chunk.Prelude.tables)
			{
				// lock(globals) // not needed because this vm initialization does not run in parallel
				globals.Add(new Unit(entry.Value));
			}
			LoadedModules = new Dictionary<string, int>();
			modules = new List<ModuleUnit>();

			vmPool = new Stack<VM>();
		}

		private VM(
			FunctionUnit p_main,
			List<Unit> p_data,
			Memory<Unit> p_globals,
			Library p_Prelude,
			Dictionary<string, int> p_LoadedModules,
			List<ModuleUnit> p_modules,
			Stack<VM> p_vmPool,
			bool p_parallelVM)
		{
			main = p_main;
			data = p_data;
			globals = p_globals;
			Prelude = p_Prelude;
			LoadedModules = p_LoadedModules;
			modules = p_modules;
			vmPool = p_vmPool;

			IP = 0;
			parallelVM = p_parallelVM;

			instructions = new InstructionStack(Defaults.Config.CallStackSize, main, out instructionsCache);
			stack = new Stack(Defaults.Config.CallStackSize);
			variables = new Memory<Unit>();
			upValues = new Memory<UpValueUnit>();
			registeredUpValues = new UpValueEnv(variables);
		}

		public void ResoursesTrim()
		{
			variables.Trim();
			upValues.Trim();
			registeredUpValues.Trim();
		}
		public void ReleaseVMs(int p_count)
		{
			for (int i = 0; i < p_count; i++)
				if (vmPool.Count > 0)
					vmPool.Pop();
			vmPool.TrimExcess();
		}
		public void ReleaseVMs()
		{
			vmPool.Clear();
			vmPool.TrimExcess();
		}

		public int CountVMs()
		{
			return vmPool.Count;
		}

		void Reset()
		{
			IP = 0;
			parallelVM = true;
			instructions.Reset();
			stack.Clear();
			variables.Clear();
			upValues.Clear();
			registeredUpValues.Clear();
		}
		public void RecycleVM(VM p_vm)
		{
			p_vm.Reset();
			vmPool.Push(p_vm);
		}

		public VM GetVM()
		{
			return GetVM(parallelVM);
		}
		public VM GetParallelVM()
		{
			return GetVM(true);
		}
		private VM GetVM(bool p_parallelVM)
		{
			if (vmPool.Count > 0)
			{
				VM new_vm = vmPool.Pop();
				new_vm.SetParallel(p_parallelVM);
				return new_vm;
			}
			else
			{
				VM new_vm = new VM(main, data, globals, Prelude, LoadedModules, modules, vmPool, p_parallelVM);
				return new_vm;
			}
		}

		public void SetParallel(bool p_parallelVM)
		{
			parallelVM = p_parallelVM;
		}

		public Operand AddModule(ModuleUnit p_module)
		{
			LoadedModules.Add(p_module.Name, LoadedModules.Count);
			modules.Add(p_module);
			return (Operand)(LoadedModules.Count - 1);
		}

		public Unit GetGlobal(Operand p_address)
		{
			Unit value;
			lock (globals) // not needed because require does not run in parallel
				value = globals.Get(p_address);
			return value;
		}

		public Unit GetGlobal(Chunk p_chunk, string p_name)
		{
			Nullable<Operand> maybe_address = p_chunk.GetGlobalVariableAddress(p_name);
			if (maybe_address.HasValue)
				return GetGlobal(maybe_address.Value);
			else
				return new Unit(UnitType.Null);
		}

		//////////////////////////// Accessors
		public Unit GetUnit(int p_n)
		{
			return stack.Peek(p_n);
		}

		public string GetString(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.String)
				throw new Exception("Expected a String.");
			return ((StringUnit)stack.Peek(p_n).heapUnitValue).content;
		}

		public Float GetNumber(int p_n)
		{
			Unit this_value = stack.Peek(p_n);
			if (this_value.Type == UnitType.Integer)
				return this_value.integerValue;
			if (this_value.Type == UnitType.Float)
				return (this_value.floatValue);
			throw new Exception("Expected a Float or Integer.");
		}

		public Float GetFloat(int p_n)
		{
			Unit this_value = stack.Peek(p_n);
			if (this_value.Type == UnitType.Float)
				return (this_value.floatValue);
			throw new Exception("Expected a Float.");
		}

		public Integer GetInteger(int p_n)
		{
			Unit this_value = stack.Peek(p_n);
			if (this_value.Type == UnitType.Integer)
				return this_value.integerValue;
			throw new Exception("Expected a Integer.");
		}

		public TableUnit GetTable(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.Table)
				throw new Exception("Expected a Table.");
			return (TableUnit)stack.Peek(p_n).heapUnitValue;
		}

		public ListUnit GetList(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.List)
				throw new Exception("Expected a List.");
			return (ListUnit)stack.Peek(p_n).heapUnitValue;
		}

		public T GetWrappedContent<T>(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.Wrapper)
				throw new Exception("Expected a Wrapper.");
			return ((WrapperUnit<T>)stack.Peek(p_n).heapUnitValue).UnWrap();
		}

		public WrapperUnit<T> GetWrapperUnit<T>(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.Wrapper)
				throw new Exception("Expected a Wrapper.");
			return (WrapperUnit<T>)stack.Peek(p_n).heapUnitValue;
		}

		public StringUnit GetStringUnit(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.String)
				throw new Exception("Expected a String.");
			return (StringUnit)stack.Peek(p_n).heapUnitValue;
		}

		public char GetChar(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.Char)
				throw new Exception("Expected a Char.");
			return (stack.Peek(p_n).charValue);
		}

		public bool GetBool(int p_n)
		{
			if (stack.Peek(p_n).Type != UnitType.Boolean)
				throw new Exception("Expected a Boolean.");
			return stack.Peek(p_n).ToBool();
		}

		//////////////////////////// End Accessors
		public Unit ProtectedCallFunction(Unit p_callable, List<Unit> p_args = null)
		{
			try
			{
				return CallFunction(p_callable, p_args);
			}
			catch (Exception e)
			{
				Logger.LogLine("VM Busted ...\n" + e.ToString(), Defaults.Config.VMLogFile);
				return new Unit(UnitType.Null);
			}
		}
		public Unit CallFunction(Unit p_callable, List<Unit> p_args = null)
		{
			Operand before_call_IP = IP;

			UnitType this_type = p_callable.Type;

			if (this_type == UnitType.Function)
			{
				if (p_args != null)
					for (int i = p_args.Count - 1; i >= 0; i--)
						stack.Push(p_args[i]);

				instructions.PushRET((Operand)(main.Body.Count - 1));
				FunctionUnit this_func = (FunctionUnit)p_callable.heapUnitValue;
				instructions.PushFunction(this_func, Env, out instructionsCache);

				IP = 0;
			}
			else if (this_type == UnitType.Closure)
			{
				if (p_args != null)
					for (int i = p_args.Count - 1; i >= 0; i--)
						stack.Push(p_args[i]);

				instructions.PushRET((Operand)(main.Body.Count - 1));
				ClosureUnit this_closure = (ClosureUnit)p_callable.heapUnitValue;

				upValues.PushEnv();


				foreach (UpValueUnit u in this_closure.UpValues)
				{
					upValues.Add(u);
				}
				instructions.PushFunction(this_closure.Function, Env, out instructionsCache);

				IP = 0;
			}
			else if (this_type == UnitType.Intrinsic)
			{
				IntrinsicUnit this_intrinsic = (IntrinsicUnit)p_callable.heapUnitValue;
				if (p_args != null && p_args.Count == this_intrinsic.Arity)
					for (int i = this_intrinsic.Arity - 1; i >= 0; i--)
						stack.Push(p_args[i]);

				Unit intrinsic_result = this_intrinsic.Function(this);
				stack.top -= this_intrinsic.Arity;

				return intrinsic_result;
			}
			else if (this_type == UnitType.ExternalFunction)
			{
				ExternalFunctionUnit this_external_function = (ExternalFunctionUnit)p_callable.heapUnitValue;
				object[] arguments = [];

				if (p_args != null && p_args.Count == this_external_function.Arity)
					for (int i = this_external_function.Arity - 1; i >= 0; i--)
						arguments.Append(Unit.ToObject(p_args[i]));

				Unit this_external_result = Unit.FromObject(this_external_function.Function.Invoke(null, arguments));
				return this_external_result;
			}

			VMResult result = Run();
			IP = before_call_IP;
			if (result.status == VMResultType.OK)
				return result.value;
			return new Unit(UnitType.Null);
		}
		//////////////////////////////////////////////////// End Public

		ModuleUnit GetModule(string p_moduleName)
		{
			return modules[LoadedModules[p_moduleName]];
		}

		Operand CalculateEnvShift(Operand p_nShift)
		{
			return (Operand)(variables.Env - p_nShift);
		}

		Operand CalculateEnvShiftUpVal(Operand p_env)
		{
			return (Operand)(p_env - 1);// there are no global upvalues
		}

		void EnvPush()
		{
			variables.PushEnv();
			registeredUpValues.PushEnv();
		}

		void EnvPop()
		{
			registeredUpValues.PopEnv();
			variables.PopEnv();
		}

		void EnvSet(int p_target_env)
		{
			while ((variables.Env) > p_target_env)
			{
				EnvPop();
			}
		}

		public void Error(string p_msg)
		{
			throw new Exception(p_msg);
		}

		public string CurrentInstructionPositionDataString()
		{
			return "Function: " + instructions.ExecutingFunction.Name +
					" from module: " + instructions.ExecutingFunction.Module +
					" on position: " + instructions.ExecutingFunction.ChunkPosition.GetPosition(IP);
		}

		public string CurrentInstructionModule()
		{
			return instructions.ExecutingFunction.Module;
		}
		public PositionData CurrentInstructionPositionData()
		{
			return instructions.ExecutingFunction.ChunkPosition.GetPosition(IP);
		}

		public VMResult ProtectedRun()
		{
			try
			{
				return Run();
			}
			catch (Exception e)
			{
				Logger.LogLine("VM Busted ...\n" + e.ToString(), Defaults.Config.VMLogFile);
				return new VMResult(VMResultType.ERROR, new Unit(UnitType.Null));
			}
		}
		private VMResult Run()
		{
			Instruction instruction;

			while (true)
			{
				instruction = instructionsCache[IP];

				switch (instruction.opCode)
				{
					case OpCode.POP:
						IP++;
						stack.Pop();
						break;
					case OpCode.DUP:
						IP++;
						stack.Push(stack.Peek());
						break;
					case OpCode.LOAD_DATA:
						IP++;
						stack.Push(data[instruction.opA]);
						break;
					case OpCode.LOAD_VARIABLE:
						IP++;
						stack.Push(variables.GetAt(instruction.opA, CalculateEnvShift(instruction.opB)));
						break;
					case OpCode.LOAD_GLOBAL:
						IP++;
						if (parallelVM == true)
							lock (globals.Get(instruction.opA).heapUnitValue) // needed because global may be loaded from parallel functions
								stack.Push(globals.Get(instruction.opA));
						else
							stack.Push(globals.Get(instruction.opA));
						break;
					case OpCode.LOAD_IMPORTED_GLOBAL:
						IP++;
						stack.Push(modules[instruction.opB].GetGlobal(instruction.opA));
						break;
					case OpCode.LOAD_IMPORTED_DATA:
						IP++;
						stack.Push(modules[instruction.opB].GetData(instruction.opA));
						break;
					case OpCode.LOAD_UPVALUE:
						IP++;
						if (parallelVM == true)
							lock (upValues.GetAt(instruction.opA)) // needed because upvalue may be loaded from parallel functions
								stack.Push(upValues.GetAt(instruction.opA).UpValue);
						else
							stack.Push(upValues.GetAt(instruction.opA).UpValue);
						break;
					case OpCode.LOAD_NIL:
						IP++;
						stack.Push(new Unit(UnitType.Null));
						break;
					case OpCode.LOAD_TRUE:
						IP++;
						stack.Push(new Unit(true));
						break;
					case OpCode.LOAD_FALSE:
						IP++;
						stack.Push(new Unit(false));
						break;
					case OpCode.LOAD_INTRINSIC:
						IP++;
						stack.Push(new Unit(Intrinsics[instruction.opA]));
						break;
					case OpCode.DECLARE_VARIABLE:
						IP++;
						variables.Add(stack.Pop());
						break;
					case OpCode.DECLARE_GLOBAL:
						IP++;
						// lock(globals) // not needed because global declaration can not happen inside parallel functions
						globals.Add(stack.Pop());
						break;
					case OpCode.DECLARE_FUNCTION:
						{
							IP++;
							Operand env = instruction.opA;
							Operand is_function_expression = instruction.opB;
							Operand new_fun_address = instruction.opC;
							Unit this_callable = data[new_fun_address];
							if (this_callable.Type == UnitType.Function)
							{
								if (is_function_expression == 0)
									if (env == 0)// Global
									{
										// lock(globals) // not needed because global declaration can not happen inside parallel functions
										globals.Add(this_callable);
									}
									else
									{
										variables.Add(this_callable);
									}
								else
									stack.Push(this_callable);
							}
							else
							{
								ClosureUnit this_closure = (ClosureUnit)this_callable.heapUnitValue;

								// new upvalues
								List<UpValueUnit> new_upValues = new List<UpValueUnit>();
								foreach (UpValueUnit u in this_closure.UpValues)
								{
									// here we convert env from shift based to absolute based
									UpValueUnit new_upvalue = registeredUpValues.Get(u.Address, CalculateEnvShiftUpVal(u.Env));
									new_upValues.Add(new_upvalue);
								}
								ClosureUnit new_closure = new ClosureUnit(this_closure.Function, new_upValues);

								Unit new_closure_unit = new Unit(new_closure);
								if (is_function_expression == 0)
									if (env == 0)// yes they exist!
									{
										// lock(globals) // not needed because global declaration can not happen inside parallel functions
										globals.Add(new_closure_unit);
									}
									else
									{
										variables.Add(new_closure_unit);
									}
								else
									stack.Push(new_closure_unit);
							}
							break;
						}
					case OpCode.ASSIGN_VARIABLE:
						{
							IP++;

							Unit old_value = variables.GetAt(instruction.opA, CalculateEnvShift(instruction.opB));
							Unit result;
							switch (instruction.opC)
							{
								case ASSIGN:
									variables.SetAt(stack.Peek(), instruction.opA, CalculateEnvShift(instruction.opB));
									break;
								case ADDITION_ASSIGN:
									result = old_value + stack.Pop();
									stack.Push(result);
									variables.SetAt(result, instruction.opA, CalculateEnvShift(instruction.opB));
									break;
								case SUBTRACTION_ASSIGN:
									result = old_value - stack.Pop();
									stack.Push(result);
									variables.SetAt(result, instruction.opA, CalculateEnvShift(instruction.opB));
									break;
								case MULTIPLICATION_ASSIGN:
									result = old_value * stack.Pop();
									stack.Push(result);
									variables.SetAt(result, instruction.opA, CalculateEnvShift(instruction.opB));
									break;
								case DIVISION_ASSIGN:
									result = old_value / stack.Pop();
									stack.Push(result);
									variables.SetAt(result, instruction.opA, CalculateEnvShift(instruction.opB));
									break;
								default:
									throw new Exception("Unknown operator");
							}
							break;
						}
					case OpCode.ASSIGN_GLOBAL:
						{
							IP++;
							Operand address = instruction.opA;
							Operand op = instruction.opB;
							Unit new_value = stack.Peek();
							if (parallelVM == true)
							{
								switch (op)
								{
									case ASSIGN:
										lock (globals.Get(address).heapUnitValue)
											globals.Set(new_value, address);
										break;
									case ADDITION_ASSIGN:
										lock (globals.Get(address).heapUnitValue)
										{
											Unit result = globals.Get(address) + new_value;
											globals.Set(result, address);
										}
										break;
									case SUBTRACTION_ASSIGN:
										lock (globals.Get(address).heapUnitValue)
										{
											Unit result = globals.Get(address) - new_value;
											globals.Set(result, address);
										}
										break;
									case MULTIPLICATION_ASSIGN:
										lock (globals.Get(address).heapUnitValue)
										{
											Unit result = globals.Get(address) * new_value;
											globals.Set(result, address);
										}
										break;
									case DIVISION_ASSIGN:
										lock (globals.Get(address).heapUnitValue)
										{
											Unit result = globals.Get(address) / new_value;
											globals.Set(result, address);
										}
										break;
									default:
										throw new Exception("Unknown operator");
								}
							}
							else
							{
								Unit result;
								switch (op)
								{
									case ASSIGN:
										globals.Set(new_value, address);
										break;
									case ADDITION_ASSIGN:
										result = globals.Get(address) + new_value;
										globals.Set(result, address);
										break;
									case SUBTRACTION_ASSIGN:
										result = globals.Get(address) - new_value;
										globals.Set(result, address);
										break;
									case MULTIPLICATION_ASSIGN:
										result = globals.Get(address) * new_value;
										globals.Set(result, address);
										break;
									case DIVISION_ASSIGN:
										result = globals.Get(address) / new_value;
										globals.Set(result, address);
										break;
									default:
										throw new Exception("Unknown operator");
								}
							}
							break;
						}
					case OpCode.ASSIGN_IMPORTED_GLOBAL:
						IP++;
						modules[instruction.opB].SetOpGlobal(stack.Peek(), instruction.opC, instruction.opA);
						break;
					case OpCode.ASSIGN_UPVALUE:
						IP++;
						if (parallelVM == true)
						{
							UpValueUnit this_upValue;
							this_upValue = upValues.GetAt(instruction.opA);
							switch (instruction.opB)
							{
								case ASSIGN:
									lock (this_upValue)
										this_upValue.UpValue = stack.Peek();
									break;
								case ADDITION_ASSIGN:
									lock (this_upValue)
										this_upValue.UpValue = upValues.GetAt(instruction.opA).UpValue + stack.Peek();
									break;
								case SUBTRACTION_ASSIGN:
									lock (this_upValue)
										this_upValue.UpValue = upValues.GetAt(instruction.opA).UpValue - stack.Peek();
									break;
								case MULTIPLICATION_ASSIGN:
									lock (this_upValue)
										this_upValue.UpValue = upValues.GetAt(instruction.opA).UpValue * stack.Peek();
									break;
								case DIVISION_ASSIGN:
									lock (this_upValue)
										this_upValue.UpValue = upValues.GetAt(instruction.opA).UpValue / stack.Peek();
									break;
								default:
									throw new Exception("Unknown operator");
							}
						}
						else
						{
							UpValueUnit this_upValue = upValues.GetAt(instruction.opA);
							switch (instruction.opB)
							{
								case ASSIGN:
									this_upValue.UpValue = stack.Peek();
									break;
								case ADDITION_ASSIGN:
									this_upValue.UpValue = this_upValue.UpValue + stack.Peek();
									break;
								case SUBTRACTION_ASSIGN:
									this_upValue.UpValue = this_upValue.UpValue - stack.Peek();
									break;
								case MULTIPLICATION_ASSIGN:
									this_upValue.UpValue = this_upValue.UpValue * stack.Peek();
									break;
								case DIVISION_ASSIGN:
									this_upValue.UpValue = this_upValue.UpValue / stack.Peek();
									break;
								default:
									throw new Exception("Unknown operator");
							}
						}
						break;
					case OpCode.GET:
						{
							IP++;
							Operand indexes_counter = instruction.opA;

							Unit[] indexes = new Unit[indexes_counter];
							for (int i = indexes_counter - 1; i >= 0; i--)
								indexes[i] = stack.Pop();

							Unit value = stack.Pop();
							foreach (Unit v in indexes)
							{
								value = value.heapUnitValue.Get(v);
							}
							stack.Push(value);
							break;
						}

					case OpCode.SET:
						{
							IP++;
							Operand indexes_counter = instruction.opA;
							Operand op = instruction.opB;

							Unit[] indexes = new Unit[indexes_counter];
							for (int i = indexes_counter - 1; i >= 0; i--)
								indexes[i] = stack.Pop();

							Unit this_table = stack.Pop();

							for (int i = 0; i < indexes_counter - 1; i++)
							{
								Unit v = indexes[i];
								this_table = this_table.heapUnitValue.Get(v);
							}
							Unit new_value = stack.Peek();
							Unit index = indexes[indexes_counter - 1];
							UnitType index_type = indexes[indexes_counter - 1].Type;
							if (op == ASSIGN)
							{
								this_table.heapUnitValue.Set(index, new_value);
							}
							else if (parallelVM == true)
							{
								Unit old_value;
								Unit result;
								switch (op)
								{
									case ADDITION_ASSIGN:
										lock (this_table.heapUnitValue)
										{
											old_value = ((TableUnit)this_table.heapUnitValue).Get(index);
											result = old_value + new_value;
											this_table.heapUnitValue.Set(index, result);
										}
										break;
									case SUBTRACTION_ASSIGN:
										lock (this_table.heapUnitValue)
										{
											old_value = ((TableUnit)this_table.heapUnitValue).Get(index);
											result = old_value - new_value;
											this_table.heapUnitValue.Set(index, result);
										}
										break;
									case MULTIPLICATION_ASSIGN:
										lock (this_table.heapUnitValue)
										{
											old_value = ((TableUnit)this_table.heapUnitValue).Get(index);
											result = old_value * new_value;
											this_table.heapUnitValue.Set(index, result);
										}
										break;
									case DIVISION_ASSIGN:
										lock (this_table.heapUnitValue)
										{
											old_value = ((TableUnit)this_table.heapUnitValue).Get(index);
											result = old_value / new_value;
											this_table.heapUnitValue.Set(index, result);
										}
										break;
									default:
										throw new Exception("Unknown operator");

								}
							}
							else
							{
								Unit old_value = this_table.heapUnitValue.Get(index);
								Unit result;
								switch (op)
								{
									case ADDITION_ASSIGN:
										result = old_value + new_value;
										break;
									case SUBTRACTION_ASSIGN:
										result = old_value - new_value;
										break;
									case MULTIPLICATION_ASSIGN:
										result = old_value * new_value;
										break;
									case DIVISION_ASSIGN:
										result = old_value / new_value;
										break;
									default:
										throw new Exception("Unknown operator");

								}
								this_table.heapUnitValue.Set(index, result);
							}
							break;
						}
					case OpCode.JUMP:
						IP += instruction.opA;
						break;
					case OpCode.JUMP_IF_NOT_TRUE:
						if (stack.Pop().ToBool() == false)
						{
							IP += instruction.opA;
						}
						else
						{
							IP++;
						}
						break;
					case OpCode.JUMP_BACK:
						IP -= instruction.opA;
						break;
					case OpCode.OPEN_ENV:
						IP++;
						EnvPush();
						break;
					case OpCode.CLOSE_ENV:
						IP++;
						EnvPop();
						break;
					case OpCode.RETURN:
						IP = instructions.PopRET();
						break;
					case OpCode.RETURN_SET:
						instructions.PushRET((Operand)(instruction.opA + IP));
						IP++;
						break;
					case OpCode.ADD:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(stack.Pop() + opB);

							break;
						}
					case OpCode.APPEND:
						{
							IP++;
							Unit opB = stack.Pop();
							string result = stack.Pop().ToString() + opB.ToString();
							stack.Push(new Unit(result));
							break;
						}
					case OpCode.SUBTRACT:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(stack.Pop() - opB);
							break;
						}
					case OpCode.MULTIPLY:
						IP++;
						stack.Push(stack.Pop() * stack.Pop());
						break;
					case OpCode.DIVIDE:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(stack.Pop() / opB);
							break;
						}
					case OpCode.NEGATE:
						IP++;
						stack.Push(-stack.Pop());
						break;
					case OpCode.INCREMENT:
						IP++;
						stack.Push(stack.Pop() + 1);
						break;
					case OpCode.DECREMENT:
						IP++;
						stack.Push(stack.Pop() - 1);
						break;
					case OpCode.EQUALS:
						IP++;
						stack.Push(new Unit(stack.Pop().Equals(stack.Pop())));
						break;
					case OpCode.NOT_EQUALS:
						IP++;
						stack.Push(new Unit(!stack.Pop().Equals(stack.Pop())));
						break;
					case OpCode.GREATER_EQUALS:
						IP++;
						// the values are popped out of order from stack, so the logic is inverted!
						stack.Push(new Unit(stack.Pop().floatValue <= stack.Pop().floatValue));
						break;
					case OpCode.LESS_EQUALS:
						IP++;
						// the values are popped out of order from stack, so the logic is inverted!
						stack.Push(new Unit(stack.Pop().floatValue >= stack.Pop().floatValue));
						break;
					case OpCode.GREATER:
						IP++;
						// the values are popped out of order from stack, so the logic is inverted!
						stack.Push(new Unit(stack.Pop().floatValue < stack.Pop().floatValue));
						break;
					case OpCode.LESS:
						IP++;
						// the values are popped out of order from stack, so the logic is inverted!
						stack.Push(new Unit(stack.Pop().floatValue > stack.Pop().floatValue));
						break;
					case OpCode.NOT:
						IP++;
						stack.Push(new Unit(!stack.Pop().ToBool()));
						break;
					case OpCode.AND:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(stack.Pop().ToBool() && opB.ToBool()));
							break;
						}
					case OpCode.OR:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(stack.Pop().ToBool() || opB.ToBool()));
							break;
						}
					case OpCode.XOR:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(stack.Pop().ToBool() ^ opB.ToBool()));
							break;
						}
					case OpCode.NAND:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(!(stack.Pop().ToBool() && opB.ToBool())));
							break;
						}
					case OpCode.NOR:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(!(stack.Pop().ToBool() || opB.ToBool())));
							break;
						}
					case OpCode.XNOR:
						{
							IP++;
							Unit opB = stack.Pop();
							stack.Push(new Unit(!(stack.Pop().ToBool() ^ opB.ToBool())));
							break;
						}
					case OpCode.CLOSE_CLOSURE:
						upValues.PopEnv();
						EnvSet(instructions.TargetEnv);
						IP = instructions.PopFunction(out instructionsCache);

						break;
					case OpCode.CLOSE_FUNCTION:
						EnvSet(instructions.TargetEnv);
						IP = instructions.PopFunction(out instructionsCache);

						break;
					case OpCode.NEW_TABLE:
						{
							IP++;

							TableUnit new_table = new TableUnit(null);

							int n_table = instruction.opA;
							for (int i = 0; i < n_table; i++)
							{
								Unit val = stack.Pop();
								Unit key = stack.Pop();
								new_table.Map.Add(key, val);
							}

							stack.Push(new Unit(new_table));
							break;
						}
					case OpCode.NEW_LIST:
						{
							IP++;

							ListUnit new_list = new ListUnit(null);

							int n_table = instruction.opA;
							for (int i = 0; i < n_table; i++)
							{
								Unit val = stack.Pop();
								new_list.Elements.Add(val);
							}

							stack.Push(new Unit(new_list));
							break;
						}
					case OpCode.CALL:
						{
							IP++;

							Unit this_callable = stack.Pop();
							UnitType this_type = this_callable.Type;

							if (this_type == UnitType.Function)
							{
								FunctionUnit this_func = (FunctionUnit)this_callable.heapUnitValue;

								instructions.PushRET(IP);
								instructions.PushFunction(this_func, Env, out instructionsCache);

								IP = 0;
							}
							else if (this_type == UnitType.Closure)
							{
								ClosureUnit this_closure = (ClosureUnit)this_callable.heapUnitValue;

								upValues.PushEnv();

								foreach (UpValueUnit u in this_closure.UpValues)
								{
									upValues.Add(u);
								}

								instructions.PushRET(IP);
								instructions.PushFunction(this_closure.Function, Env, out instructionsCache);

								IP = 0;
							}
							else if (this_type == UnitType.Intrinsic)
							{
								IntrinsicUnit this_intrinsic = (IntrinsicUnit)this_callable.heapUnitValue;
								Unit result = this_intrinsic.Function(this);
								stack.top -= this_intrinsic.Arity;
								stack.Push(result);
							}
							else if (this_type == UnitType.ExternalFunction)
							{
								object[] arguments = [];
								ExternalFunctionUnit this_external_function = (ExternalFunctionUnit)this_callable.heapUnitValue;

								for (int i = this_external_function.Arity - 1; i >= 0; i--)
									arguments.Append(Unit.ToObject(stack.Pop()));

								Unit result = Unit.FromObject(this_external_function.Function.Invoke(null, arguments));
								stack.Push(result);
							}
							else
							{
								Error("Trying to call a " + this_callable.Type);
								return new VMResult(VMResultType.OK, new Unit(UnitType.Null));
							}
							break;
						}
					case OpCode.PUSH_STASH:
						IP++;
						stack.PushStash();
						break;
					case OpCode.POP_STASH:
						IP++;
						stack.PopStash();
						break;
					case OpCode.EXIT:
						if (stack.top > 0)
							return new VMResult(VMResultType.OK, stack.Pop());
						return new VMResult(VMResultType.OK, new Unit(UnitType.Null));
					default:
						Error("Unkown OpCode: " + instruction.opCode);
						return new VMResult(VMResultType.ERROR, new Unit(UnitType.Null));
				}
			}
		}

		public int StackCount()
		{
			return stack.top;
		}

		public int GlobalsCount()
		{
			int value;
			lock (globals) // I think it is not needed
				value = globals.Count;
			return value;
		}

		public int VariablesCount()
		{
			return variables.Count;
		}

		public int VariablesCapacity()
		{
			return variables.Capacity;
		}

		public int UpValuesCount()
		{
			int value;
			// lock(upValues)
			value = upValues.Count;
			return value;
		}

		public int UpValueCapacity()
		{
			int value;
			// lock(upValues)
			value = upValues.Capacity;
			return value;
		}
	}
}
