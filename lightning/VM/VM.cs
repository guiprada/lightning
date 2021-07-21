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

        Operand IP;
        FunctionUnit main;
        private bool parallelVM;

        Instructions instructions;
        List<Instruction> instructionsCache;

        Memory<Unit> globals; // used for global variables
        Memory<Unit> variables; // used for scoped variables
        List<Unit> constants;

        public Stack stack;

        Memory<UpValueUnit> upValues;
        Memory<UpValueUnit> upValuesRegistry;
        UpValueMatrix registeredUpValues;

        public List<IntrinsicUnit> Intrinsics { get; private set; }
        public Library Prelude { get; private set; }
        public Dictionary<string, int> LoadedModules { get; private set; }
        public List<ModuleUnit> modules;
        public List<Unit> Constants{ get {return constants;}}
        int Env{ get{ return variables.Env; } }

        const Operand ASSIGN = (Operand)AssignmentOperatorType.ASSIGN;
        const Operand ADDITION_ASSIGN = (Operand)AssignmentOperatorType.ADDITION_ASSIGN;
        const Operand SUBTRACTION_ASSIGN = (Operand)AssignmentOperatorType.SUBTRACTION_ASSIGN;
        const Operand MULTIPLICATION_ASSIGN = (Operand)AssignmentOperatorType.MULTIPLICATION_ASSIGN;
        const Operand DIVISION_ASSIGN = (Operand)AssignmentOperatorType.DIVISION_ASSIGN;

        Stack<VM> vmPool;
        static int functionDeepness;

//////////////////////////////////////////////////// Public
        static VM(){
            functionDeepness = 30;
        }
        public VM(Chunk p_chunk)
        {
            IP = 0;
            parallelVM = false;

            main = p_chunk.GetFunctionUnit("main");

            instructions = new Instructions(functionDeepness, main, out instructionsCache);

            stack = new Stack(functionDeepness);

            variables = new Memory<Unit>();
            upValues = new Memory<UpValueUnit>();
            upValuesRegistry = new Memory<UpValueUnit>();
            registeredUpValues = new UpValueMatrix();

            constants = p_chunk.GetConstants;
            Prelude = p_chunk.Prelude;
            globals = new Memory<Unit>();
            Intrinsics = p_chunk.Prelude.intrinsics;
            foreach (IntrinsicUnit v in Intrinsics)
            {
                globals.Add(new Unit(v));
            }
            foreach (KeyValuePair<string, TableUnit> entry in p_chunk.Prelude.tables)
            {
                globals.Add(new Unit(entry.Value));
            }
            LoadedModules = new Dictionary<string, int>();
            modules = new List<ModuleUnit>();

            vmPool = new Stack<VM>();
        }

        private VM(
            FunctionUnit p_main,
            List<Unit> p_constants,
            Memory<Unit> p_globals,
            Library p_Prelude,
            Dictionary<string,int> p_LoadedModules,
            List<ModuleUnit> p_modules,
            bool p_parallelVM)
        {
            main = p_main;
            constants = p_constants;
            globals = p_globals;
            Prelude = p_Prelude;
            LoadedModules = p_LoadedModules;
            modules = p_modules;

            IP = 0;
            parallelVM = p_parallelVM;

            instructions = new Instructions(functionDeepness, main, out instructionsCache);
            stack = new Stack(functionDeepness);
            variables = new Memory<Unit>();
            upValues = new Memory<UpValueUnit>();
            upValuesRegistry = new Memory<UpValueUnit>();
            registeredUpValues = new UpValueMatrix();
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

        void Reset(){
            IP = 0;
            parallelVM = true;
            instructions.Reset();
            stack.Clear();
            variables.Clear();
            upValues.Clear();
            upValuesRegistry.Clear();
            registeredUpValues.Clear();
        }
        public void RecycleVM(VM vm)
        {
            vm.Reset();
            vmPool.Push(vm);
        }

        public VM GetVM(){
            return GetVM(parallelVM);
        }
        public VM GetParallelVM(){
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
                VM new_vm = new VM(main, constants, globals, Prelude, LoadedModules, modules, p_parallelVM);
                return new_vm;
            }
        }

        public void SetParallel(bool p_parallelVM){
            parallelVM = p_parallelVM;
        }

        public Operand AddModule(ModuleUnit this_module)
        {
            LoadedModules.Add(this_module.Name, LoadedModules.Count);
            modules.Add(this_module);
            return (Operand)(LoadedModules.Count - 1);
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
            UnitType this_type = this_callable.Type;
            if (args != null)
                for (int i = args.Count - 1; i >= 0; i--)
                    stack.Push(args[i]);

            if (this_type == UnitType.Function)
            {
                instructions.PushRET((Operand)(main.Body.Count - 1));
                FunctionUnit this_func = (FunctionUnit)(this_callable.heapUnitValue);
                instructions.PushFunction(this_func, Env, out instructionsCache);

                IP = 0;
            }
            else if (this_type == UnitType.Closure)
            {
                instructions.PushRET((Operand)(main.Body.Count - 1));
                ClosureUnit this_closure = (ClosureUnit)(this_callable.heapUnitValue);

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
                IntrinsicUnit this_intrinsic = (IntrinsicUnit)(this_callable.heapUnitValue);
                Unit intrinsic_result = this_intrinsic.Function(this);
                stack.top -= this_intrinsic.Arity;
                stack.Push(intrinsic_result);
            }
            VMResult result = Run();
            if (result.status == VMResultType.OK)
                return result.value;
            return new Unit(UnitType.Null);
        }
