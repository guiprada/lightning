using System;
using System.Collections.Generic;

using Operand = System.UInt16;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
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
        static Stack<VM> vmPool;
        static VM vm0;
        int functionDeepness;
        bool parallelVM;

        int Env{ get{ return variables.Env; } }

//////////////////////////////////////////////////// Public
        public VM(Chunk p_chunk, int p_function_deepness = 100, Memory<Unit> p_globals = null, bool p_parallelVM = false)
        {
            if(vm0 == null)
                vm0 = this;

            chunk = p_chunk;
            IP = 0;
            functionDeepness = p_function_deepness;
            parallelVM = p_parallelVM;

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

        public void ResoursesTrim(){
            upValuesRegistry.Trim();
            variables.Trim();
            upValues.Trim();
        }
        public static void ReleaseVMs(int count){
            for (int i = 0; i < count; i++)
                if (vmPool.Count > 0)
                    vmPool.Pop();
                vmPool.TrimExcess();
        }
        public static void ReleaseVMs(){
            vmPool.Clear();
            vmPool.TrimExcess();
        }

        public static int CountVMs(){
            return vmPool.Count;
        }
        public static void RecycleVM(VM vm)
        {
            vmPool.Push(vm);
        }

        public VM GetVM()
        {
            if (vmPool.Count > 0)
            {
                return vmPool.Pop();
            }
            else
            {
                VM new_vm = new VM(chunk, 5, globals, true);
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

//////////////////////////// Accessors
        public Unit GetUnit(int n){
            return stack.Peek(n);
        }

        public string GetString(int n){
            return ((StringUnit)(stack.Peek(n).heapUnitValue)).content;
        }

        public Float GetNumber(int n){
            Unit this_value = stack.Peek(n);
            if(this_value.Type == UnitType.Integer)
                return this_value.integerValue;
            if(this_value.Type == UnitType.Float)
                return (this_value.floatValue);
            throw new Exception("Trying to get a integer value of non numeric type." + VM.ErrorString(this));
        }

        public Integer GetInteger(int n){
            Unit this_value = stack.Peek(n);
            if(this_value.Type == UnitType.Integer)
                return this_value.integerValue;
            if(this_value.Type == UnitType.Float)
                return (Integer)(this_value.floatValue);
            throw new Exception("Trying to get a integer value of non numeric type." + VM.ErrorString(this));
        }

        public TableUnit GetTable(int n){
            return (TableUnit)(stack.Peek(n).heapUnitValue);
        }

        public T GetWrappedContent<T>(int n){
            return ((WrapperUnit<T>)(stack.Peek(n).heapUnitValue)).UnWrapp();
        }

        public WrapperUnit<T> GetWrapperUnit<T>(int n){
            return ((WrapperUnit<T>)(stack.Peek(n).heapUnitValue));
        }

        public StringUnit GetStringUnit(int n){
            return ((StringUnit)(stack.Peek(n).heapUnitValue));
        }

        public char GetChar(int n){
            return (stack.Peek(n).charValue);
        }

        public bool GetBool(int n){
            return stack.Peek(n).ToBool();
        }

//////////////////////////// End Accessors
        public Unit CallFunction(Unit this_callable, List<Unit> args)
        {
            if (args != null)
                for (int i = args.Count - 1; i >= 0; i--)
                    stack.Push(args[i]);

            UnitType this_type = this_callable.Type;

            instructions.PushRET((Operand)(chunk.ProgramSize - 1));
            if (this_type == UnitType.Function)
            {
                FunctionUnit this_func = (FunctionUnit)(this_callable.heapUnitValue);
                instructions.PushFunction(this_func, Env, out instructionsCache);

                IP = 0;
            }
            else if (this_type == UnitType.Closure)
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
            else if (this_type == UnitType.Intrinsic)
            {
                IntrinsicUnit this_intrinsic = (IntrinsicUnit)(this_callable.heapUnitValue);
                Unit intrinsic_result = this_intrinsic.function(this);
                stack.top -= this_intrinsic.arity;
                stack.Push(intrinsic_result);
            }
            VMResult result = Run();
            if (result.status == VMResultType.OK)
                return result.value;
            return new Unit(UnitType.Null);
        }
//////////////////////////////////////////////////// End Public

        ModuleUnit GetModule(string p_module_name){
            return modules[loadedModules[p_module_name]];
        }

        Operand CalculateEnvShift(Operand n_shift){
            return (Operand)(variables.Env - n_shift);
        }

        Operand CalculateEnvShiftUpVal(Operand env){
            return (Operand)(variables.Env + 1 - env);
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
                Console.WriteLine("Error: " + msg + " on line: "+ chunk.lineCounter.GetLine(IP));
            else
            {
                Console.Write("Error: " + msg);
                Console.Write(" on function: " + instructions.ExecutingFunction.name);
                Console.Write(" from module: " + instructions.ExecutingFunction.module);
                Console.WriteLine(" on line: " + instructions.ExecutingFunction.lineCounter.GetLine(IP));
            }
        }

        public static string ErrorString(VM vm)
        {
            vm = vm ?? vm0;
            if (vm == null)
                return null;
            if (vm.instructions.ExecutingInstructionsIndex == 0)
                return "Line: " + vm.chunk.lineCounter.GetLine(vm.IP);
            else
            {
                return "Function: " + vm.instructions.ExecutingFunction.name +
                " from module: " + vm.instructions.ExecutingFunction.module +
                " on line: " + vm.instructions.ExecutingFunction.lineCounter.GetLine(vm.IP);
            }
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
                    case OpCode.LOAD_CONSTANT:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit constant = chunk.GetConstant(address);
                            stack.Push(constant);
                            break;
                        }
                    case OpCode.LOAD_VARIABLE:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            var variable = variables.GetAt(address, CalculateEnvShift(n_shift));
                            stack.Push(variable);
                            break;
                        }
                    case OpCode.LOAD_GLOBAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit global;
                            global = globals.Get(address);
                            stack.Push(global);
                            break;
                        }
                    case OpCode.LOAD_IMPORTED_GLOBAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit global = modules[module].globals[address];
                            stack.Push(global);
                            break;
                        }
                    case OpCode.LOAD_IMPORTED_CONSTANT:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit constant = modules[module].constants[address];

                            stack.Push(constant);
                            break;
                        }
                    case OpCode.LOAD_UPVALUE:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            UpValueUnit up_val = upValues.GetAt(address);
                            stack.Push(up_val.UpValue);
                            break;
                        }
                    case OpCode.LOAD_NIL:
                        {
                            IP++;
                            stack.Push(new Unit(UnitType.Null));
                            break;
                        }
                    case OpCode.LOAD_TRUE:
                        {
                            IP++;
                            stack.Push(new Unit(true));
                            break;
                        }
                    case OpCode.LOAD_FALSE:
                        {
                            IP++;
                            stack.Push(new Unit(false));
                            break;
                        }
                    case OpCode.LOAD_INTRINSIC:
                        {
                            IP++;
                            Operand value = instruction.opA;
                            stack.Push(new Unit(Intrinsics[value]));
                            break;
                        }
                    case OpCode.DECLARE_VARIABLE:
                        {
                            IP++;
                            Unit new_value = stack.Pop();
                            variables.Add(new_value);
                            break;
                        }
                    case OpCode.DECLARE_GLOBAL:
                        {
                            IP++;
                            Unit new_value = stack.Pop();
                            globals.Add(new_value);

                            break;
                        }
                    case OpCode.DECLARE_FUNCTION:
                        {
                            IP++;
                            Operand env = instruction.opA;
                            Operand lambda = instruction.opB;
                            Operand new_fun_address = instruction.opC;
                            Unit this_callable = chunk.GetConstant(new_fun_address);
                            if (this_callable.Type == UnitType.Function)
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
                    case OpCode.ASSIGN_VARIABLE:
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
                                Unit result;
                                if (op == 1)
                                    result = old_value + new_value;
                                else if (op == 2)
                                    result = old_value - new_value;
                                else if (op == 3)
                                    result = old_value * new_value;
                                else if (op == 4)
                                    result = old_value / new_value;
                                else
                                    throw new Exception("Unknown operator" + VM.ErrorString(this));
                                variables.SetAt(result, address, CalculateEnvShift(n_shift));
                            }
                            break;
                        }
                    case OpCode.ASSIGN_GLOBAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                globals.Set(new_value, address);
                            }else if(parallelVM == true){
                                if (op == 1)
                                    lock(globals){
                                        Unit result = globals.Get(address) + new_value;
                                        globals.Set(result, address);
                                    }
                                else if (op == 2)
                                    lock(globals){
                                        Unit result = globals.Get(address) - new_value;
                                        globals.Set(result, address);
                                    }
                                else if (op == 3)
                                    lock(globals){
                                        Unit result = globals.Get(address) * new_value;
                                        globals.Set(result, address);
                                    }
                                else if (op == 4)
                                    lock(globals){
                                        Unit result = globals.Get(address) / new_value;
                                        globals.Set(result, address);
                                    }
                            }else{
                                if (op == 1){
                                    Unit result = globals.Get(address) + new_value;
                                    globals.Set(result, address);
                                }
                                else if (op == 2)
                                {
                                    Unit result = globals.Get(address) - new_value;
                                    globals.Set(result, address);
                                }
                                else if (op == 3)
                                {
                                    Unit result = globals.Get(address) * new_value;
                                    globals.Set(result, address);
                                }
                                else if (op == 4)
                                {
                                    Unit result = globals.Get(address) / new_value;
                                    globals.Set(result, address);
                                }
                            }
                            break;
                        }
                    case OpCode.ASSIGN_UPVALUE:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            UpValueUnit this_upValue = upValues.GetAt(address);
                            Unit new_value = stack.Peek();
                            if (op == 0)
                            {
                                this_upValue.UpValue = new_value;
                            }else if(parallelVM == true){
                                if (op == 1)
                                    lock(this_upValue){
                                        this_upValue.UpValue = this_upValue.UpValue + new_value;
                                    }
                                else if (op == 2)
                                    lock(this_upValue){
                                        this_upValue.UpValue = this_upValue.UpValue - new_value;
                                    }
                                else if (op == 3)
                                    lock(this_upValue){
                                        this_upValue.UpValue = this_upValue.UpValue * new_value;
                                    }
                                else if (op == 4)
                                    lock(this_upValue){
                                        this_upValue.UpValue = this_upValue.UpValue / new_value;
                                    }
                            }else{
                                if (op == 1)
                                {
                                    this_upValue.UpValue = new Unit(this_upValue.UpValue.floatValue + new_value.floatValue);
                                }
                                else if (op == 2)
                                {
                                    this_upValue.UpValue = this_upValue.UpValue - new_value;
                                }
                                else if (op == 3)
                                {
                                    this_upValue.UpValue = this_upValue.UpValue * new_value;
                                }
                                else if (op == 4)
                                {
                                    this_upValue.UpValue = this_upValue.UpValue / new_value;
                                }
                            }
                            break;
                        }
                    case OpCode.TABLE_GET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;

                            Unit[] indexes = new Unit[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = stack.Pop();

                            Unit value = stack.Pop();
                            foreach (Unit v in indexes)
                            {
                                value = (value.heapUnitValue).Get(v);
                            }
                            stack.Push(value);
                            break;
                        }

                    case OpCode.TABLE_SET:
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
                                this_table = ((TableUnit)(this_table.heapUnitValue)).Get(v);
                            }
                            Unit new_value = stack.Peek();
                            Unit index = indexes[indexes_counter - 1];
                            UnitType index_type = indexes[indexes_counter - 1].Type;
                            if (op == 0)
                            {
                                ((TableUnit)(this_table.heapUnitValue)).Set(index, new_value);
                            }
                            else if(parallelVM == true)
                            {
                                Unit old_value;
                                Unit result;
                                lock(((TableUnit)(this_table.heapUnitValue)).elements){
                                    old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);

                                    if (op == 1)
                                        result = old_value + new_value;
                                    else if (op == 2)
                                        result = old_value - new_value;
                                    else if (op == 3)
                                        result = old_value * new_value;
                                    else if (op == 4)
                                        result = old_value / new_value;
                                    else
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));

                                    ((TableUnit)(this_table.heapUnitValue)).Set(index, result);
                                }
                            }
                            else
                            {
                                Unit old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);
                                Unit result;
                                if (op == 1)
                                    result = old_value + new_value;
                                else if (op == 2)
                                    result = old_value - new_value;
                                else if (op == 3)
                                    result = old_value * new_value;
                                else if (op == 4)
                                    result = old_value / new_value;
                                else
                                    throw new Exception("Unknown operator" + VM.ErrorString(this));

                                ((TableUnit)(this_table.heapUnitValue)).Set(index, result);

                            }
                            break;
                        }
                    case OpCode.JUMP:
                        {
                            Operand value = instruction.opA;
                            IP += value;
                            break;
                        }
                    case OpCode.JUMP_IF_NOT_TRUE:
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
                    case OpCode.JUMP_BACK:
                        {
                            Operand value = instruction.opA;
                            IP -= value;
                            break;
                        }
                    case OpCode.OPEN_ENV:
                        {
                            IP++;
                            EnvPush();
                            break;
                        }
                    case OpCode.CLOSE_ENV:
                        {
                            IP++;
                            EnvPop();
                            break;
                        }
                    case OpCode.RETURN:
                        {
                            IP = instructions.PopRET();
                            break;
                        }
                    case OpCode.RETURN_SET:
                        {
                            Operand value = instruction.opA;
                            instructions.PushRET((Operand)(value + IP));

                            IP++;
                            break;
                        }
                    case OpCode.ADD:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            stack.Push(opA + opB);

                            break;
                        }
                    case OpCode.APPEND:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            string result = opA.ToString() + opB.ToString();
                            Unit new_value = new Unit(result);
                            stack.Push(new_value);

                            break;
                        }
                    case OpCode.SUBTRACT:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            stack.Push(opA - opB);

                            break;
                        }
                    case OpCode.MULTIPLY:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            stack.Push(opA * opB);

                            break;
                        }
                    case OpCode.DIVIDE:
                        {
                            IP++;
                            Unit opB = stack.Pop();
                            Unit opA = stack.Pop();

                            stack.Push(opA / opB);

                            break;
                        }
                    case OpCode.NEGATE:
                        {
                            IP++;
                            Unit opA = stack.Pop();
                            stack.Push(-opA);
                            break;
                        }
                    case OpCode.INCREMENT:
                        {
                            IP++;
                            Unit opA = stack.Pop();
                            Unit new_value = opA + 1;
                            stack.Push(new_value);
                            break;
                        }
                    case OpCode.DECREMENT:
                        {
                            IP++;
                            Unit opA = stack.Pop();
                            Unit new_value = opA - 1;
                            stack.Push(new_value);
                            break;
                        }
                    case OpCode.EQUALS:
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
                    case OpCode.NOT_EQUALS:
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
                    case OpCode.GREATER_EQUALS:
                        {
                            IP++;
                            Float opB = stack.Pop().floatValue;
                            Float opA = stack.Pop().floatValue;
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
                    case OpCode.LESS_EQUALS:
                        {
                            IP++;
                            Float opB = stack.Pop().floatValue;
                            Float opA = stack.Pop().floatValue;
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
                    case OpCode.GREATER:
                        {
                            IP++;
                            Float opB = stack.Pop().floatValue;
                            Float opA = stack.Pop().floatValue;
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
                    case OpCode.LESS:
                        {
                            IP++;
                            Float opB = stack.Pop().floatValue;
                            Float opA = stack.Pop().floatValue;
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
                                return new VMResult(VMResultType.ERROR, new Unit(UnitType.Null));
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
                    case OpCode.CLOSE_CLOSURE:
                        {
                            upValues.PopEnv();

                            int target_env = instructions.TargetEnv;
                            EnvSet(target_env);
                            IP = instructions.PopFunction(out instructionsCache);

                            break;
                        }
                    case OpCode.CLOSE_FUNCTION:
                        {
                            int target_env = instructions.TargetEnv;
                            EnvSet(target_env);
                            IP = instructions.PopFunction(out instructionsCache);

                            break;
                        }
                    case OpCode.NEW_TABLE:
                        {
                            IP++;

                            TableUnit new_table = new TableUnit(null, null);

                            int n_table = instruction.opB;
                            for (int i = 0; i < n_table; i++)
                            {
                                Unit val = stack.Pop();
                                Unit key = stack.Pop();
                                new_table.table.Add(key, val);
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

                                foreach (UpValueUnit u in this_closure.upValues)
                                {
                                    upValues.Add(u);
                                }

                                instructions.PushRET(IP);
                                instructions.PushFunction(this_closure.function, Env, out instructionsCache);

                                IP = 0;
                            }
                            else if (this_type == UnitType.Intrinsic)
                            {
                                IntrinsicUnit this_intrinsic = (IntrinsicUnit)this_callable.heapUnitValue;
                                Unit result = this_intrinsic.function(this);
                                stack.top -= this_intrinsic.arity;
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
                        {
                            IP++;
                            stack.PushStash();
                            break;
                        }
                    case OpCode.POP_STASH:
                        {
                            IP++;
                            stack.PopStash();
                            break;
                        }
                    case OpCode.EXIT:
                        {
                            Unit result = new Unit(UnitType.Null);
                            if (stack.top > 0)
                                result = stack.Pop();
                            return new VMResult(VMResultType.OK, result);
                        }
                    default:
                        Error("Unkown OpCode: " + instruction.opCode);
                        return new VMResult(VMResultType.ERROR, new Unit(UnitType.Null));
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
