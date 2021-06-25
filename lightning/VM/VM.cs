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
        List<Instruction>[] instructionsStack;
        int executingInstructions;// contains the currently executing instructions
        ValFunction[] functionCallStack;
        Unit[] stack; // used for operations
        int stackTop;
        List<Unit> globals; // used for global variables
        List<Unit> variables; // used for scoped variables
        int variablesTop;

        int[] variablesBases; // used to control the address used by each scope
        int variablesBasesTop;

        List<ValUpValue> upValues; // used to store upvalues
        int[] upValuesBases; //
        int upValuesBasesTop;

        List<ValUpValue> upValuesRegistry;
        int[] upValuesRegistryBases;
        int upValuesRegistryBasesTop;

        Operand[] ret;
        int ret_count;
        int[] funCallEnv;
        Unit[] stash;
        int stashTop;

        Operand IP;
        List<ValIntrinsic> Intrinsics { get; set; }
        public Dictionary<string, int> loadedModules { get; private set; }
        public List<ValModule> modules;

        Stack<VM> vmPool;
        int function_deepness;

        public VM(Chunk p_chunk, int p_function_deepness = 25)
        {
            chunk = p_chunk;
            function_deepness = p_function_deepness;

            instructionsStack = new List<Instruction>[function_deepness];
            instructionsStack[0] = chunk.Program;
            executingInstructions = 0;
            functionCallStack = new ValFunction[function_deepness];

            stack = new Unit[3 * function_deepness];
            stackTop = 0;
            globals = new List<Unit>();
            variables = new List<Unit>();
            variablesTop = 0;

            variablesBases = new int[3 * function_deepness];
            variablesBasesTop = 0;
            variablesBases[variablesBasesTop] = 0;
            variablesBasesTop++;

            upValues = new List<ValUpValue>();
            upValuesBases = new int[function_deepness];
            upValuesBasesTop = 0;
            upValuesBases[upValuesBasesTop] = 0;
            upValuesBasesTop++;

            upValuesRegistry = new List<ValUpValue>();
            upValuesRegistryBases = new int[function_deepness];
            upValuesRegistryBasesTop = 0;

            ret = new Operand[2 * function_deepness];
            ret[executingInstructions] = (Operand)(chunk.ProgramSize - 1);
            ret_count = 1;
            funCallEnv = new int[function_deepness];
            stash = new Unit[function_deepness];
            stashTop = 0;
            IP = 0;

            Intrinsics = chunk.Prelude.intrinsics;
            foreach (ValIntrinsic v in Intrinsics)
            {
                globals.Add(new Unit(v));
            }
            foreach (KeyValuePair<string, ValTable> entry in chunk.Prelude.tables)
            {
                globals.Add(new Unit(entry.Value));
            }

            loadedModules = new Dictionary<string, int>();
            modules = new List<ValModule>();

            vmPool = new Stack<VM>();
        }

        public void ResoursesTrim(){
            upValuesRegistry.TrimExcess();
            variables.TrimExcess();
            upValues.TrimExcess();
        }
        public void ReleaseVMs(int count){
            for (int i = 0; i < count; i++)
                if (vmPool.Count > 0)
                    vmPool.Pop();
        }
        public void ReleaseVMs(){
            vmPool.Clear();
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
                VM new_vm = new VM(chunk, function_deepness);
                new_vm.globals = globals;
                return new_vm;
            }
        }

        public Operand AddModule(ValModule this_module)
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
            return globals[address];
        }

        void StackPush(Unit p_value)
        {
            stack[stackTop] = p_value;
            stackTop++;
        }

        Unit StackPop()
        {
            stackTop--;
            Unit popped = stack[stackTop];
            return popped;
        }

        Unit StackPeek()
        {
            return stack[stackTop - 1];
        }

        public Unit StackPeek(int n)
        {
            if (n < 0 || n > (stackTop - 1)){
                Console.WriteLine("ERROR: tried to read empty stack!");
                throw new Exception("Reading empty stack");
            }
            return stack[stackTop - n - 1];
        }

        Unit VarAt(Operand address, Operand n_env)
        {
            int this_BP = variablesBases[n_env];
            return variables[this_BP + address];
        }

        void VarSet(Unit new_value, Operand address, Operand n_env)
        {
            variables[address + variablesBases[n_env]] = new_value;
        }

        void RegisterUpValue(ValUpValue u)
        {
            upValuesRegistry.Add(u);
        }

        void EnvPush()
        {
            variablesBases[variablesBasesTop] = variablesTop;
            variablesBasesTop++;
            upValuesRegistryBases[upValuesRegistryBasesTop] = upValuesRegistry.Count;
            upValuesRegistryBasesTop++;
        }

        void EnvPop()
        {
            // capture closures
            int upvalues_start = upValuesRegistryBases[upValuesRegistryBasesTop - 1];
            upValuesRegistryBasesTop--;
            int upvalues_end = upValuesRegistry.Count;
            for (int i = upvalues_start; i < upvalues_end; i++)
            {
                upValuesRegistry[i].Capture();
            }

            int this_basePointer = variablesBases[variablesBasesTop - 1];
            variablesBasesTop--;
            variablesTop = this_basePointer;

            variables.RemoveRange(this_basePointer, variables.Count - this_basePointer);
            upValuesRegistry.RemoveRange(upvalues_start, upValuesRegistry.Count - upvalues_start);
        }

        void EnvSet(int target_env)
        {
            while ((variablesBasesTop - 1) > target_env)
            {
                EnvPop();
            }
        }

        void UpValuesPush()
        {
            upValuesBases[upValuesBasesTop] = upValues.Count;
            upValuesBasesTop++;
        }

        void UpValuesPop()
        {
            int this_UpvaluesBase = upValuesBases[upValuesBasesTop - 1];
            upValuesBasesTop--;

            upValues.RemoveRange(this_UpvaluesBase, upValues.Count - this_UpvaluesBase);
        }

        ValUpValue UpValuesAt(int address)
        {
            int this_UpvalueBasePointer = upValuesBases[upValuesBasesTop - 1];
            return upValues[this_UpvalueBasePointer + address];
        }

        void Error(string msg)
        {
            if (executingInstructions == 0)
                Console.WriteLine("Error: " + msg + chunk.GetLine(IP));
            else
            {
                Console.Write("Error: " + msg);
                Console.Write(" on function: " + functionCallStack[executingInstructions].name);
                Console.Write(" from module: " + functionCallStack[executingInstructions].module);
                Console.WriteLine(
                    " on line: "
                    + chunk.GetLine(IP + functionCallStack[executingInstructions].originalPosition));
            }
        }

        public Unit CallFunction(Unit this_callable, List<Unit> args)
        {
            if (args != null)
                for (int i = args.Count - 1; i >= 0; i--)
                    StackPush(args[i]);

            Type this_type = this_callable.Type();
            ret[ret_count] = (Operand)(chunk.ProgramSize - 1);
            ret_count += 1;
            funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;
            if (this_type == typeof(ValFunction))
            {
                ValFunction this_func = (ValFunction)(this_callable.value);
                instructionsStack[executingInstructions + 1] = this_func.body;
                functionCallStack[executingInstructions + 1] = this_func;
                executingInstructions = executingInstructions + 1;
                IP = 0;
            }
            else if (this_type == typeof(ValClosure))
            {
                ValClosure this_closure = (ValClosure)(this_callable.value);
                UpValuesPush();

                foreach (ValUpValue u in this_closure.upValues)
                {
                    upValues.Add(u);
                }
                instructionsStack[executingInstructions + 1] = this_closure.function.body;
                functionCallStack[executingInstructions + 1] = this_closure.function;
                executingInstructions = executingInstructions + 1;
                IP = 0;
            }
            else if (this_type == typeof(ValIntrinsic))
            {
                ValIntrinsic this_intrinsic = (ValIntrinsic)(this_callable.value);
                Unit intrinsic_result = this_intrinsic.function(this);
                stackTop -= this_intrinsic.arity;
                StackPush(intrinsic_result);
            }
            VMResult result = Run();
            if (result.status == VMResultType.OK)
                return result.value;
            return new Unit(Value.Nil);
        }

        public VMResult Run()
        {
            Instruction instruction;
            //Console.WriteLine(System.Runtime.InteropServices.Marshal.SizeOf(chunk.ReadInstruction(IP)));
            //Console.WriteLine("All set, let's go!\n");
            while (true)
            {
                instruction = instructionsStack[executingInstructions][IP];
                //Console.Write(IP + " ");
                //Chunk.PrintInstruction(instruction);
                //Console.WriteLine();
                switch (instruction.opCode)
                {
                    case OpCode.POP:
                        {
                            IP++;
                            Unit value = StackPop();
                            break;
                        }
                    case OpCode.LOADC:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit constant = chunk.GetConstant(address);
                            StackPush(constant);
                            break;
                        }
                    case OpCode.LOADV:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            var variable = VarAt(address, (Operand)(variablesBasesTop - 1 - n_shift));
                            StackPush(variable);
                            break;
                        }
                    case OpCode.LOADG:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Unit global;
                            global = globals[address];
                            StackPush(global);
                            break;
                        }
                    case OpCode.LOADGI:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit global = modules[module].globals[address];
                            StackPush(global);
                            break;
                        }
                    case OpCode.LOADCI:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Unit constant = modules[module].constants[address];
                            
                            StackPush(constant);
                            break;
                        }
                    case OpCode.LOADUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            ValUpValue up_val = UpValuesAt(address);
                            StackPush(up_val.Val);
                            break;
                        }
                    case OpCode.LOADNIL:
                        {
                            IP++;
                            StackPush(new Unit(Value.Nil));
                            break;
                        }
                    case OpCode.LOADTRUE:
                        {
                            IP++;
                            StackPush(new Unit(Value.True));
                            break;
                        }
                    case OpCode.LOADFALSE:
                        {
                            IP++;
                            StackPush(new Unit(Value.False));
                            break;
                        }
                    case OpCode.LOADINTR:
                        {
                            IP++;
                            Operand value = instruction.opA;
                            StackPush(new Unit(Intrinsics[value]));
                            break;
                        }
                    case OpCode.VARDCL:
                        {
                            IP++;
                            Unit new_value = StackPop();

                            if (variablesTop > (variables.Count - 1))
                            {
                                variables.Add(new_value);
                                variablesTop++;
                            }
                            else
                            {
                                variables[variablesTop] = new_value;
                                variablesTop++;
                            }
                            break;
                        }
                    case OpCode.GLOBALDCL:
                        {
                            IP++;
                            Unit new_value = StackPop();
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
                            if (this_callable.Type() == typeof(ValFunction))
                            {
                                if (lambda == 0)
                                    if (env == 0)// Global
                                    {
                                        globals.Add(this_callable);
                                    }
                                    else
                                    {
                                        if (variablesTop >= (variables.Count))
                                        {
                                            variables.Add(this_callable);
                                            variablesTop++;
                                        }
                                        else
                                        {
                                            variables[variablesTop] = this_callable;
                                            variablesTop++;
                                        }
                                    }
                                else
                                    StackPush(this_callable);
                            }
                            else
                            {
                                ValClosure this_closure = (ValClosure)(this_callable.value);

                                // new upvalues
                                List<ValUpValue> new_upValues = new List<ValUpValue>();
                                foreach (ValUpValue u in this_closure.upValues)
                                {
                                    // here we convert env from shift based to absolute based
                                    ValUpValue new_upvalue = new ValUpValue(u.address, (Operand)(variablesBasesTop - u.env));
                                    new_upValues.Add(new_upvalue);
                                }
                                ValClosure new_closure = new ValClosure(this_closure.function, new_upValues);

                                new_closure.Register(variables, variablesBases);
                                foreach (ValUpValue u in new_closure.upValues)
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
                                        if (variablesTop >= (variables.Count))
                                        {
                                            variables.Add(new_closure_unit);
                                            variablesTop++;
                                        }
                                        else
                                        {
                                            variables[variablesTop] = new_closure_unit;
                                            variablesTop++;
                                        }
                                    }
                                else
                                    StackPush(new_closure_unit);
                            }
                            break;
                        }
                    case OpCode.ASSIGN:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            Operand op = instruction.opC;
                            Unit new_value = StackPeek();
                            if (op == 0)
                            {
                                VarSet(new_value, address, (Operand)(variablesBasesTop - 1 - n_shift));
                            }
                            else
                            {
                                Unit old_value = VarAt(address, (Operand)(variablesBasesTop - 1 - n_shift));
                                Number result = 0;
                                if (op == 1)
                                    result = old_value.number + new_value.number;
                                else if (op == 2)
                                    result = old_value.number - new_value.number;
                                else if (op == 3)
                                    result = old_value.number * new_value.number;
                                else if (op == 4)
                                    result = old_value.number / new_value.number;
                                VarSet(new Unit(result), address, (Operand)(variablesBasesTop - 1 - n_shift));
                            }
                            break;
                        }
                    case OpCode.ASSIGNG:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            Unit new_value = StackPeek();
                            if (op == 0)
                            {
                                lock(globals){
                                    globals[address] = new_value;
                                }
                            }
                            else
                            {
                                Unit old_value;
                                lock(globals){
                                    old_value = globals[address];
                                
                                    Number result = 0;
                                    if (op == 1)
                                        result = old_value.number + new_value.number;
                                    else if (op == 2)
                                        result = old_value.number - new_value.number;
                                    else if (op == 3)
                                        result = old_value.number * new_value.number;
                                    else if (op == 4)
                                        result = old_value.number / new_value.number;

                                    globals[address] = new Unit(result);
                                }
                            }
                            break;
                        }
                    case OpCode.ASSIGNUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand op = instruction.opB;
                            ValUpValue this_upValue = UpValuesAt(address);
                            Unit new_value = StackPeek();
                            if (op == 0)
                            {
                                this_upValue.Val = new_value;
                            }
                            else
                            {
                                Unit old_value = this_upValue.Val;
                                Number result = 0;
                                if (op == 1)
                                    result = old_value.number + new_value.number;
                                else if (op == 2)
                                    result = old_value.number - new_value.number;
                                else if (op == 3)
                                    result = old_value.number * new_value.number;
                                else if (op == 4)
                                    result = old_value.number / new_value.number;
                                this_upValue.Val = new Unit(result);
                            }
                            break;
                        }
                    case OpCode.TABLEGET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;

                            Unit[] indexes = new Unit[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = StackPop();

                            Unit value = StackPop();
                            foreach (Unit v in indexes)
                            {
                                if (v.type == UnitType.Number)
                                {
                                    value = ((ValTable)(value.value)).elements[(int)v.number];
                                }
                                else
                                {
                                    value = ((ValTable)(value.value)).table[(ValString)v.value];
                                }
                            }
                            StackPush(value);
                            break;
                        }

                    case OpCode.TABLESET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;
                            Operand op = instruction.opB;

                            Unit[] indexes = new Unit[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = StackPop();

                            Unit this_table = StackPop();

                            for (int i = 0; i < indexes_counter - 1; i++)
                            {
                                Unit v = indexes[i];
                                if (v.type == UnitType.Number)
                                {
                                    this_table = ((ValTable)(this_table.value)).elements[(int)v.number];
                                }
                                else
                                {
                                    this_table = ((ValTable)(this_table.value)).table[(ValString)v.value];
                                }
                            }
                            Unit new_value = StackPeek();
                            if (op == 0)
                            {
                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                {
                                    if (((ValTable)(this_table.value)).elements.Count - 1 >= ((int)(indexes[indexes_counter - 1].number)))
                                    {
                                        Unit old_value = ((ValTable)(this_table.value)).elements[(int)(indexes[indexes_counter - 1].number)];
                                    }
                                    ((ValTable)(this_table.value)).ElementSet((int)(indexes[indexes_counter - 1].number), new_value);
                                }
                                else
                                {
                                    if (((ValTable)(this_table.value)).table.ContainsKey((ValString)indexes[indexes_counter - 1].value))
                                    {
                                        Unit old_value = ((ValTable)(this_table.value)).table[(ValString)indexes[indexes_counter - 1].value];
                                    }
                                    ((ValTable)(this_table.value)).TableSet((ValString)indexes[indexes_counter - 1].value, new_value);
                                }
                            }
                            else
                            {
                                Unit old_value;
                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                    old_value = ((ValTable)(this_table.value)).elements[(int)(indexes[indexes_counter - 1].number)];
                                else
                                    old_value = ((ValTable)(this_table.value)).table[(ValString)indexes[indexes_counter - 1].value];

                                Number result = 0;
                                if (op == 1)
                                    result = old_value.number + new_value.number;
                                else if (op == 2)
                                    result = old_value.number - new_value.number;
                                else if (op == 3)
                                    result = old_value.number * new_value.number;
                                else if (op == 4)
                                    result = old_value.number / new_value.number;

                                if (indexes[indexes_counter - 1].type == UnitType.Number)
                                    ((ValTable)(this_table.value)).elements[(int)(indexes[indexes_counter - 1].number)] = new Unit(result);
                                else
                                    ((ValTable)(this_table.value)).table[(ValString)indexes[indexes_counter - 1].value] = new Unit(result);

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
                            bool value = StackPop().ToBool();
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
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            break;
                        }
                    case OpCode.RETSREL:
                        {
                            Operand value = instruction.opA;
                            ret[ret_count] = ((Operand)(value + IP));
                            ret_count += 1;
                            IP++;
                            break;
                        }
                    case OpCode.ADD:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;

                            Number result = opA + opB;
                            StackPush(new Unit(result));

                            break;
                        }
                    case OpCode.APP:
                        {
                            IP++;
                            Unit opB = StackPop();
                            Unit opA = StackPop();

                            string result = opA.ToString() + opB.ToString();
                            Unit new_value = new Unit(new ValString(result));
                            StackPush(new_value);

                            break;
                        }
                    case OpCode.SUB:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;

                            Number result = opA - opB;
                            StackPush(new Unit(result));

                            break;
                        }
                    case OpCode.MUL:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;

                            Number result = opA * opB;
                            StackPush(new Unit(result));

                            break;
                        }
                    case OpCode.DIV:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;

                            Number result = opA / opB;
                            StackPush(new Unit(result));

                            break;
                        }
                    case OpCode.NEG:
                        {
                            IP++;
                            Number opA = StackPop().number;
                            Unit new_value = new Unit(-opA);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.INC:
                        {
                            IP++;
                            Number opA = StackPop().number;
                            Unit new_value = new Unit(opA + 1);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            IP++;
                            Number opA = StackPop().number;
                            Unit new_value = new Unit(opA - 1);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.EQ:
                        {
                            IP++;
                            Unit opB = StackPop();
                            Unit opA = StackPop();
                            if (opA.Equals(opB))
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }

                            break;
                        }
                    case OpCode.NEQ:
                        {
                            IP++;
                            Unit opB = StackPop();
                            Unit opA = StackPop();

                            if (!opA.Equals(opB))
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }

                            break;
                        }
                    case OpCode.GTQ:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;
                            bool truthness = opA >= opB;
                            if (truthness == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.LTQ:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;
                            bool truthness = opA <= opB;
                            if (truthness == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.GT:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;
                            bool truthness = opA > opB;
                            if (truthness == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.LT:
                        {
                            IP++;
                            Number opB = StackPop().number;
                            Number opA = StackPop().number;
                            bool truthness = opA < opB;
                            if (truthness == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.NOT:
                        {
                            IP++;
                            Value opA = StackPop().value;
                            if (opA.ToBool() == true)
                                StackPush(new Unit(Value.False));
                            else if (opA.ToBool() == false)
                                StackPush(new Unit(Value.True));
                            else
                            {
                                Error("NOT is insane!");
                                return new VMResult(VMResultType.ERROR, new Unit(Value.Nil));
                            }

                            break;
                        }
                    case OpCode.AND:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;
                            bool result = opA.ToBool() && opB.ToBool();
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.OR:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;

                            bool result = opA.ToBool() || opB.ToBool();
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.XOR:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;
                            bool result = opA.ToBool() ^ opB.ToBool();
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.NAND:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;
                            bool result = !(opA.ToBool() && opB.ToBool());
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.NOR:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;
                            bool result = !(opA.ToBool() || opB.ToBool());
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.XNOR:
                        {
                            IP++;
                            Value opB = StackPop().value;
                            Value opA = StackPop().value;
                            bool result = !(opA.ToBool() ^ opB.ToBool());
                            if (result == true)
                            {
                                StackPush(new Unit(Value.True));
                            }
                            else
                            {
                                StackPush(new Unit(Value.False));
                            }
                            break;
                        }
                    case OpCode.CLOSURECLOSE:
                        {
                            UpValuesPop();
                            int target_env = funCallEnv[executingInstructions];
                            EnvSet(target_env);
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            executingInstructions = executingInstructions - 1;
                            break;
                        }
                    case OpCode.FUNCLOSE:
                        {
                            int target_env = funCallEnv[executingInstructions];
                            EnvSet(target_env);
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            executingInstructions = executingInstructions - 1;
                            break;
                        }
                    case OpCode.NTABLE:
                        {
                            IP++;

                            ValTable new_table = new ValTable(null, null);

                            int n_table = instruction.opB;
                            for (int i = 0; i < n_table; i++)
                            {
                                Unit val = StackPop();
                                Unit key = StackPop();
                                new_table.table.Add((ValString)key.value, val);
                            }

                            int n_elements = instruction.opA;
                            for (int i = 0; i < n_elements; i++)
                            {
                                Unit new_value = StackPop();
                                new_table.elements.Add(new_value);
                            }

                            StackPush(new Unit(new_table));
                            break;
                        }
                    case OpCode.CALL:
                        {
                            IP++;

                            Unit this_callable = StackPop();
                            Type this_type = this_callable.Type();

                            if (this_type == typeof(ValFunction))
                            {
                                ret[ret_count] = IP;// add return address to stack
                                ret_count += 1;
                                funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;// sets the return env to funclose/closureclose
                                ValFunction this_func = (ValFunction)this_callable.value;
                                instructionsStack[executingInstructions + 1] = this_func.body;
                                functionCallStack[executingInstructions + 1] = this_func;
                                executingInstructions = executingInstructions + 1;
                                IP = 0;
                            }
                            else if (this_type == typeof(ValClosure))
                            {
                                ret[ret_count] = IP;// add return address to stack
                                ret_count += 1;
                                funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;// sets the return env to funclose/closureclose

                                ValClosure this_closure = (ValClosure)this_callable.value;
                                UpValuesPush();

                                foreach (ValUpValue u in this_closure.upValues)
                                {
                                    upValues.Add(u);
                                }
                                instructionsStack[executingInstructions + 1] = this_closure.function.body;
                                functionCallStack[executingInstructions + 1] = this_closure.function;
                                executingInstructions = executingInstructions + 1;
                                IP = 0;
                            }
                            else if (this_type == typeof(ValIntrinsic))
                            {
                                ValIntrinsic this_intrinsic = (ValIntrinsic)this_callable.value;
                                Unit result = this_intrinsic.function(this);
                                stackTop -= this_intrinsic.arity;
                                StackPush(result);
                            }
                            else
                            {
                                Error("Trying to call a " + this_callable.Type());
                                return new VMResult(VMResultType.OK, new Unit(Value.Nil));
                            }
                            break;
                        }
                    case OpCode.STASHTOP:
                        {
                            IP++;

                            stash[stashTop] = StackPop();
                            stashTop++;
                            break;
                        }
                    case OpCode.POPSTASH:
                        {
                            IP++;
                            StackPush(stash[stashTop - 1]);
                            stashTop--;
                            break;
                        }
                    case OpCode.FOREACH:
                        {
                            IP++;
                            Unit func = StackPop();
                            ValTable table = (ValTable)(StackPop().value);

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
                            Unit func = StackPop();
                            ValTable table = (ValTable)(StackPop().value);
                            Number tasks = StackPop().number;

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
                            Unit result = new Unit(Value.Nil);
                            if (stackTop > 0)
                                result = StackPop();
                            return new VMResult(VMResultType.OK, result);
                        }
                    default:
                        Error("Unkown OpCode: " + instruction.opCode);
                        return new VMResult(VMResultType.ERROR, new Unit(Value.Nil));
                }
            }
        }

        public string Stats()
        {
            string statsValue = "";
            int counter = 0;
            for (int i = 0; i < stackTop; i++)
            {
                if (i == 0)
                {
                    statsValue += "Stack:\n";
                }
                statsValue += counter.ToString() + ": " + stack[i].ToString() + '\n';
                counter++;
            }
            if (counter == 0)
            {
                statsValue += "Stack empty :)\n";
            }

            counter = 0;
            for (int i = 0; i < variablesTop; i++)
            {
                if (i == 0)
                {
                    statsValue = statsValue + "Variables:\n";
                }
                statsValue += counter.ToString() + ": " + variables[i].ToString() + '\n';
                counter++;
            }
            if (counter == 0)
            {
                statsValue += "Variables empty :)\n";
            }

            counter = 0;
            for (int i = 0; i < upValuesBases[upValuesBasesTop - 1]; i++)
            {
                if (i == 0)
                {
                    statsValue += "Upvalues:\n";
                }
                foreach (Value v in upValues)
                {
                    statsValue += counter.ToString() + ": " + upValues[i].ToString() + '\n';
                    counter++;
                }
            }
            if (counter == 0)
            {
                statsValue += "Upvalues empty :)\n";
            }
            return statsValue;
        }
    }
}
