using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Operand = System.UInt16;

#if DOUBLE
    using Number = System.Double;
#else
    using Number = System.Single;
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
        Chunk chunk;
        Operand IP;

        Instructions instructions;
        List<Instruction> instructionsCache;

        Memory<Unit> globals; // used for global variables
        Memory<Unit> variables; // used for scoped variables

        public Stack stack;

        Memory<UpValueUnit> upValues;
        Memory<UpValueUnit> upValuesRegistry;

        List<IntrinsicUnit> Intrinsics { get; set; }
        public Dictionary<string, int> loadedModules { get; private set; }
        public List<ModuleUnit> modules;
        Stack<VM> vmPool;
        int functionDeepness;

        int Env{ get{ return variables.Env; } }

        public VM(Chunk p_chunk, int p_function_deepness = 100, Memory<Unit> p_globals = null)
        {
            chunk = p_chunk;
            IP = 0;
            functionDeepness = p_function_deepness;

            instructions = new Instructions(functionDeepness, chunk, out instructionsCache);

            stack = new Stack(functionDeepness);

            variables = new Memory<Unit>();
            upValues = new Memory<UpValueUnit>();
            upValuesRegistry = new Memory<UpValueUnit>();

            if(p_globals == null){
                globals = new Memory<Unit>();
                Intrinsics = chunk.Prelude.intrinsics;
                foreach (IntrinsicUnit v in Intrinsics)
                {
                    globals.Add(new Unit(v));
                }
                foreach (KeyValuePair<string, TableUnit> entry in chunk.Prelude.tables)
                {
                    globals.Add(new Unit(entry.Value));
                }
            }
            else
                globals = p_globals;

            loadedModules = new Dictionary<string, int>();
            modules = new List<ModuleUnit>();

            vmPool = new Stack<VM>();
        }

        Operand CalculateEnvShift(Operand n_shift){
            return (Operand)(variables.Env - n_shift);
        }

        Operand CalculateEnvShiftUpVal(Operand env){
            return (Operand)(variables.Env + 1 - env);
        }

        public void ResoursesTrim(){
            upValuesRegistry.Trim();
            variables.Trim();
            upValues.Trim();
        }
        public void ReleaseVMs(int count){
            for (int i = 0; i < count; i++)
                if (vmPool.Count > 0)
                    vmPool.Pop();
                vmPool.TrimExcess();
        }
        public void ReleaseVMs(){
            vmPool.Clear();
            vmPool.TrimExcess();
        }

        public int CountVMs(){
            return vmPool.Count;
        }
        void RecycleVM(VM vm)
        {
            vmPool.Push(vm);
        }

        VM GetVM()
        {
            if (vmPool.Count > 0)
            {
                return vmPool.Pop();
            }
            else
            {
                VM new_vm = new VM(chunk, 5, globals);
                return new_vm;
            }
        }

        public Operand AddModule(ModuleUnit this_module)
        {
            loadedModules.Add(this_module.name, loadedModules.Count);
            modules.Add(this_module);
            return (Operand)(loadedModules.Count - 1);
        }

        public Chunk GetChunk()
        {
            return chunk;
        }

        public Unit GetGlobal(Operand address)
        {
            return globals.Get(address);
        }

        void RegisterUpValue(UpValueUnit u)
        {
            upValuesRegistry.Add(u);
        }

        void EnvPush()
        {
            variables.PushEnv();
            upValuesRegistry.PushEnv();
        }

        void EnvPop()
        {
            // capture closures
            int upvalues_start = upValuesRegistry.Marker;
            int upvalues_end = upValuesRegistry.Count;
            for (int i = upvalues_start; i < upvalues_end; i++)
            {
                upValuesRegistry.Get(i).Capture();
            }
            upValuesRegistry.PopEnv();
            variables.PopEnv();
        }

        void EnvSet(int target_env)
        {
            while ((variables.Env) > target_env)
            {
                EnvPop();
            }
        }

        void Error(string msg)
        {
            if (instructions.ExecutingInstructionsIndex == 0)
                Console.WriteLine("Error: " + msg + chunk.GetLine(IP));
            else
            {
                Console.Write("Error: " + msg);
                Console.Write(" on function: " + instructions.ExecutingFunction.name);
                Console.Write(" from module: " + instructions.ExecutingFunction.module);
                Console.WriteLine(" on line: " + chunk.GetLine(IP + instructions.ExecutingFunction.originalPosition));
            }
        }

        public Unit CallFunction(Unit this_callable, List<Unit> args)
        {
            if (args != null)
                for (int i = args.Count - 1; i >= 0; i--)
                    stack.Push(args[i]);

            Type this_type = this_callable.HeapUnitType();

            instructions.PushRET((Operand)(chunk.ProgramSize - 1));
            if (this_type == typeof(FunctionUnit))
            {
                FunctionUnit this_func = (FunctionUnit)(this_callable.heapUnitValue);
                instructions.PushFunction(this_func, Env, out instructionsCache);

                IP = 0;
            }
            else if (this_type == typeof(ClosureUnit))
            {
                ClosureUnit this_closure = (ClosureUnit)(this_callable.heapUnitValue);

                upValues.PushEnv();

                foreach (UpValueUnit u in this_closure.upValues)
                {
                    upValues.Add(u);
                }
                instructions.PushFunction(this_closure.function, Env, out instructionsCache);

                IP = 0;
            }
            else if (this_type == typeof(IntrinsicUnit))
            {
                IntrinsicUnit this_intrinsic = (IntrinsicUnit)(this_callable.heapUnitValue);
                Unit intrinsic_result = this_intrinsic.function(this);
                stack.top -= this_intrinsic.arity;
                stack.Push(intrinsic_result);
            }
            VMResult result = Run();
            if (result.status == VMResultType.OK)
                return result.value;
            return new Unit("null");
        }

        public VMResult Run()
        {
            Instruction instruction;

            while (true)
            {
                instruction = instructionsCache[IP];

                switch (instruction.opCode)
                {
                    case OpCode.POP:
                        {
                            IP++;
                            Unit value = stack.Pop();
                            break;
                        }
                    case OpCode.LOADC:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit constant = chunk.GetConstant(address);
                            stack.Push(constant);
                            break;
                        }
                    case OpCode.LOADV:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            var variable = variables.GetAt(address, CalculateEnvShift(n_shift));
                            stack.Push(variable);
                            break;
                        }
                    case OpCode.LOADG:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit global;
                            global = globals.Get(address);
                            stack.Push(global);
                            break;
                        }
                    case OpCode.LOADGI:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit global = modules[module].globals[address];
                            stack.Push(global);
                            break;
                        }
                    case OpCode.LOADCI:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit constant = modules[module].constants[address];

                            stack.Push(constant);
                            break;
                        }
                    case OpCode.LOADUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            UpValueUnit up_val = upValues.GetAt(address);
                            stack.Push(up_val.UpValue);
                            break;
                        }
                    case OpCode.LOADNIL:
                        {
                            IP++;
                            stack.Push(new Unit("null"));
                            break;
                        }
                    case OpCode.LOADTRUE:
                        {
                            IP++;
                            stack.Push(new Unit(true));
                            break;
                        }
                    case OpCode.LOADFALSE:
                        {
                            IP++;
                            stack.Push(new Unit(false));
                            break;
                        }
                    case OpCode.LOADINTR:
                        {
                            IP++;
                            Operand value = instruction.opA;
                            stack.Push(new Unit(Intrinsics[value]));
                            break;
                        }
                    case OpCode.VARDCL:
                        {
                            IP++;
                            Unit new_value = stack.Pop();
                            variables.Add(new_value);
                            break;
                        }
                    case OpCode.GLOBALDCL:
                        {
                            IP++;
                            Unit new_value = stack.Pop();
                            globals.Add(new_value);

                            break;
                        }
                    case OpCode.FUNDCL:
                        {
                            IP++;
                            Operand env = instruction.opA;
                            Operand lambda = instruction.opB;
                            Operand new_fun_address = instruction.opC;
                            Unit this_callable = chunk.GetConstant(new_fun_address);
                            if (this_callable.HeapUnitType() == typeof(FunctionUnit))
                            {
                                if (lambda == 0)
                                    if (env == 0)// Global
                                    {
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
                                ClosureUnit this_closure = (ClosureUnit)(this_callable.heapUnitValue);

                                // new upvalues
                                List<UpValueUnit> new_upValues = new List<UpValueUnit>();
                                foreach (UpValueUnit u in this_closure.upValues)
                                {
                                    // here we convert env from shift based to absolute based
                                    UpValueUnit new_upvalue = new UpValueUnit(u.address, CalculateEnvShiftUpVal(u.env));
                                    new_upValues.Add(new_upvalue);
                                }
                                ClosureUnit new_closure = new ClosureUnit(this_closure.function, new_upValues);

                                new_closure.Register(variables);
                                foreach (UpValueUnit u in new_closure.upValues)
                                {
                                    RegisterUpValue(u);
                                }
                                Unit new_closure_unit = new Unit(new_closure);
                                if (lambda == 0)
                                    if (env == 0)// yes they exist!
                                    {
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
                    case OpCode.ASSIGN:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            Operand op = instruction.opC;
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                variables.SetAt(new_value, address, CalculateEnvShift(n_shift));
                            }
                            else
                            {
                                Unit old_value = variables.GetAt(address, CalculateEnvShift(n_shift));
                                Number result = 0;
                                if (op == 1)
                                    result = old_value.unitValue + new_value.unitValue;
                                else if (op == 2)
                                    result = old_value.unitValue - new_value.unitValue;
                                else if (op == 3)
                                    result = old_value.unitValue * new_value.unitValue;
                                else if (op == 4)
                                    result = old_value.unitValue / new_value.unitValue;
                                variables.SetAt(new Unit(result), address, CalculateEnvShift(n_shift));
                            }
                            break;
                        }
                    case OpCode.ASSIGNG:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                lock(globals){
                                    globals.Set(new_value, address);
                                }
                            }
                            else
                            {
                                if (op == 1)
                                    lock(globals){
                                        Unit result = new Unit(globals.Get(address).unitValue + new_value.unitValue);
                                        globals.Set(result, address);
                                    }
                                else if (op == 2)
                                    lock(globals){
                                        Unit result = new Unit(globals.Get(address).unitValue - new_value.unitValue);
                                        globals.Set(result, address);
                                    }
                                else if (op == 3)
                                    lock(globals){
                                        Unit result = new Unit(globals.Get(address).unitValue * new_value.unitValue);
                                        globals.Set(result, address);
                                    }
                                else if (op == 4)
                                    lock(globals){
                                        Unit result = new Unit(globals.Get(address).unitValue / new_value.unitValue);
                                        globals.Set(result, address);
                                    }
                            }
                            break;
                        }
                    case OpCode.ASSIGNUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            UpValueUnit this_upValue = upValues.GetAt(address);
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                lock(this_upValue){
                                    this_upValue.UpValue = new_value;
                                }
                            }
                            else
                            {
                                if (op == 1)
                                    lock(this_upValue){
                                        this_upValue.UpValue = new Unit(this_upValue.UpValue.unitValue + new_value.unitValue);
                                    }
                                else if (op == 2)
                                    lock(this_upValue){
                                        this_upValue.UpValue = new Unit(this_upValue.UpValue.unitValue - new_value.unitValue);
                                    }
                                else if (op == 3)
                                    lock(this_upValue){
                                        this_upValue.UpValue = new Unit(this_upValue.UpValue.unitValue * new_value.unitValue);
                                    }
                                else if (op == 4)
                                    lock(this_upValue){
                                        this_upValue.UpValue = new Unit(this_upValue.UpValue.unitValue / new_value.unitValue);
                                    }
                            }
                            break;
                        }
                    case OpCode.TABLEGET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;

                            Unit[] indexes = new Unit[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = stack.Pop();

                            Unit value = stack.Pop();
                            foreach (Unit v in indexes)
                            {
                                if (v.type == UnitType.Number)
                                {
                                    value = ((TableUnit)(value.heapUnitValue)).elements[(int)v.unitValue];
                                }
                                else
                                {
                                    value = ((TableUnit)(value.heapUnitValue)).table[(StringUnit)v.heapUnitValue];
                                }
                            }
                            stack.Push(value);
                            break;
                        }

                    case OpCode.TABLESET:
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
                                if (v.type == UnitType.Number)
                                {
                                    this_table = ((TableUnit)(this_table.heapUnitValue)).elements[(int)v.unitValue];
                                }
                                else
                                {
                                    this_table = ((TableUnit)(this_table.heapUnitValue)).table[(StringUnit)v.heapUnitValue];
                                }
                            }
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                {
                                    if (((TableUnit)(this_table.heapUnitValue)).elements.Count - 1 >= ((int)(indexes[indexes_counter - 1].unitValue)))
                                    {
                                        Unit old_value = ((TableUnit)(this_table.heapUnitValue)).elements[(int)(indexes[indexes_counter - 1].unitValue)];
                                    }
                                    ((TableUnit)(this_table.heapUnitValue)).ElementSet((int)(indexes[indexes_counter - 1].unitValue), new_value);
                                }
                                else
                                {
                                    if (((TableUnit)(this_table.heapUnitValue)).table.ContainsKey((StringUnit)indexes[indexes_counter - 1].heapUnitValue))
                                    {
                                        Unit old_value = ((TableUnit)(this_table.heapUnitValue)).table[(StringUnit)indexes[indexes_counter - 1].heapUnitValue];
                                    }
                                    ((TableUnit)(this_table.heapUnitValue)).TableSet((StringUnit)indexes[indexes_counter - 1].heapUnitValue, new_value);
                                }
                            }
                            else
                            {
                                Unit old_value;
                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                    old_value = ((TableUnit)(this_table.heapUnitValue)).elements[(int)(indexes[indexes_counter - 1].unitValue)];
                                else
                                    old_value = ((TableUnit)(this_table.heapUnitValue)).table[(StringUnit)indexes[indexes_counter - 1].heapUnitValue];

                                Number result = 0;
                                if (op == 1)
                                    result = old_value.unitValue + new_value.unitValue;
                                else if (op == 2)
                                    result = old_value.unitValue - new_value.unitValue;
                                else if (op == 3)
                                    result = old_value.unitValue * new_value.unitValue;
                                else if (op == 4)
                                    result = old_value.unitValue / new_value.unitValue;

                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                    ((TableUnit)(this_table.heapUnitValue)).elements[(int)(indexes[indexes_counter - 1].unitValue)] = new Unit(result);
                                else
                                    ((TableUnit)(this_table.heapUnitValue)).table[(StringUnit)indexes[indexes_counter - 1].heapUnitValue] = new Unit(result);

                            }
                            break;
                        }
                    case OpCode.JMP:
                        {
                            Operand value = instruction.opA;
                            IP += value;
                            break;
                        }
                    case OpCode.JNT:
                        {
                            bool value = stack.Pop().ToBool();
                            if (value == false)
                            {
                                IP += instruction.opA;
                            }
                            else
                            {
                                IP++;
                            }
                            break;
                        }
                    case OpCode.JMPB:
                        {
                            Operand value = instruction.opA;
                            IP -= value;
                            break;
                        }
                    case OpCode.NENV:
                        {
                            IP++;
                            EnvPush();
                            break;
                        }
                    case OpCode.CENV:
                        {
                            IP++;
                            EnvPop();
                            break;
                        }
                    case OpCode.RET:
                        {
                            IP = instructions.PopRET();
                            break;
                        }
                    case OpCode.SETRET:
                        {
                            Operand value = instruction.opA;
                            instructions.PushRET((Operand)(value + IP));

                            IP++;
                            break;
                        }
                    case OpCode.ADD:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;

                            Number result = opA + opB;
                            stack.Push(new Unit(result));

                            break;
                        }
                    case OpCode.APP:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            string result = opA.ToString() + opB.ToString();
                            Unit new_value = new Unit(new StringUnit(result));
                            stack.Push(new_value);

                            break;
                        }
                    case OpCode.SUB:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;

                            Number result = opA - opB;
                            stack.Push(new Unit(result));

                            break;
                        }
                    case OpCode.MUL:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;

                            Number result = opA * opB;
                            stack.Push(new Unit(result));

                            break;
                        }
                    case OpCode.DIV:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;

                            Number result = opA / opB;
                            stack.Push(new Unit(result));

                            break;
                        }
                    case OpCode.NEG:
                        {
                            IP++;
                            Number opA = stack.Pop().unitValue;
                            Unit new_value = new Unit(-opA);
                            stack.Push(new_value);
                            break;
                        }
                    case OpCode.INC:
                        {
                            IP++;
                            Number opA = stack.Pop().unitValue;
                            Unit new_value = new Unit(opA + 1);
                            stack.Push(new_value);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            IP++;
                            Number opA = stack.Pop().unitValue;
                            Unit new_value = new Unit(opA - 1);
                            stack.Push(new_value);
                            break;
                        }
                    case OpCode.EQ:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();
                            if (opA.Equals(opB))
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }

                            break;
                        }
                    case OpCode.NEQ:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            if (!opA.Equals(opB))
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }

                            break;
                        }
                    case OpCode.GTQ:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;
                            bool truthness = opA >= opB;
                            if (truthness == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.LTQ:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;
                            bool truthness = opA <= opB;
                            if (truthness == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.GT:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;
                            bool truthness = opA > opB;
                            if (truthness == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.LT:
                        {
                            IP++;
                            Number opB = stack.Pop().unitValue;
                            Number opA = stack.Pop().unitValue;
                            bool truthness = opA < opB;
                            if (truthness == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.NOT:
                        {
                            IP++;
                            bool truthness = stack.Pop().ToBool();
                            if (truthness == true)
                                stack.Push(new Unit(false));
                            else if (truthness == false)
                                stack.Push(new Unit(true));
                            else
                            {
                                Error("NOT is insane!");
                                return new VMResult(VMResultType.ERROR, new Unit("null"));
                            }

                            break;
                        }
                    case OpCode.AND:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();
                            bool result = opA_truthness && opB_truthness;
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.OR:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();

                            bool result = opA_truthness || opB_truthness;
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.XOR:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();
                            bool result = opA_truthness ^ opB_truthness;
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.NAND:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();
                            bool result = !(opA_truthness && opB_truthness);
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.NOR:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();
                            bool result = !(opA_truthness || opB_truthness);
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.XNOR:
                        {
                            IP++;
                            bool opB_truthness = stack.Pop().ToBool();
                            bool opA_truthness = stack.Pop().ToBool();
                            bool result = !(opA_truthness ^ opB_truthness);
                            if (result == true)
                            {
                                stack.Push(new Unit(true));
                            }
                            else
                            {
                                stack.Push(new Unit(false));
                            }
                            break;
                        }
                    case OpCode.CLOSURECLOSE:
                        {
                            upValues.PopEnv();

                            int target_env = instructions.TargetEnv;
                            EnvSet(target_env);
                            IP = instructions.PopFunction(out instructionsCache);

                            break;
                        }
                    case OpCode.FUNCLOSE:
                        {
                            int target_env = instructions.TargetEnv;
                            EnvSet(target_env);
                            IP = instructions.PopFunction(out instructionsCache);

                            break;
                        }
                    case OpCode.NTABLE:
                        {
                            IP++;

                            TableUnit new_table = new TableUnit(null, null);

                            int n_table = instruction.opB;
                            for (int i = 0; i < n_table; i++)
                            {
                                Unit val = stack.Pop();
                                Unit key = stack.Pop();
                                new_table.table.Add((StringUnit)key.heapUnitValue, val);
                            }

                            int n_elements = instruction.opA;
                            for (int i = 0; i < n_elements; i++)
                            {
                                Unit new_value = stack.Pop();
                                new_table.elements.Add(new_value);
                            }

                            stack.Push(new Unit(new_table));
                            break;
                        }
                    case OpCode.CALL:
                        {
                            IP++;

                            Unit this_callable = stack.Pop();
                            Type this_type = this_callable.HeapUnitType();

                            if (this_type == typeof(FunctionUnit))
                            {
                                FunctionUnit this_func = (FunctionUnit)this_callable.heapUnitValue;

                                instructions.PushRET(IP);
                                instructions.PushFunction(this_func, Env, out instructionsCache);

                                IP = 0;
                            }
                            else if (this_type == typeof(ClosureUnit))
                            {
                                ClosureUnit this_closure = (ClosureUnit)this_callable.heapUnitValue;

                                upValues.PushEnv();

                                foreach (UpValueUnit u in this_closure.upValues)
                                {
                                    upValues.Add(u);
                                }

                                instructions.PushRET(IP);
                                instructions.PushFunction(this_closure.function, Env, out instructionsCache);

                                IP = 0;
                            }
                            else if (this_type == typeof(IntrinsicUnit))
                            {
                                IntrinsicUnit this_intrinsic = (IntrinsicUnit)this_callable.heapUnitValue;
                                Unit result = this_intrinsic.function(this);
                                stack.top -= this_intrinsic.arity;
                                stack.Push(result);
                            }
                            else
                            {
                                Error("Trying to call a " + this_callable.HeapUnitType());
                                return new VMResult(VMResultType.OK, new Unit("null"));
                            }
                            break;
                        }
                    case OpCode.PUSHSTASH:
                        {
                            IP++;
                            stack.PushStash();
                            break;
                        }
                    case OpCode.POPSTASH:
                        {
                            IP++;
                            stack.PopStash();
                            break;
                        }
                    case OpCode.FOREACH:
                        {
                            IP++;
                            Unit func = stack.Pop();
                            TableUnit table = (TableUnit)(stack.Pop().heapUnitValue);

                            int init = 0;
                            int end = table.ECount;
                            VM[] vms = new VM[end];
                            for (int i = init; i < end; i++)
                            {
                                vms[i] = GetVM();
                            }
                            System.Threading.Tasks.Parallel.For(init, end, (index) =>
                            {
                                List<Unit> args = new List<Unit>();
                                args.Add(new Unit(index));
                                args.Add(new Unit(table));
                                vms[index].CallFunction(func, args);
                            });
                            for (int i = init; i < end; i++)
                            {
                                RecycleVM(vms[i]);
                            }
                            break;
                        }
                    case OpCode.RANGE:
                        {
                            IP++;
                            Unit func = stack.Pop();
                            TableUnit table = (TableUnit)(stack.Pop().heapUnitValue);
                            Number tasks = stack.Pop().unitValue;

                            int n_tasks = (int)tasks;

                            int init = 0;
                            int end = n_tasks;
                            VM[] vms = new VM[end];
                            for (int i = 0; i < end; i++)
                            {
                                vms[i] = GetVM();
                            }

                            int count = table.ECount;
                            int step = (count / n_tasks) + 1;

                            System.Threading.Tasks.Parallel.For(init, end, (index) =>
                            {
                                List<Unit> args = new List<Unit>();
                                int range_start = index * step;
                                args.Add(new Unit(range_start));
                                int range_end = range_start + step;
                                if (range_end > count) range_end = count;
                                args.Add(new Unit(range_end));
                                args.Add(new Unit(table));
                                vms[index].CallFunction(func, args);
                            });
                            for (int i = 0; i < end; i++)
                            {
                                RecycleVM(vms[i]);
                            }
                            break;
                        }
                    case OpCode.EXIT:
                        {
                            Unit result = new Unit("null");
                            if (stack.top > 0)
                                result = stack.Pop();
                            return new VMResult(VMResultType.OK, result);
                        }
                    default:
                        Error("Unkown OpCode: " + instruction.opCode);
                        return new VMResult(VMResultType.ERROR, new Unit("null"));
                }
            }
        }

        public int StackCount(){
            return stack.top;
        }

        public int GlobalsCount(){
            return globals.Count;
        }

        public int VariablesCount(){
            return variables.Count;
        }

        public int VariablesCapacity(){
            return variables.Capacity;
        }

        public int UpValuesCount(){
            return upValues.Count;
        }

        public int UpValueCapacity(){
            return upValues.Capacity;
        }
    }
}