//////////////////////////////////////////////////// End Public

        ModuleUnit GetModule(string p_module_name){
            return modules[LoadedModules[p_module_name]];
        }

        Operand CalculateEnvShift(Operand n_shift){
            return (Operand)(variables.Env - n_shift);
        }

        Operand CalculateEnvShiftUpVal(Operand env){
            return (Operand)(variables.Env + 1 - env);
        }

        UpValueUnit GetUpValue(Operand p_address, Operand p_env){
            UpValueUnit up_value = registeredUpValues.Get(p_address, p_env);
            if(up_value == null){
                up_value = new UpValueUnit(p_address, p_env);
                registeredUpValues.Set(up_value, p_address, p_env);
            }
            return up_value;
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
            registeredUpValues.Clear();
            variables.PopEnv();
        }

        void EnvSet(int target_env)
        {
            while ((variables.Env) > target_env)
            {
                EnvPop();
            }
        }

        public void Error(string msg)
        {
            Console.Write("Error: " + msg);
            Console.Write(" on function: " + instructions.ExecutingFunction.Name);
            Console.Write(" from module: " + instructions.ExecutingFunction.Module);
            Console.WriteLine(" on line: " + instructions.ExecutingFunction.LineCounter.GetLine(IP));
        }

        public static string ErrorString(VM vm)
        {
            if (vm == null)
                return null;
            return "Function: " + vm.instructions.ExecutingFunction.Name +
            " from module: " + vm.instructions.ExecutingFunction.Module +
            " on line: " + vm.instructions.ExecutingFunction.LineCounter.GetLine(vm.IP);
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
                        IP++;
                        stack.Pop();
                        break;
                    case OpCode.LOAD_CONSTANT:
                        IP++;
                        stack.Push(constants[instruction.opA]);
                        break;
                    case OpCode.LOAD_VARIABLE:
                        IP++;
                        stack.Push(variables.GetAt(instruction.opA, CalculateEnvShift(instruction.opB)));
                        break;
                    case OpCode.LOAD_GLOBAL:
                        IP++;
                        stack.Push(globals.Get(instruction.opA));
                        break;
                    case OpCode.LOAD_IMPORTED_GLOBAL:
                        IP++;
                        Unit global = modules[instruction.opB].GetGlobal(instruction.opA);
                        stack.Push(global);
                        break;
                    case OpCode.LOAD_IMPORTED_CONSTANT:
                        IP++;
                        stack.Push(modules[instruction.opB].GetConstant(instruction.opA));
                        break;
                    case OpCode.LOAD_UPVALUE:
                        IP++;
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
                        globals.Add(stack.Pop());
                        break;
                    case OpCode.DECLARE_FUNCTION:
                        {
                            IP++;
                            Operand env = instruction.opA;
                            Operand lambda = instruction.opB;
                            Operand new_fun_address = instruction.opC;
                            Unit this_callable = constants[new_fun_address];
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
                                foreach (UpValueUnit u in this_closure.UpValues)
                                {
                                    // here we convert env from shift based to absolute based
                                    UpValueUnit new_upvalue = GetUpValue(u.Address, CalculateEnvShiftUpVal(u.Env));
                                    new_upValues.Add(new_upvalue);
                                }
                                ClosureUnit new_closure = new ClosureUnit(this_closure.Function, new_upValues);

                                new_closure.Register(variables);
                                foreach (UpValueUnit u in new_closure.UpValues)
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
                            if (op == ASSIGN){
                                variables.SetAt(new_value, address, CalculateEnvShift(n_shift));
                            }else{
                                Unit old_value = variables.GetAt(address, CalculateEnvShift(n_shift));
                                Unit result;
                                switch(op){
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
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }

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
                            if (op == ASSIGN){
                                globals.Set(new_value, address);
                            }else if(parallelVM == true){
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        lock(globals){
                                            Unit result = globals.Get(address) + new_value;
                                            globals.Set(result, address);
                                        }
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        lock(globals){
                                            Unit result = globals.Get(address) - new_value;
                                            globals.Set(result, address);
                                        }
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        lock(globals){
                                            Unit result = globals.Get(address) * new_value;
                                            globals.Set(result, address);
                                        }
                                        break;
                                    case DIVISION_ASSIGN:
                                        lock(globals){
                                            Unit result = globals.Get(address) / new_value;
                                            globals.Set(result, address);
                                        }
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }
                            }else{
                                Unit result;
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        result = globals.Get(address) + new_value;
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        result = globals.Get(address) - new_value;
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        result = globals.Get(address) * new_value;
                                        break;
                                    case DIVISION_ASSIGN:
                                        result = globals.Get(address) / new_value;
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }
                                globals.Set(result, address);
                            }
                            break;
                        }
                    case OpCode.ASSIGN_IMPORTED_GLOBAL:
                        {
                            IP++;
                            Operand op = instruction.opC;
                            Unit new_value = stack.Peek();
                            if (op == ASSIGN){
                                modules[instruction.opB].SetGlobal(new_value, instruction.opA);
                                //modules[instruction.opB].GetGlobal(instruction.opA);
                            }else if(parallelVM == true){
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        lock(modules[instruction.opB].Globals){
                                            Unit result = modules[instruction.opB].GetGlobal(instruction.opA) + new_value;
                                            modules[instruction.opB].SetGlobal(result, instruction.opA);
                                        }
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        lock(modules[instruction.opB].Globals){
                                            Unit result = modules[instruction.opB].GetGlobal(instruction.opA) - new_value;
                                            modules[instruction.opB].SetGlobal(result, instruction.opA);
                                        }
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        lock(modules[instruction.opB].Globals){
                                            Unit result = modules[instruction.opB].GetGlobal(instruction.opA) * new_value;
                                            modules[instruction.opB].SetGlobal(result, instruction.opA);
                                        }
                                        break;
                                    case DIVISION_ASSIGN:
                                        lock(modules[instruction.opB].Globals){
                                            Unit result = modules[instruction.opB].GetGlobal(instruction.opA) / new_value;
                                            modules[instruction.opB].SetGlobal(result, instruction.opA);
                                        }
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }
                            }else{
                                Unit result;
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        result = modules[instruction.opB].GetGlobal(instruction.opA) + new_value;
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        result = modules[instruction.opB].GetGlobal(instruction.opA) - new_value;
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        result = modules[instruction.opB].GetGlobal(instruction.opA) * new_value;
                                        break;
                                    case DIVISION_ASSIGN:
                                        result = modules[instruction.opB].GetGlobal(instruction.opA) / new_value;
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }
                                modules[instruction.opB].SetGlobal(result, instruction.opA);
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
                            if (op == ASSIGN){
                                this_upValue.UpValue = new_value;
                            }else if(parallelVM == true){
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        lock(this_upValue){
                                            this_upValue.UpValue = this_upValue.UpValue + new_value;
                                        }
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        lock(this_upValue){
                                            this_upValue.UpValue = this_upValue.UpValue - new_value;
                                        }
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        lock(this_upValue){
                                            this_upValue.UpValue = this_upValue.UpValue * new_value;
                                        }
                                        break;
                                    case DIVISION_ASSIGN:
                                        lock(this_upValue){
                                            this_upValue.UpValue = this_upValue.UpValue / new_value;
                                        }
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
                                }
                            }else{
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        this_upValue.UpValue = this_upValue.UpValue + new_value;
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        this_upValue.UpValue = this_upValue.UpValue - new_value;
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        this_upValue.UpValue = this_upValue.UpValue * new_value;
                                        break;
                                    case DIVISION_ASSIGN:
                                        this_upValue.UpValue = this_upValue.UpValue / new_value;
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));
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
                                this_table = (this_table.heapUnitValue).Get(v);
                            }
                            Unit new_value = stack.Peek();
                            Unit index = indexes[indexes_counter - 1];
                            UnitType index_type = indexes[indexes_counter - 1].Type;
                            if (op == ASSIGN)
                            {
                                ((this_table.heapUnitValue)).Set(index, new_value);
                            }
                            else if(parallelVM == true)
                            {
                                Unit old_value;
                                Unit result;
                                switch(op){
                                    case ADDITION_ASSIGN:
                                        lock(this_table.heapUnitValue){
                                            old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);
                                            result = old_value + new_value;
                                            (this_table.heapUnitValue).Set(index, result);
                                        }
                                        break;
                                    case SUBTRACTION_ASSIGN:
                                        lock(this_table.heapUnitValue){
                                            old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);
                                            result = old_value - new_value;
                                            (this_table.heapUnitValue).Set(index, result);
                                        }
                                        break;
                                    case MULTIPLICATION_ASSIGN:
                                        lock(this_table.heapUnitValue){
                                            old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);
                                            result = old_value * new_value;
                                            (this_table.heapUnitValue).Set(index, result);
                                        }
                                        break;
                                    case DIVISION_ASSIGN:
                                        lock(this_table.heapUnitValue){
                                            old_value = ((TableUnit)(this_table.heapUnitValue)).Get(index);
                                            result = old_value / new_value;
                                            (this_table.heapUnitValue).Set(index, result);
                                        }
                                        break;
                                    default:
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));

                                }
                            }
                            else
                            {
                                Unit old_value = (this_table.heapUnitValue).Get(index);
                                Unit result;
                                switch(op){
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
                                        throw new Exception("Unknown operator" + VM.ErrorString(this));

                                }
                                (this_table.heapUnitValue).Set(index, result);
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

                            TableUnit new_table = new TableUnit(null, null);

                            int n_table = instruction.opB;
                            for (int i = 0; i < n_table; i++)
                            {
                                Unit val = stack.Pop();
                                Unit key = stack.Pop();
                                new_table.Table.Add(key, val);
                            }

                            int n_elements = instruction.opA;
                            for (int i = 0; i < n_elements; i++)
                            {
                                Unit new_value = stack.Pop();
                                new_table.Elements.Add(new_value);
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
